/*
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

namespace EonaCat.DeepSpeak.Models;

public class Message
{
    public string Content { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;


    public static Message NewUserMessage(string content)
    {
        return new Message
        {
            Content = content,
            Role = "user"
        };
    }

    public static Message NewSystemMessage(string content)
    {
        return new Message
        {
            Content = content,
            Role = "system"
        };
    }

    public static Message NewAssistantMessage(string content)
    {
        return new Message
        {
            Content = content,
            Role = "assistant"
        };
    }
}
