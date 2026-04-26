namespace Wordle.TrackerSupreme.Api.Models.Admin;

public record AdminPlayersPageResponse(
    IReadOnlyList<AdminPlayerSummaryResponse> Players,
    int TotalCount,
    int Page,
    int PageSize);
