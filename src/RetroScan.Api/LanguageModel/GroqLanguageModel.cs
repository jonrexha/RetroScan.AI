using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RetroScan.Api.Errors;

namespace RetroScan.Api.LanguageModel;

public sealed class GroqLanguageModel(
    HttpClient http,
    IOptions<GroqOptions> options,
    ILogger<GroqLanguageModel> logger) : ILanguageModel
{
    private const string ServiceName = "Groq";

    public async Task<string> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var groq = options.Value;
        if (string.IsNullOrWhiteSpace(groq.ApiKey))
        {
            throw new LanguageModelNotConfiguredException();
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", groq.ApiKey);
        var reasoningEffort = string.IsNullOrEmpty(groq.ReasoningEffort) ? null : groq.ReasoningEffort;
        request.Content = JsonContent.Create(new CompletionRequest(
            groq.Model,
            messages,
            groq.Temperature,
            groq.MaxCompletionTokens,
            reasoningEffort,
            IncludeReasoning: reasoningEffort is null ? null : false));

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Groq returned {StatusCode}: {Error}", (int)response.StatusCode, error);
                throw new UpstreamServiceException(ServiceName, $"status {(int)response.StatusCode}");
            }

            var completion = await response.Content.ReadFromJsonAsync<CompletionResponse>(cancellationToken);
            var content = completion?.Choices.FirstOrDefault()?.Message.Content;
            return string.IsNullOrWhiteSpace(content)
                ? throw new UpstreamServiceException(ServiceName, "empty completion")
                : content.Trim();
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException
                                          || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new UpstreamServiceException(ServiceName, exception.GetType().Name, exception);
        }
    }

    private sealed record CompletionRequest(
        string Model,
        IReadOnlyList<ChatMessage> Messages,
        double Temperature,
        [property: JsonPropertyName("max_completion_tokens")] int MaxCompletionTokens,
        [property: JsonPropertyName("reasoning_effort"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? ReasoningEffort,
        [property: JsonPropertyName("include_reasoning"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        bool? IncludeReasoning);

    private sealed record CompletionResponse(IReadOnlyList<Choice> Choices);

    private sealed record Choice(ChoiceMessage Message);

    private sealed record ChoiceMessage(string? Content);
}
