using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Domain.Repositories;

public interface INytImportRepository
{
    Task AddSession(NytImportSession session, CancellationToken cancellationToken);
    Task<NytImportSession?> GetSession(Guid sessionId, CancellationToken cancellationToken);
    Task<NytImportSession?> GetSessionByCodeHash(string codeHash, CancellationToken cancellationToken);
    Task<bool> TryClaimSession(Guid sessionId, DateTime usedAt, CancellationToken cancellationToken);
    Task SaveChanges(CancellationToken cancellationToken);
}
