using System.ComponentModel.DataAnnotations;

namespace RetroScan.Api.LanguageModel;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    [Required]
    public Uri BaseUrl { get; set; } = new("https://api.groq.com/openai/v1/");

    public string? ApiKey { get; set; }

    [Required]
    public string Model { get; set; } = "openai/gpt-oss-120b";

    // Only for reasoning models such as gpt-oss; leave empty for a model that rejects the parameter.
    [AllowedValues(null, "", "low", "medium", "high")]
    public string? ReasoningEffort { get; set; } = "low";

    [Range(0, 2)]
    public double Temperature { get; set; } = 0.2;

    [Range(1, 8192)]
    public int MaxCompletionTokens { get; set; } = 1024;
}
