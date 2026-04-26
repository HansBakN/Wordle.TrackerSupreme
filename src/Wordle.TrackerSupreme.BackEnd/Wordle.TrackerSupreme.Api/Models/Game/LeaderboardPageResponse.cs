using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Api.Models.Game;

public record LeaderboardPageResponse(
    IReadOnlyList<PuzzleStream> Streams,
    IReadOnlyList<LeaderboardEntryResponse> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages);
