namespace RetroScan.Api.Sources;

public interface ISourceSearch
{
    Task<IReadOnlyList<SourceDocument>> SearchAsync(string query, int limit, CancellationToken cancellationToken);
}
