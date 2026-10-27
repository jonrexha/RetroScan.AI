namespace RetroScan.Api.Data;

public sealed class Question
{
    public const int TextMaxLength = 1000;
    public const int SearchQueryMaxLength = 300;

    public long Id { get; set; }
    public Guid SessionId { get; set; }
    public string Text { get; set; } = "";
    public string SearchQuery { get; set; } = "";
    public string Answer { get; set; } = "";
    public DateTime AskedAt { get; set; }
    public List<Source> Sources { get; set; } = [];
}
