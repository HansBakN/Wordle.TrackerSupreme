using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Infrastructure.Database;
using Wordle.TrackerSupreme.Infrastructure.Repositories;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class PlayerRepositoryTests
{
    [Fact]
    public async Task GetPlayersWithAttempts_includes_attempts_and_puzzles()
    {
        var options = new DbContextOptionsBuilder<WordleTrackerSupremeDbContext>()
            .UseInMemoryDatabase($"players-{Guid.NewGuid()}")
            .Options;

        var player = new Player
        {
            Id = Guid.NewGuid(),
            DisplayName = "Stat Tester",
            Email = "stats@example.com",
            PasswordHash = "hash",
            Attempts = []
        };
        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = new DateOnly(2025, 1, 3),
            Solution = "CRANE"
        };
        var attempt = new PlayerPuzzleAttempt
        {
            Id = Guid.NewGuid(),
            Player = player,
            PlayerId = player.Id,
            DailyPuzzle = puzzle,
            DailyPuzzleId = puzzle.Id,
            Status = AttemptStatus.Solved,
            PlayedInHardMode = true,
            CreatedOn = puzzle.PuzzleDate.ToDateTime(TimeOnly.MinValue)
        };

        player.Attempts.Add(attempt);
        puzzle.Attempts.Add(attempt);

        await using (var context = new WordleTrackerSupremeDbContext(options))
        {
            context.Add(player);
            context.Add(puzzle);
            await context.SaveChangesAsync();
        }

        await using var readContext = new WordleTrackerSupremeDbContext(options);
        var repository = new PlayerRepository(readContext);

        var players = await repository.GetPlayersWithAttempts(CancellationToken.None);

        players.Should().ContainSingle();
        var loaded = players.Single();
        loaded.Attempts.Should().ContainSingle();
        loaded.Attempts.First().DailyPuzzle.Should().NotBeNull();
    }
}
