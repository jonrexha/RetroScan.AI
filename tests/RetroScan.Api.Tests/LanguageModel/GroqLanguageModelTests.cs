using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RetroScan.Api.Errors;
using RetroScan.Api.LanguageModel;

namespace RetroScan.Api.Tests.LanguageModel;

public sealed class GroqLanguageModelTests
{
    private const string Completion = """{"choices":[{"message":{"role":"assistant","content":"  Sharks are fish [1].  "}}]}""";

    private static readonly IReadOnlyList<ChatMessage> Messages =
        [ChatMessage.System("Be brief."), ChatMessage.User("What are sharks?")];

    private static GroqLanguageModel Model(StubHttpHandler handler, string? apiKey = "test-key", string? reasoningEffort = "low") =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://groq.test/openai/v1/") },
            Options.Create(new GroqOptions { ApiKey = apiKey, Model = "test-model", ReasoningEffort = reasoningEffort }),
            NullLogger<GroqLanguageModel>.Instance);

    [Fact]
    public async Task Posts_the_conversation_to_chat_completions_with_the_api_key()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, Completion);

        await Model(handler).CompleteAsync(Messages, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://groq.test/openai/v1/chat/completions", request.RequestUri!.AbsoluteUri);
        Assert.Equal("Bearer test-key", request.Headers.Authorization!.ToString());

        using var body = JsonDocument.Parse(handler.RequestBodies[0]);
        Assert.Equal("test-model", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("low", body.RootElement.GetProperty("reasoning_effort").GetString());
        Assert.False(body.RootElement.GetProperty("include_reasoning").GetBoolean());
        var messages = body.RootElement.GetProperty("messages");
        Assert.Equal(["system", "user"], messages.EnumerateArray().Select(m => m.GetProperty("role").GetString()));
        Assert.Equal("What are sharks?", messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task Leaves_out_the_reasoning_parameters_when_no_effort_is_configured()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, Completion);

        await Model(handler, reasoningEffort: null).CompleteAsync(Messages, CancellationToken.None);

        using var body = JsonDocument.Parse(handler.RequestBodies[0]);
        Assert.False(body.RootElement.TryGetProperty("reasoning_effort", out _));
        Assert.False(body.RootElement.TryGetProperty("include_reasoning", out _));
    }

    [Fact]
    public async Task Returns_the_trimmed_answer()
    {
        var answer = await Model(new StubHttpHandler(HttpStatusCode.OK, Completion)).CompleteAsync(Messages, CancellationToken.None);

        Assert.Equal("Sharks are fish [1].", answer);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, """{"error":{"message":"rate limit"}}""")]
    [InlineData(HttpStatusCode.OK, """{"choices":[{"message":{"content":""}}]}""")]
    [InlineData(HttpStatusCode.OK, "not json")]
    public async Task Reports_an_upstream_failure_for_errors_and_empty_answers(HttpStatusCode status, string body)
    {
        var model = Model(new StubHttpHandler(status, body));

        var exception = await Assert.ThrowsAsync<UpstreamServiceException>(
            () => model.CompleteAsync(Messages, CancellationToken.None));
        Assert.Equal("Groq", exception.Service);
    }

    [Fact]
    public async Task Refuses_without_an_api_key_and_sends_nothing()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, Completion);

        await Assert.ThrowsAsync<LanguageModelNotConfiguredException>(
            () => Model(handler, apiKey: " ").CompleteAsync(Messages, CancellationToken.None));
        Assert.Empty(handler.Requests);
    }
}
