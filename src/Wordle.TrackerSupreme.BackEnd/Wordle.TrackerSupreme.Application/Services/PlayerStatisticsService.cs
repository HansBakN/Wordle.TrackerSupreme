using Wordle.TrackerSupreme.Domain.Models;

using Wordle.TrackerSupreme.Domain.Services;

namespace Wordle.TrackerSupreme.Application.Services;

public class PlayerStatisticsService : IPlayerStatisticsService
{
    public PlayerStatistics Calculate(
        Player player,
        PlayerStatisticsFilter? filter = null,
        Func<PlayerPuzzleAttempt, bool>? isAfterReveal = null)
    {
        var attempts = player.Attempts ?? [];
        var filterOptions = filter ?? new PlayerStatisticsFilter();
        var isAfterRevealFn = isAfterReveal ?? (_ => false);

        var filteredAttempts = attempts
            .Where(a => MatchesFilter(a, filterOptions, isAfterRevealFn))
            .ToList();

        var practiceAttempts = filteredAttempts.Where(a => isAfterRevealFn(a)).ToList();
        var countedAttempts = filterOptions.CountPracticeAttempts
            ? filteredAttempts
            : filteredAttempts.Where(a => !isAfterRevealFn(a)).ToList();

        var totalAttempts = countedAttempts.Count;
        var wins = countedAttempts.Count(a => a.Status == AttemptStatus.Solved);
        var failures = countedAttempts.Count(a => a.Status == AttemptStatus.Failed);

        var guessCounts = countedAttempts
            .Where(a => a.GuessCount.HasValue)
            .Select(a => (double)a.GuessCount!.Value)
            .ToList();

        double? averageGuesses = guessCounts.Count > 0
            ? guessCounts.Average()
            : null;

        var (currentStreak, longestStreak) = CalculateStreaks(countedAttempts);

        return new PlayerStatistics
        {
            TotalAttempts = totalAttempts,
            Wins = wins,
            Failures = failures,
            CurrentStreak = currentStreak,
            LongestStreak = longestStreak,
            AverageGuessCount = averageGuesses,
            PracticeAttempts = practiceAttempts.Count
        };
    }

    private static bool MatchesFilter(
        PlayerPuzzleAttempt attempt,
        PlayerStatisticsFilter filter,
        Func<PlayerPuzzleAttempt, bool> isAfterReveal)
    {
        var isAfter = isAfterReveal(attempt);
        if (isAfter)
        {
            if (!filter.IncludeAfterReveal)
            {
                return false;
            }
        }
        else if (!filter.IncludeBeforeReveal)
        {
            return false;
        }

        if (attempt.PlayedInHardMode)
        {
            if (!filter.IncludeHardMode)
            {
                return false;
            }
        }
        else if (!filter.IncludeEasyMode)
        {
            return false;
        }

        if (attempt.Status == AttemptStatus.Solved)
        {
            if (!filter.IncludeSolved)
            {
                return false;
            }
        }
        else if (attempt.Status == AttemptStatus.Failed)
        {
            if (!filter.IncludeFailed)
            {
                return false;
            }
        }
        else if (!filter.IncludeInProgress)
        {
            return false;
        }

        var puzzleDate = attempt.DailyPuzzle?.PuzzleDate ?? DateOnly.FromDateTime(attempt.CreatedOn);
        if (filter.FromDate.HasValue && puzzleDate < filter.FromDate.Value)
        {
            return false;
        }
        if (filter.ToDate.HasValue && puzzleDate > filter.ToDate.Value)
        {
            return false;
        }

        var guessCount = attempt.GuessCount ?? 0;
        if (filter.MinGuessCount.HasValue && guessCount < filter.MinGuessCount.Value)
        {
            return false;
        }
        if (filter.MaxGuessCount.HasValue && guessCount > filter.MaxGuessCount.Value)
        {
            return false;
        }

        return true;
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
