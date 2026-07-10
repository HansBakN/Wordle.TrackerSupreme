using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;

namespace Wordle.TrackerSupreme.Tests.Fakes;

public class FakeNytImportRepository : INytImportRepository
{
    private readonly List<NytImportSession> _sessions = [];
    public IReadOnlyList<NytImportSession> Sessions => _sessions;

    public Task AddSession(NytImportSession session, CancellationToken cancellationToken)
    {
        _sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task<NytImportSession?> GetSession(Guid sessionId, CancellationToken cancellationToken)
        => Task.FromResult(_sessions.SingleOrDefault(session => session.Id == sessionId));

    public Task<NytImportSession?> GetSessionByCodeHash(string codeHash, CancellationToken cancellationToken)
        => Task.FromResult(_sessions.SingleOrDefault(session => session.CodeHash == codeHash));

    public Task<bool> TryClaimSession(Guid sessionId, DateTime usedAt, CancellationToken cancellationToken)
    {
        var session = _sessions.SingleOrDefault(item => item.Id == sessionId);
        if (session is null || session.UsedAt is not null || session.ExpiresAt <= usedAt)
        {
            return Task.FromResult(false);
        }
        session.UsedAt = usedAt;
        return Task.FromResult(true);
    }

    public Task SaveChanges(CancellationToken cancellationToken) => Task.CompletedTask;
}
