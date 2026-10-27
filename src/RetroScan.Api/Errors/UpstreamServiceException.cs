namespace RetroScan.Api.Errors;

public sealed class UpstreamServiceException(string service, string reason, Exception? innerException = null)
    : Exception($"{service} request failed: {reason}", innerException)
{
    public string Service { get; } = service;
}
