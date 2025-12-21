using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
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
        var wordList = new WordListValidator();
        var guessEvaluator = new GuessEvaluationService(gameOptions, wordList);
        var passwordHasher = new PasswordHasher<Player>();
        var generator = new SeedDataGenerator(options, gameOptions, wordSelector, wordList, guessEvaluator, passwordHasher);
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SeederRunner>();
        var environment = new TestHostEnvironment("Development");
        var runner = new SeederRunner(dbContext, options, generator, logger, environment);

        await runner.SeedAsync();

        dbContext.Players.Should().HaveCount(options.PlayerCount);
        dbContext.DailyPuzzles.Should().HaveCount(options.PuzzleDays);
        var featuredCount = Math.Min(3, options.PlayerCount);
        dbContext.Attempts.Should().HaveCount(options.PlayerCount * 5 + featuredCount);

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
        dbContext.Attempts.Should().HaveCount(options.PlayerCount * 5 + featuredCount);
    }

    [Fact]
    public async Task SeedAsync_resets_database_when_enabled()
    {
        var options = new SeederOptions
        {
            RandomSeed = 4242,
            PlayerCount = 2,
            MinSolvedPuzzles = 2,
            MaxSolvedPuzzles = 2,
            FailedPuzzlesMin = 1,
            FailedPuzzlesMax = 1,
            InProgressPuzzlesMin = 1,
            InProgressPuzzlesMax = 1,
            PuzzleDays = 5,
            AnchorDate = "2025-02-01",
            AllowReseed = true,
            ResetDatabase = true
        };
        var gameOptions = new GameOptions { MaxGuesses = 6, WordLength = 5 };
        var dbOptions = new DbContextOptionsBuilder<WordleTrackerSupremeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new WordleTrackerSupremeDbContext(dbOptions);
        var wordSelector = new WordSelector();
        var wordList = new WordListValidator();
        var guessEvaluator = new GuessEvaluationService(gameOptions, wordList);
        var passwordHasher = new PasswordHasher<Player>();
        var generator = new SeedDataGenerator(options, gameOptions, wordSelector, wordList, guessEvaluator, passwordHasher);
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SeederRunner>();
        var environment = new TestHostEnvironment("Development");
        var runner = new SeederRunner(dbContext, options, generator, logger, environment);
        var originalReset = Environment.GetEnvironmentVariable("E2E_RESET_ENABLED");

        Environment.SetEnvironmentVariable("E2E_RESET_ENABLED", "true");
        try
        {
            await runner.SeedAsync();
            await runner.SeedAsync();
        }
        finally
        {
            Environment.SetEnvironmentVariable("E2E_RESET_ENABLED", originalReset);
        }

        var featuredCount = Math.Min(3, options.PlayerCount);
        var expectedPerPlayer = Math.Min(
            options.PuzzleDays,
            options.MinSolvedPuzzles + options.FailedPuzzlesMin + options.InProgressPuzzlesMin);
        dbContext.Players.Count().Should().Be(options.PlayerCount);
        dbContext.Attempts.Count().Should().Be(options.PlayerCount * expectedPerPlayer + featuredCount);
    }

    [Fact]
    public async Task SeedAsync_throws_in_production_when_reset_is_enabled()
    {
        var options = new SeederOptions
        {
            RandomSeed = 4242,
            PlayerCount = 1,
            MinSolvedPuzzles = 1,
            MaxSolvedPuzzles = 1,
            FailedPuzzlesMin = 0,
            FailedPuzzlesMax = 0,
            InProgressPuzzlesMin = 0,
            InProgressPuzzlesMax = 0,
            PuzzleDays = 1,
            AnchorDate = "2025-02-01",
            AllowReseed = true,
            ResetDatabase = true
        };
        var gameOptions = new GameOptions { MaxGuesses = 6, WordLength = 5 };
        var dbOptions = new DbContextOptionsBuilder<WordleTrackerSupremeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new WordleTrackerSupremeDbContext(dbOptions);
        var wordSelector = new WordSelector();
        var wordList = new WordListValidator();
        var guessEvaluator = new GuessEvaluationService(gameOptions, wordList);
        var passwordHasher = new PasswordHasher<Player>();
        var generator = new SeedDataGenerator(options, gameOptions, wordSelector, wordList, guessEvaluator, passwordHasher);
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SeederRunner>();
        var environment = new TestHostEnvironment("Production");
        var runner = new SeederRunner(dbContext, options, generator, logger, environment);
        var originalReset = Environment.GetEnvironmentVariable("E2E_RESET_ENABLED");

        Environment.SetEnvironmentVariable("E2E_RESET_ENABLED", "true");
        try
        {
            await runner.Invoking(r => r.SeedAsync())
                .Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Resetting seed data is not allowed in Production.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("E2E_RESET_ENABLED", originalReset);
        }
    }

    [Fact]
    public async Task SeedAsync_throws_when_reset_is_enabled_without_opt_in()
    {
        var options = new SeederOptions
        {
            RandomSeed = 4242,
            PlayerCount = 1,
            MinSolvedPuzzles = 1,
            MaxSolvedPuzzles = 1,
            FailedPuzzlesMin = 0,
            FailedPuzzlesMax = 0,
            InProgressPuzzlesMin = 0,
            InProgressPuzzlesMax = 0,
            PuzzleDays = 1,
            AnchorDate = "2025-02-01",
            AllowReseed = true,
            ResetDatabase = true
        };
        var gameOptions = new GameOptions { MaxGuesses = 6, WordLength = 5 };
        var dbOptions = new DbContextOptionsBuilder<WordleTrackerSupremeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new WordleTrackerSupremeDbContext(dbOptions);
        var wordSelector = new WordSelector();
        var wordList = new WordListValidator();
        var guessEvaluator = new GuessEvaluationService(gameOptions, wordList);
        var passwordHasher = new PasswordHasher<Player>();
        var generator = new SeedDataGenerator(options, gameOptions, wordSelector, wordList, guessEvaluator, passwordHasher);
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SeederRunner>();
        var environment = new TestHostEnvironment("Development");
        var runner = new SeederRunner(dbContext, options, generator, logger, environment);
        var originalReset = Environment.GetEnvironmentVariable("E2E_RESET_ENABLED");

        Environment.SetEnvironmentVariable("E2E_RESET_ENABLED", null);
        try
        {
            await runner.Invoking(r => r.SeedAsync())
                .Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("ResetDatabase requested but E2E_RESET_ENABLED is not set to true.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("E2E_RESET_ENABLED", originalReset);
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Wordle.TrackerSupreme.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
