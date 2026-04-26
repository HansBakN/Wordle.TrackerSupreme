namespace Wordle.TrackerSupreme.Api.Models.Game;

public record TodayLeaderboardEntryResponse(
    int Rank,
    Guid PlayerId,
    string DisplayName,
    string Result,
    int GuessCount,
    bool PlayedInHardMode);
