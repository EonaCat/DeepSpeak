# EonaCat.DeepSpeak

You need to have a DeepSeek API Key: 

Obtain from [DeepSeek](https://platform.deepseek.com/)

```bash
dotnet add package EonaCat.DeepSpeak
```

### Setup Client

```csharp
var client = new DeepSpeakClient("api_key");
```

### Get Models

```csharp
var models = await client.ListModelsAsync();
if (models?.Data is null)
{
    Console.WriteLine($"Error: {client.ErrorMsg}");
    return;
}

foreach (var model in models.Data)
{
    Console.WriteLine($"- {model.Id}: {model.Capabilities}");
}
```

### Chat

```csharp
var chatRequest = new ChatRequest
{
    Messages = new[]
    {
        Message.NewUserMessage("Explain how stocks work"),
    },
    Model = ChatModels.Chat
};

var response = await client.ChatAsync(chatRequest);
Console.WriteLine(response?.Choices.First().Message.Content ?? "No response");
```

### Streaming the contents

```csharp
var streamRequest = new ChatRequest
{
    Messages = new[] { Message.NewUserMessage("Generate a story about cats in 500 words") },
    Model = ChatModels.Chat,
    Stream = true
};

var stream = await client.ChatStreamAsync(streamRequest);
if (stream is null) return;

await foreach (var chunk in stream)
{
    Console.Write(chunk.Delta?.Content);
}
```