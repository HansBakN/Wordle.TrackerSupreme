namespace Wordle.TrackerSupreme.Api.Models.Game;

public record PlayerStatsEntryResponse(
    Guid PlayerId,
    string DisplayName,
    PlayerStatsResponse Stats);
