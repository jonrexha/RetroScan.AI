using Microsoft.EntityFrameworkCore;
using RetroScan.Api.Contracts;
using RetroScan.Api.Data;

namespace RetroScan.Api.Research;

public sealed class SessionService(RetroScanDbContext db, TimeProvider clock)
{
    public const int ListLimit = 50;

    public async Task<IReadOnlyList<SessionSummaryResponse>> ListAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await db.Sessions
            .Where(session => session.OwnerId == ownerId)
            .OrderByDescending(session => session.UpdatedAt)
            .Take(ListLimit)
            .Select(session => new SessionSummaryResponse(
                session.Id,
                session.Title,
                session.Questions.Count,
                session.CreatedAt,
                session.UpdatedAt))
            .ToListAsync(cancellationToken);

    public async Task<SessionResponse> CreateAsync(Guid ownerId, string? title, CancellationToken cancellationToken)
    {
        var now = clock.UtcNowForDatabase();
        var session = new ResearchSession
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            Title = title?.Trim() ?? "",
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Sessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return session.ToResponse();
    }

    public async Task<SessionResponse?> GetAsync(Guid ownerId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await db.Sessions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Questions.OrderBy(question => question.Id))
            .ThenInclude(question => question.Sources.OrderBy(source => source.Number))
            .SingleOrDefaultAsync(s => s.Id == sessionId && s.OwnerId == ownerId, cancellationToken);

        return session?.ToResponse();
    }

    public async Task<bool> DeleteAsync(Guid ownerId, Guid sessionId, CancellationToken cancellationToken) =>
        await db.Sessions
            .Where(session => session.Id == sessionId && session.OwnerId == ownerId)
            .ExecuteDeleteAsync(cancellationToken) > 0;
}
