namespace RetroScan.Api.Data;

public sealed class Source
{
    public const int TitleMaxLength = 300;
    public const int UrlMaxLength = 2048;

    public long Id { get; set; }
    public long QuestionId { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string Excerpt { get; set; } = "";
}
