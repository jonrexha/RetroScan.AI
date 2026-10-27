namespace RetroScan.Api.LanguageModel;

public interface ILanguageModel
{
    Task<string> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken);
}
