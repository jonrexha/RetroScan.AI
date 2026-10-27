namespace RetroScan.Api.Data;

public sealed class ResearchSession
{
    public const int TitleMaxLength = 120;

    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Title { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Question> Questions { get; set; } = [];
}
