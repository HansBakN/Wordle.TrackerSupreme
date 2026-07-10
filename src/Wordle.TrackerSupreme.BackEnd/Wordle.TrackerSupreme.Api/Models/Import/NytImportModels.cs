using System.ComponentModel.DataAnnotations;

namespace Wordle.TrackerSupreme.Api.Models.Import;

public sealed record NytImportSessionResponse(Guid SessionId, string Code, DateTime ExpiresAt);
public sealed record NytImportSessionStatusResponse(Guid SessionId, DateTime ExpiresAt, DateTime? CompletedAt,
    int? AggregateGamesPlayed, int Requested, int Imported, int Duplicates, int Conflicts, int Rejected, int MissingFromNyt);
public sealed record NytPuzzleCatalogueEntryResponse(int NytPuzzleId, DateOnly PrintDate, int? PublicPuzzleNumber);
public sealed record NytImportSubmissionRequest(int SchemaVersion, [Required] string ImportCode, int? AggregateGamesPlayed,
    [Required] IReadOnlyList<NytImportStateRequest?> States);
public sealed record NytImportStateRequest(int? NytPuzzleId, DateOnly? PrintDate, long? Timestamp, string? Status,
    bool HardMode, bool IsPlayingArchive, IReadOnlyList<string?>? BoardState);
public sealed record NytImportResponse(int Requested, int Imported, int Duplicates, int Conflicts, int Rejected,
    int MissingFromNyt, IReadOnlyList<NytImportRecordResponse> Results);
public sealed record NytImportRecordResponse(int NytPuzzleId, DateOnly PrintDate, string Outcome, string? Reason);
