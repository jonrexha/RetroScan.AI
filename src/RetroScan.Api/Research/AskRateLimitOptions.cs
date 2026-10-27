using System.ComponentModel.DataAnnotations;

namespace RetroScan.Api.Research;

public sealed class AskRateLimitOptions
{
    public const string SectionName = "RateLimit";
    public const string PolicyName = "ask";

    [Range(1, 10_000)]
    public int PermitLimit { get; set; } = 4;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
