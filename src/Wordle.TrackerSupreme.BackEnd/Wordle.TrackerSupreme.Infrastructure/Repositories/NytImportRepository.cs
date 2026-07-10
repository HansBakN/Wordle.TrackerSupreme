using Microsoft.EntityFrameworkCore;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;
using Wordle.TrackerSupreme.Infrastructure.Database;

namespace Wordle.TrackerSupreme.Infrastructure.Repositories;

public class NytImportRepository(WordleTrackerSupremeDbContext dbContext) : INytImportRepository
{
    public Task AddSession(NytImportSession session, CancellationToken cancellationToken)
    {
        dbContext.NytImportSessions.Add(session);
        return Task.CompletedTask;
    }

    public Task<NytImportSession?> GetSession(Guid sessionId, CancellationToken cancellationToken)
        => dbContext.NytImportSessions.FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken);

    public Task<NytImportSession?> GetSessionByCodeHash(string codeHash, CancellationToken cancellationToken)
        => dbContext.NytImportSessions.FirstOrDefaultAsync(session => session.CodeHash == codeHash, cancellationToken);

    public async Task<bool> TryClaimSession(Guid sessionId, DateTime usedAt, CancellationToken cancellationToken)
        => await dbContext.NytImportSessions
            .Where(session => session.Id == sessionId && session.UsedAt == null && session.ExpiresAt > usedAt)
            .ExecuteUpdateAsync(update => update.SetProperty(session => session.UsedAt, usedAt), cancellationToken) == 1;

    public Task SaveChanges(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
