using RetroScan.Api.Data;

namespace RetroScan.Api.Contracts;

public sealed record SessionSummaryResponse(
    Guid Id,
    string Title,
    int QuestionCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record SessionResponse(
    Guid Id,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<QuestionResponse> Questions);

public sealed record QuestionResponse(
    long Id,
    string Text,
    string SearchQuery,
    string Answer,
    DateTime AskedAt,
    IReadOnlyList<SourceResponse> Sources);

public sealed record SourceResponse(int Number, string Title, string Url, string Excerpt);

public static class ResponseMapping
{
    public static SessionResponse ToResponse(this ResearchSession session) =>
        new(session.Id,
            session.Title,
            session.CreatedAt,
            session.UpdatedAt,
            session.Questions.Select(question => question.ToResponse()).ToList());

    public static QuestionResponse ToResponse(this Question question) =>
        new(question.Id,
            question.Text,
            question.SearchQuery,
            question.Answer,
            question.AskedAt,
            question.Sources
                .Select(source => new SourceResponse(source.Number, source.Title, source.Url, source.Excerpt))
                .ToList());
}
