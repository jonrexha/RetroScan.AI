using System.ComponentModel.DataAnnotations;

namespace RetroScan.Api.Sources;

public sealed class WikipediaOptions
{
    public const string SectionName = "Wikipedia";

    [Required]
    public Uri BaseUrl { get; set; } = new("https://en.wikipedia.org/");

    // Wikimedia rejects API clients without a descriptive User-Agent.
    [Required]
    public string UserAgent { get; set; } = "RetroScan.AI/2.0 (https://github.com/jonrexha/RetroScan.AI)";
}
