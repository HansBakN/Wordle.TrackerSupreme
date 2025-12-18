using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Domain.Services;

public class PlayerStatisticsService
{
    public PlayerStatistics Calculate(Player player)
    {
        var attempts = player.Attempts ?? [];

        var totalAttempts = attempts.Count;
        var wins = attempts.Count(a => a.Status == AttemptStatus.Solved);
        var failures = attempts.Count(a => a.Status == AttemptStatus.Failed);

        var guessCounts = attempts
            .Where(a => a.GuessCount.HasValue)
            .Select(a => (double)a.GuessCount!.Value)
            .ToList();

        double? averageGuesses = guessCounts.Count > 0
            ? guessCounts.Average()
            : null;

        var (currentStreak, longestStreak) = CalculateStreaks(attempts);

        return new PlayerStatistics
        {
            TotalAttempts = totalAttempts,
            Wins = wins,
            Failures = failures,
            CurrentStreak = currentStreak,
            LongestStreak = longestStreak,
            AverageGuessCount = averageGuesses,
        };
    }

    private static (int currentStreak, int longestStreak) CalculateStreaks(IEnumerable<PlayerPuzzleAttempt> attempts)
    {
        var orderedAttempts = attempts
            .Where(a => a.DailyPuzzle is not null)
            .OrderBy(a => a.DailyPuzzle!.PuzzleDate)
            .ToList();

        int streak = 0;
        int longest = 0;
        DateOnly? previousDate = null;

        foreach (var group in orderedAttempts.GroupBy(a => a.DailyPuzzle!.PuzzleDate))
        {
            if (previousDate is not null && group.Key > previousDate.Value.AddDays(1))
            {
                streak = 0;
            }

            var solvedToday = group.Any(a => a.Status == AttemptStatus.Solved);

            streak = solvedToday ? streak + 1 : 0;
            longest = Math.Max(longest, streak);
            previousDate = group.Key;
        }

        return (streak, longest);
    }
}
