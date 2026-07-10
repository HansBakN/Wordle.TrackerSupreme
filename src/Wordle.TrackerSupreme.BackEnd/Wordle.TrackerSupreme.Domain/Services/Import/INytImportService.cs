using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Domain.Services.Import;

public interface INytImportService
{
    Task<NytImportSessionCreated> CreateSession(Guid playerId, CancellationToken cancellationToken);
    Task<NytImportSessionStatus?> GetSession(Guid playerId, Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<NytPuzzleMetadata>> GetPublishedCatalogue(string importCode, CancellationToken cancellationToken);
    Task<NytImportResult> Import(NytImportRequest request, CancellationToken cancellationToken);
}

public sealed record NytImportSessionCreated(Guid SessionId, string Code, DateTime ExpiresAt);

public sealed record NytImportSessionStatus(
    Guid SessionId,
    DateTime ExpiresAt,
    DateTime? CompletedAt,
    int? AggregateGamesPlayed,
    int Requested,
    int Imported,
    int Duplicates,
    int Conflicts,
    int Rejected,
    int MissingFromNyt);

public sealed record NytImportRequest(int SchemaVersion, string ImportCode, int? AggregateGamesPlayed, IReadOnlyList<NytImportedState?> States);

public sealed record NytImportedState(
    int? NytPuzzleId,
    DateOnly? PrintDate,
    long? Timestamp,
    string? Status,
    bool HardMode,
    bool IsPlayingArchive,
    IReadOnlyList<string?>? BoardState);

public sealed record NytImportResult(
    int Requested,
    int Imported,
    int Duplicates,
    int Conflicts,
    int Rejected,
    int MissingFromNyt,
    IReadOnlyList<NytImportRecordResult> Results);

public sealed record NytImportRecordResult(int NytPuzzleId, DateOnly PrintDate, string Outcome, string? Reason = null);
