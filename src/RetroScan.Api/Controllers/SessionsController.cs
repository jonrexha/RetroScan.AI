using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using RetroScan.Api.Contracts;
using RetroScan.Api.Owners;
using RetroScan.Api.Research;

namespace RetroScan.Api.Controllers;

[ApiController]
[Route("api/sessions")]
[Produces("application/json")]
public sealed class SessionsController(SessionService sessions, ResearchService research) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<SessionSummaryResponse>> List(CancellationToken cancellationToken) =>
        sessions.ListAsync(HttpContext.GetOwnerId(), cancellationToken);

    [HttpPost]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SessionResponse>> Create(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CreateSessionRequest? request,
        CancellationToken cancellationToken)
    {
        var session = await sessions.CreateAsync(HttpContext.GetOwnerId(), request?.Title, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = session.Id }, session);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var session = await sessions.GetAsync(HttpContext.GetOwnerId(), id, cancellationToken);
        return session is null ? NotFound() : session;
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await sessions.DeleteAsync(HttpContext.GetOwnerId(), id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/questions")]
    [EnableRateLimiting(AskRateLimitOptions.PolicyName)]
    [ProducesResponseType<QuestionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<QuestionResponse>> Ask(Guid id, AskRequest request, CancellationToken cancellationToken)
    {
        var question = await research.AskAsync(HttpContext.GetOwnerId(), id, request.Text, cancellationToken);
        return question is null ? NotFound() : question;
    }
}
