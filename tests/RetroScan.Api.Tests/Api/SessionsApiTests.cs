using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RetroScan.Api.Contracts;
using RetroScan.Api.Data;
using RetroScan.Api.Errors;

namespace RetroScan.Api.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class SessionsApiTests
{
    private readonly RetroScanApiFactory _factory;

    public SessionsApiTests(ApiFixture fixture)
    {
        _factory = fixture.Factory;
        _factory.LanguageModel.Reset();
        _factory.SourceSearch.Reset();
    }

    // Each client has its own cookie jar, so each one is a separate visitor with its own sessions.
    private HttpClient NewVisitor() => _factory.CreateClient();

    private static async Task<SessionResponse> CreateSession(HttpClient visitor)
    {
        var response = await visitor.PostAsJsonAsync("/api/sessions", new { });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionResponse>())!;
    }

    private static Task<HttpResponseMessage> Ask(HttpClient visitor, Guid sessionId, string text) =>
        visitor.PostAsJsonAsync($"/api/sessions/{sessionId}/questions", new { text });

    [Fact]
    public async Task Creating_a_session_returns_201_with_its_location()
    {
        var response = await NewVisitor().PostAsJsonAsync("/api/sessions", new { title = "Deep sea" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = (await response.Content.ReadFromJsonAsync<SessionResponse>())!;
        Assert.Equal("Deep sea", session.Title);
        Assert.Empty(session.Questions);
        Assert.Equal($"/api/sessions/{session.Id}", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task Asking_answers_from_the_sources_and_stores_the_question()
    {
        var visitor = NewVisitor();
        var session = await CreateSession(visitor);

        var response = await Ask(visitor, session.Id, "  What are sharks?  ");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var question = (await response.Content.ReadFromJsonAsync<QuestionResponse>())!;
        Assert.Equal("What are sharks?", question.Text);
        Assert.Equal(FakeLanguageModel.SearchQuery, question.SearchQuery);
        Assert.Equal(FakeLanguageModel.Answer, question.Answer);
        Assert.Equal([1, 2], question.Sources.Select(source => source.Number));
        Assert.Equal("https://en.wikipedia.org/wiki/Whale_shark", question.Sources[1].Url);
        Assert.Equal([FakeLanguageModel.SearchQuery], _factory.SourceSearch.Queries);

        var stored = (await visitor.GetFromJsonAsync<SessionResponse>($"/api/sessions/{session.Id}"))!;
        Assert.Equal("What are sharks?", stored.Title);
        Assert.Equivalent(question, Assert.Single(stored.Questions));
    }

    [Fact]
    public async Task Follow_up_questions_send_the_earlier_turns_to_the_model()
    {
        var visitor = NewVisitor();
        var session = await CreateSession(visitor);
        await Ask(visitor, session.Id, "What are sharks?");

        await Ask(visitor, session.Id, "How long do they live?");

        var searchPrompt = _factory.LanguageModel.Calls[^2];
        Assert.Contains("- What are sharks?", searchPrompt[^1].Content);
        var answerPrompt = _factory.LanguageModel.Calls[^1];
        Assert.Equal(["system", "user", "assistant", "user"], answerPrompt.Select(message => message.Role));
        Assert.Equal("How long do they live?", answerPrompt[^1].Content);
    }

    [Fact]
    public async Task A_search_without_results_answers_without_asking_the_model_again()
    {
        _factory.SourceSearch.Results = [];
        var visitor = NewVisitor();
        var session = await CreateSession(visitor);

        var question = (await (await Ask(visitor, session.Id, "qwxzzy?")).Content.ReadFromJsonAsync<QuestionResponse>())!;

        Assert.StartsWith("No Wikipedia articles matched", question.Answer);
        Assert.Empty(question.Sources);
        Assert.Single(_factory.LanguageModel.Calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Blank_questions_are_rejected(string? text)
    {
        var visitor = NewVisitor();
        var session = await CreateSession(visitor);

        var response = await visitor.PostAsJsonAsync($"/api/sessions/{session.Id}/questions", new { text });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
        Assert.Contains("Text", problem.Errors.Keys);
        Assert.Empty(_factory.LanguageModel.Calls);
    }

    [Fact]
    public async Task Questions_over_a_thousand_characters_are_rejected()
    {
        var visitor = NewVisitor();
        var session = await CreateSession(visitor);

        var response = await Ask(visitor, session.Id, new string('a', 1001));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_sessions_return_404()
    {
        var visitor = NewVisitor();
        var unknown = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/sessions/{unknown}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.DeleteAsync($"/api/sessions/{unknown}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Ask(visitor, unknown, "What are sharks?")).StatusCode);
    }

    [Fact]
    public async Task Sessions_are_private_to_the_visitor_who_created_them()
    {
        var owner = NewVisitor();
        var stranger = NewVisitor();
        var session = await CreateSession(owner);

        var strangersSessions = (await stranger.GetFromJsonAsync<List<SessionSummaryResponse>>("/api/sessions"))!;
        Assert.DoesNotContain(strangersSessions, summary => summary.Id == session.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/sessions/{session.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Ask(stranger, session.Id, "What are sharks?")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.DeleteAsync($"/api/sessions/{session.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/sessions/{session.Id}")).StatusCode);
    }

    [Fact]
    public async Task The_list_shows_the_most_recently_used_session_first_with_question_counts()
    {
        var visitor = NewVisitor();
        var older = await CreateSession(visitor);
        var newer = await CreateSession(visitor);
        await Ask(visitor, older.Id, "What are sharks?");

        var sessions = (await visitor.GetFromJsonAsync<List<SessionSummaryResponse>>("/api/sessions"))!;

        Assert.Equal([older.Id, newer.Id], sessions.Select(session => session.Id));
        Assert.Equal([1, 0], sessions.Select(session => session.QuestionCount));
    }

    [Fact]
    public async Task Deleting_a_session_removes_its_questions_and_sources()
    {
        var visitor = NewVisitor();
        var session = await CreateSession(visitor);
        var question = (await (await Ask(visitor, session.Id, "What are sharks?")).Content.ReadFromJsonAsync<QuestionResponse>())!;

        Assert.Equal(HttpStatusCode.NoContent, (await visitor.DeleteAsync($"/api/sessions/{session.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/sessions/{session.Id}")).StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RetroScanDbContext>();
        Assert.False(await db.Questions.AnyAsync(q => q.SessionId == session.Id));
        Assert.False(await db.Sources.AnyAsync(source => source.QuestionId == question.Id));
    }

    [Fact]
    public async Task A_failing_model_returns_502_and_stores_nothing()
    {
        _factory.LanguageModel.Failure = new UpstreamServiceException("Groq", "status 500");
        var visitor = NewVisitor();
        var session = await CreateSession(visitor);

        var response = await Ask(visitor, session.Id, "What are sharks?");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
        Assert.Equal("Groq did not answer. Try again in a moment.", problem.Detail);
        var stored = (await visitor.GetFromJsonAsync<SessionResponse>($"/api/sessions/{session.Id}"))!;
        Assert.Empty(stored.Questions);
        Assert.Equal("", stored.Title);
    }

    [Fact]
    public async Task A_server_without_a_groq_key_returns_503()
    {
        _factory.LanguageModel.Failure = new LanguageModelNotConfiguredException();
        var visitor = NewVisitor();
        var session = await CreateSession(visitor);

        var response = await Ask(visitor, session.Id, "What are sharks?");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Asking_too_often_returns_429()
    {
        await using var limited = _factory.WithWebHostBuilder(builder => builder.UseSetting("RateLimit:PermitLimit", "2"));
        var visitor = limited.CreateClient();
        var session = await CreateSession(visitor);

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            statuses.Add((await Ask(visitor, session.Id, "What are sharks?")).StatusCode);
        }

        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests], statuses);
    }

    [Fact]
    public async Task Serves_the_health_check_and_the_terminal_page()
    {
        var visitor = NewVisitor();

        Assert.Equal("Healthy", await visitor.GetStringAsync("/health"));
        var page = await visitor.GetAsync("/");
        Assert.Equal("text/html", page.Content.Headers.ContentType!.MediaType);
        Assert.Contains("RETROSCAN", await page.Content.ReadAsStringAsync());
    }
}
