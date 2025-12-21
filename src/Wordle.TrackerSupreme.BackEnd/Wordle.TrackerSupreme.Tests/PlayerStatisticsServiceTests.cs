using FluentAssertions;
using Wordle.TrackerSupreme.Application.Services;
using Wordle.TrackerSupreme.Domain.Models;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class PlayerStatisticsServiceTests
{
    [Fact]
    public void Calculate_filters_by_hard_mode_and_status()
    {
        var player = CreatePlayer();
        player.Attempts =
        [
            CreateAttempt(player, new DateOnly(2025, 1, 1), AttemptStatus.Solved, true, 3),
            CreateAttempt(player, new DateOnly(2025, 1, 2), AttemptStatus.Failed, false, 4)
        ];

        var filter = new PlayerStatisticsFilter
        {
            IncludeHardMode = true,
            IncludeEasyMode = false,
            IncludeSolved = true,
            IncludeFailed = false,
            IncludeInProgress = false
        };

        var service = new PlayerStatisticsService();
        var stats = service.Calculate(player, filter, _ => false);

        stats.TotalAttempts.Should().Be(1);
        stats.Wins.Should().Be(1);
        stats.Failures.Should().Be(0);
    }

    [Fact]
    public void Calculate_excludes_after_reveal_when_disabled()
    {
        var afterRevealId = Guid.NewGuid();
        var beforeRevealId = Guid.NewGuid();
        var player = CreatePlayer();
        player.Attempts =
        [
            CreateAttempt(player, new DateOnly(2025, 1, 3), AttemptStatus.Solved, true, 2, beforeRevealId),
            CreateAttempt(player, new DateOnly(2025, 1, 4), AttemptStatus.Solved, true, 4, afterRevealId)
        ];

        var filter = new PlayerStatisticsFilter
        {
            IncludeBeforeReveal = true,
            IncludeAfterReveal = false
        };

        var service = new PlayerStatisticsService();
        var stats = service.Calculate(player, filter, attempt => attempt.Id == afterRevealId);

        stats.TotalAttempts.Should().Be(1);
        stats.PracticeAttempts.Should().Be(0);
    }

    [Fact]
    public void Calculate_counts_practice_attempts_when_enabled()
    {
        var afterRevealId = Guid.NewGuid();
        var player = CreatePlayer();
        player.Attempts =
        [
            CreateAttempt(player, new DateOnly(2025, 1, 5), AttemptStatus.Solved, true, 2),
            CreateAttempt(player, new DateOnly(2025, 1, 6), AttemptStatus.Failed, true, 5, afterRevealId)
        ];

        var filter = new PlayerStatisticsFilter
        {
            IncludeAfterReveal = true,
            CountPracticeAttempts = true
        };

        var service = new PlayerStatisticsService();
        var stats = service.Calculate(player, filter, attempt => attempt.Id == afterRevealId);

        stats.TotalAttempts.Should().Be(2);
        stats.PracticeAttempts.Should().Be(1);
    }

    [Fact]
    public void Calculate_filters_by_guess_count_range()
    {
        var player = CreatePlayer();
        player.Attempts =
        [
            CreateAttempt(player, new DateOnly(2025, 1, 7), AttemptStatus.Solved, true, 2),
            CreateAttempt(player, new DateOnly(2025, 1, 8), AttemptStatus.Solved, true, 5)
        ];

        var filter = new PlayerStatisticsFilter
        {
            MinGuessCount = 3,
            MaxGuessCount = 5
        };

        var service = new PlayerStatisticsService();
        var stats = service.Calculate(player, filter, _ => false);

        stats.TotalAttempts.Should().Be(1);
        stats.Wins.Should().Be(1);
        stats.AverageGuessCount.Should().Be(5);
    }

    [Fact]
    public void Calculate_filters_by_date_range()
    {
        var player = CreatePlayer();
        player.Attempts =
        [
            CreateAttempt(player, new DateOnly(2025, 1, 9), AttemptStatus.Solved, true, 3),
            CreateAttempt(player, new DateOnly(2025, 1, 12), AttemptStatus.Solved, true, 4)
        ];

        var filter = new PlayerStatisticsFilter
        {
            FromDate = new DateOnly(2025, 1, 10),
            ToDate = new DateOnly(2025, 1, 15)
        };

        var service = new PlayerStatisticsService();
        var stats = service.Calculate(player, filter, _ => false);

        stats.TotalAttempts.Should().Be(1);
        stats.Wins.Should().Be(1);
    }

    private static PlayerPuzzleAttempt CreateAttempt(
        Player player,
        DateOnly puzzleDate,
        AttemptStatus status,
        bool hardMode,
        int guessCount,
        Guid? id = null)
    {
        var puzzle = new DailyPuzzle { Id = Guid.NewGuid(), PuzzleDate = puzzleDate };
        var attempt = new PlayerPuzzleAttempt
        {
            Id = id ?? Guid.NewGuid(),
            Player = player,
            PlayerId = player.Id,
            DailyPuzzle = puzzle,
            DailyPuzzleId = puzzle.Id,
            Status = status,
            PlayedInHardMode = hardMode,
            CreatedOn = puzzleDate.ToDateTime(TimeOnly.MinValue),
            Guesses = []
        };

        attempt.Guesses = Enumerable.Range(1, guessCount)
            .Select(index => new GuessAttempt
            {
                Id = Guid.NewGuid(),
                GuessNumber = index,
                GuessWord = "CRANE",
                PlayerPuzzleAttempt = attempt
            })
            .ToList();

        return attempt;
    }

    private static Player CreatePlayer()
    {
        return new Player
        {
            Id = Guid.NewGuid(),
            DisplayName = "Tester",
            Email = "t@example.com",
            PasswordHash = "hash",
            Attempts = []
        };
    }
}
