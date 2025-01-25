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

namespace EonaCat.DeepSpeak;

public static class Constants
{
    
    /// <summary>
    /// Base domain for API requests
    /// </summary>
    public const string BaseAddress = "https://api.deepseek.com";

    /// <summary>
    /// Chat completions endpoint
    /// </summary>
    public const string CompletionEndpoint = "/chat/completions";

    /// <summary>
    /// Models list endpoint
    /// </summary>
    public const string ModelsEndpoint = "/models";

    /// <summary>
    /// Stream completion indicator
    /// </summary>
    public const string StreamDoneSign = "[DONE]";
}
