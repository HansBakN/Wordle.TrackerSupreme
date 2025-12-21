namespace Wordle.TrackerSupreme.Api.Models.Game;

public record LeaderboardEntryResponse(
    int Rank,
    Guid PlayerId,
    string DisplayName,
    int TotalAttempts,
    int Wins,
    int Failures,
    int CurrentStreak,
    int LongestStreak,
    int PracticeAttempts,
    double? AverageGuessCount,
    double? WinRate);
