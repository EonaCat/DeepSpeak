﻿/*
EonaCat DeepSpeak
Copyright (C) 2025 EonaCat (Jeroen Saey)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    https://EonaCat.com/License

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License

*/

using EonaCat.DeepSpeak.Models;
using EonaCat.Json.Serialization;
using EonaCat.Json;
using Microsoft.Extensions.AI;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace EonaCat.DeepSpeak;

public class DeepSpeakClient : IChatClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public JsonSerializerSettings JsonSerializerSettings { get; } = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
        Formatting = Formatting.None
    };

    public string? ErrorMessage { get; private set; }

    public DeepSpeakClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ConfigureHttpClient(apiKey);
    }

    public DeepSpeakClient(string apiKey) : this(new HttpClient(), apiKey)
    {
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    private void ConfigureHttpClient(string apiKey)
    {
        _httpClient.BaseAddress = new Uri(Constants.BaseAddress);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public void SetTimeout(int seconds)
    {
        if (seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        _httpClient.Timeout = TimeSpan.FromSeconds(seconds);
    }

    public async Task<ModelResponse?> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(Constants.ModelsEndpoint, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponse(response);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonHelper.ToObject<ModelResponse>(json, JsonSerializerSettings);
    }

    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        request.Stream = false;
        var content = new StringContent(JsonHelper.ToJson(request, JsonSerializerSettings), Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync(Constants.CompletionEndpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponse(response);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonHelper.ToObject<ChatResponse>(json, JsonSerializerSettings);
    }

    public async Task<IAsyncEnumerable<Choice>?> ChatStreamAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        request.Stream = true;
        var content = new StringContent(JsonHelper.ToJson(request, JsonSerializerSettings), Encoding.UTF8, "application/json");

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, Constants.CompletionEndpoint)
        {
            Content = content,
        };

        var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var stream = await response.Content.ReadAsStreamAsync();
            return ProcessStream(stream);
        }
        else
        {
            ErrorMessage = await response.Content.ReadAsStringAsync();
            return null;
        }
    }

    private IAsyncEnumerable<Choice> ProcessStream(Stream stream)
    {
        var reader = new StreamReader(stream);

        var channel = Channel.CreateUnbounded<Choice>();
        _ = Task.Run(async () =>
        {
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line is null)
                {
                    break;
                }

                line = line?.Replace("data:", "").Trim();

                if (line == Constants.StreamDoneSign) break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var chatResponse = JsonHelper.ToObject<ChatResponse>(line, JsonSerializerSettings);
                var choice = chatResponse?.Choices.FirstOrDefault();
                if (choice is null) continue;

                await channel.Writer.WriteAsync(choice);
            }
            channel.Writer.Complete();
        });

        return channel.Reader.ReadAllAsync();
    }

    private async Task HandleErrorResponse(HttpResponseMessage response)
    {
        ErrorMessage = $"HTTP {response.StatusCode}: {await response.Content.ReadAsStringAsync()}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    async Task<ChatCompletion> IChatClient.CompleteAsync(IList<ChatMessage> chatMessages, ChatOptions? options, CancellationToken cancellationToken)
    {
        ChatRequest request = CreateChatRequest(chatMessages, options);

        ChatResponse? response = await ChatAsync(request, cancellationToken);
        ThrowIfRequestFailed(response);

        return CreateChatCompletion(response);
    }

    async IAsyncEnumerable<StreamingChatCompletionUpdate> IChatClient.CompleteStreamingAsync(IList<ChatMessage> chatMessages, ChatOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IAsyncEnumerable<Choice>? choices = await ChatStreamAsync(CreateChatRequest(chatMessages, options), cancellationToken);
        ThrowIfRequestFailed(choices);

        await foreach (var choice in choices)
        {
            yield return CreateStreamingChatCompletionUpdate(choice);
        }
    }

    object? IChatClient.GetService(Type serviceType, object? serviceKey) =>
        serviceKey is null && serviceType?.IsInstanceOfType(this) is true ? this : null;

    ChatClientMetadata IChatClient.Metadata { get; } = new("deepseek", new Uri(Constants.BaseAddress));

    private void ThrowIfRequestFailed([NotNull] object? response)
    {
        if (response is null)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(ErrorMessage) ?
                $"Failed to get response" :
                $"Failed to get response: {ErrorMessage}");
        }
    }

    private static ChatCompletion CreateChatCompletion(ChatResponse response)
    {
        ChatCompletion completion = new([])
        {
            CompletionId = response.Id,
            ModelId = response.Model,
            CreatedAt = DateTimeOffset.FromUnixTimeSeconds(response.Created)
        };

        foreach (var choice in response.Choices)
        {
            completion.FinishReason ??= CreateFinishReason(choice);
            completion.Choices.Add(CreateChatMessage(choice));
        }

        if (response.Usage is Usage usage)
        {
            completion.Usage = new()
            {
                InputTokenCount = (int)usage.PromptTokens,
                TotalTokenCount = (int)usage.TotalTokens,
                OutputTokenCount = (int)usage.CompletionTokens,
                AdditionalCounts = new()
                {
                    [nameof(usage.PromptCacheHitTokens)] = (int)usage.PromptCacheHitTokens,
                    [nameof(usage.PromptCacheMissTokens)] = (int)usage.PromptCacheMissTokens,
                },
            };
        }

        return completion;
    }

    private static ChatFinishReason? CreateFinishReason(Choice choice) =>
        choice.FinishReason switch
        {
            "stop" => ChatFinishReason.Stop,
            "length" => ChatFinishReason.Length,
            "content_filter" => ChatFinishReason.ContentFilter,
            "tool_calls" => ChatFinishReason.ToolCalls,
            _ => null,
        };

    private static ChatMessage CreateChatMessage(Choice choice)
    {
        Message? choiceMessage = choice.Delta ?? choice.Message;

        ChatMessage m = new()
        {
            RawRepresentation = choice,
            Role = CreateChatRole(choiceMessage),
            Text = choiceMessage?.Content
        };

        if (choice.Logprobs is not null)
        {
            (m.AdditionalProperties ??= []).Add(nameof(choice.Logprobs), choice.Logprobs);
        }

        return m;
    }

    private static StreamingChatCompletionUpdate CreateStreamingChatCompletionUpdate(Choice choice)
    {
        Message? choiceMessage = choice.Delta ?? choice.Message;

        StreamingChatCompletionUpdate update = new()
        {
            ChoiceIndex = (int)choice.Index,
            FinishReason = CreateFinishReason(choice),
            RawRepresentation = choice,
            Role = CreateChatRole(choiceMessage),
            Text = choiceMessage?.Content
        };

        if (choice.Logprobs is not null)
        {
            (update.AdditionalProperties ??= []).Add(nameof(choice.Logprobs), choice.Logprobs);
        }

        return update;
    }

    private static ChatRole CreateChatRole(Message? m) =>
        m?.Role switch
        {
            "user" => ChatRole.User,
            "system" => ChatRole.System,
            _ => ChatRole.Assistant,
        };

    private static ChatRequest CreateChatRequest(IList<ChatMessage> chatMessages, ChatOptions? options)
    {
        ChatRequest request = new();

        if (options is not null)
        {
            if (options.ModelId is not null) request.Model = options.ModelId;
            if (options.FrequencyPenalty is not null) request.FrequencyPenalty = options.FrequencyPenalty.Value;
            if (options.MaxOutputTokens is not null) request.MaxTokens = options.MaxOutputTokens.Value;
            if (options.PresencePenalty is not null) request.PresencePenalty = options.PresencePenalty.Value;
            if (options.StopSequences is not null) request.Stop = [.. options.StopSequences];
            if (options.Temperature is not null) request.Temperature = options.Temperature.Value;
            if (options.TopP is not null) request.TopP = options.TopP.Value;
            if (options.AdditionalProperties?.TryGetValue(nameof(request.Logprobs), out bool logprobs) is true) request.Logprobs = logprobs;
            if (options.AdditionalProperties?.TryGetValue(nameof(request.TopLogprobs), out int topLogprobs) is true) request.TopLogprobs = topLogprobs;
        }

        List<Message> messages = [];
        foreach (var message in chatMessages)
        {
            string role;
            if (message.Role == ChatRole.User) role = "user";
            else if (message.Role == ChatRole.Assistant) role = "assistant";
            else if (message.Role == ChatRole.System) role = "system";
            else continue;

            string text = string.Concat(message.Contents.OfType<TextContent>());

            if (!string.IsNullOrWhiteSpace(text))
            {
                messages.Add(new() { Content = text, Role = role });
            }
        }

        request.Messages = messages.ToArray();
        return request;
    }
}
