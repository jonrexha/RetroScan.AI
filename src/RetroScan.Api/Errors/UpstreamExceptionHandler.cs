using Microsoft.AspNetCore.Diagnostics;

namespace RetroScan.Api.Errors;

public sealed class UpstreamExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<UpstreamExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            UpstreamServiceException upstream => (
                StatusCodes.Status502BadGateway,
                "Upstream service failed",
                $"{upstream.Service} did not answer. Try again in a moment."),
            LanguageModelNotConfiguredException => (
                StatusCodes.Status503ServiceUnavailable,
                "Language model not configured",
                "This server has no Groq API key, so it cannot answer questions."),
            _ => (0, "", ""),
        };

        if (status == 0)
        {
            return false;
        }

        logger.LogWarning(exception, "Returning {Status} for {Path}", status, httpContext.Request.Path);
        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title, Detail = detail },
        });
    }
}
