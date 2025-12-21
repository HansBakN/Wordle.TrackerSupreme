using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Infrastructure.Database;
using Wordle.TrackerSupreme.Seeder;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class SeederRunnerTests
{
    [Fact]
    public async Task SeedAsync_populates_data_and_is_idempotent()
    {
        var options = new SeederOptions
        {
            RandomSeed = 4242,
            PlayerCount = 3,
            MinSolvedPuzzles = 3,
            MaxSolvedPuzzles = 3,
            FailedPuzzlesMin = 1,
            FailedPuzzlesMax = 1,
            InProgressPuzzlesMin = 1,
            InProgressPuzzlesMax = 1,
            PuzzleDays = 10,
            AnchorDate = "2025-02-01"
        };
        var gameOptions = new GameOptions { MaxGuesses = 6, WordLength = 5 };
        var dbOptions = new DbContextOptionsBuilder<WordleTrackerSupremeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new WordleTrackerSupremeDbContext(dbOptions);
        var wordSelector = new WordSelector();
        var guessEvaluator = new GuessEvaluationService(gameOptions);
        var passwordHasher = new PasswordHasher<Player>();
        var generator = new SeedDataGenerator(options, gameOptions, wordSelector, guessEvaluator, passwordHasher);
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SeederRunner>();
        var runner = new SeederRunner(dbContext, options, generator, logger);

        await runner.SeedAsync();

        dbContext.Players.Should().HaveCount(options.PlayerCount);
        dbContext.DailyPuzzles.Should().HaveCount(options.PuzzleDays);
        dbContext.Attempts.Should().HaveCount(options.PlayerCount * 5);

        var attempts = await dbContext.Attempts
            .Include(a => a.DailyPuzzle)
            .Include(a => a.Guesses)
            .ThenInclude(g => g.Feedback)
            .ToListAsync();

        attempts.Should().NotBeEmpty();
        foreach (var attempt in attempts)
        {
            attempt.Guesses.Should().NotBeEmpty();
            attempt.Guesses.Should().OnlyContain(g => g.GuessWord.Length == gameOptions.WordLength);
            attempt.Guesses.Should().OnlyContain(g => g.Feedback.Count == gameOptions.WordLength);

            if (attempt.Status == AttemptStatus.Solved)
            {
                attempt.Guesses.Last().GuessWord.Should().Be(attempt.DailyPuzzle!.Solution);
            }
            else
            {
                attempt.Guesses.Should().OnlyContain(g => g.GuessWord != attempt.DailyPuzzle!.Solution);
            }
        }

        await runner.SeedAsync();

        dbContext.Players.Should().HaveCount(options.PlayerCount);
        dbContext.Attempts.Should().HaveCount(options.PlayerCount * 5);
    }
}
