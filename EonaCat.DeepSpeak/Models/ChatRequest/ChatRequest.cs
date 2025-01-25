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

using EonaCat.Json;

namespace EonaCat.DeepSpeak.Models;

/// <summary>
/// Chat request
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// List of messages
    /// </summary>
    public Message[] Messages { get; set; } = Array.Empty<Message>();

    /// <summary>
    /// The ID of the model to use. 
    /// You can use deepseek-chat or deepseek-reasoner.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// A number between -2.0 and 2.0. Positive values penalize new tokens based on their existing frequency in the text so far, reducing the model's likelihood to repeat the same content.
    /// </summary>
    [JsonProperty("frequency_penalty")]
    public double FrequencyPenalty { get; set; } = 0;

    /// <summary>
    /// The maximum number of tokens allowed for the generated completion in a single request. The total length of input tokens and output tokens is limited by the model's context length.
    /// default: 4096
    /// </summary>
    [JsonProperty("max_tokens")]
    public long MaxTokens { get; set; } = 4096;

    /// <summary>
    /// A number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.
    /// </summary>
    [JsonProperty("presence_penalty")]
    public double PresencePenalty { get; set; } = 0;

    /// <summary>
    /// A list of up to 4 strings. The API will stop generating further tokens upon encountering any of these strings.
    /// </summary>
    public List<string> Stop { get; set; } = [];

    /// <summary>
    /// If set to true, messages will be sent incrementally in a stream using SSE (server-sent events). The message stream ends with data: [DONE].
    /// </summary>
    [JsonProperty]
    internal bool Stream { get; set; }

    /// <summary>
    /// Sampling temperature, between 0 and 2. Higher values like 0.8 make the output more random, while lower values like 0.2 make it more focused and deterministic. We generally recommend adjusting this or top_p, but not both.
    /// </summary>
    public double Temperature { get; set; } = 1;

    /// <summary>
    /// An alternative to sampling with temperature, the model considers the results of the top_p probability tokens. So 0.1 means only the tokens comprising the top 10% probability mass are considered. We generally recommend adjusting this or temperature, but not both.
    /// </summary>
    [JsonProperty("top_p")]
    public double TopP { get; set; } = 1;

    /// <summary>
    /// Whether to return the log probabilities of the output tokens. If true, the log probabilities of each output token are returned in the content of the message.
    /// </summary>
    public bool Logprobs { get; set; }

    /// <summary>
    /// An integer N between 0 and 20, specifying the top N tokens with the highest probabilities to return for each output position, along with their log probabilities. If this parameter is specified, logprobs must be true.
    /// </summary>
    [JsonProperty("top_logprobs")]
    public int? TopLogprobs { get; set; }
}