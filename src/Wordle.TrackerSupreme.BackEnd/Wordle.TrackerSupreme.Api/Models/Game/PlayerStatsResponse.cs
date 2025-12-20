namespace Wordle.TrackerSupreme.Api.Models.Game;

public record PlayerStatsResponse(
    int TotalAttempts,
    int Wins,
    int Failures,
    int PracticeAttempts,
    int CurrentStreak,
    int LongestStreak,
    double? AverageGuessCount);
