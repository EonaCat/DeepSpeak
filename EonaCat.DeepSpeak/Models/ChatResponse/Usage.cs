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

using EonaCat.Json;

namespace EonaCat.DeepSpeak.Models;

/// <summary>
/// Usage request information
/// </summary>
public class Usage
{
    [JsonProperty("completion_tokens")]
    public long CompletionTokens { get; set; }

    [JsonProperty("prompt_tokens")]
    public long PromptTokens { get; set; }

    [JsonProperty("total_tokens")]
    public long TotalTokens { get; set; }

    [JsonProperty("prompt_cache_hit_tokens")]
    public long PromptCacheHitTokens { get; set; }

    [JsonProperty("prompt_cache_miss_tokens")]
    public long PromptCacheMissTokens { get; set; }
}
