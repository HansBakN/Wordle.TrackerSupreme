namespace Wordle.TrackerSupreme.Api.Models.Game;

public record LeaderboardPageResponse(
    IReadOnlyList<LeaderboardEntryResponse> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages);
