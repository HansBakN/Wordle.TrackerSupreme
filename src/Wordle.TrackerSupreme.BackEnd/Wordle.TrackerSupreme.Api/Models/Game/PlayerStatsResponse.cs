using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Api.Models.Game;

public record PlayerStatsResponse(
    IReadOnlyList<PuzzleStream> Streams,
    int TotalAttempts,
    int Wins,
    int Failures,
    int PracticeAttempts,
    int CurrentStreak,
    int LongestStreak,
    double? AverageGuessCount,
    IReadOnlyDictionary<int, int> GuessDistribution);
