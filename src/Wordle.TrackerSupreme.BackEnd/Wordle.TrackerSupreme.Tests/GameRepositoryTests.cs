using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wordle.TrackerSupreme.Domain.Exceptions;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Infrastructure.Database;
using Wordle.TrackerSupreme.Infrastructure.Repositories;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class GameRepositoryTests
{
    [Fact]
    public void DailyPuzzle_model_uses_date_and_stream_unique_index()
    {
        var options = new DbContextOptionsBuilder<WordleTrackerSupremeDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var dbContext = new WordleTrackerSupremeDbContext(options);
        var entityType = dbContext.Model.FindEntityType(typeof(DailyPuzzle));

        var index = entityType!.GetIndexes()
            .Single(i => i.Properties
                .Select(p => p.Name)
                .SequenceEqual([nameof(DailyPuzzle.PuzzleDate), nameof(DailyPuzzle.Stream)]));

        index.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task DailyPuzzles_allows_same_date_when_stream_differs()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<WordleTrackerSupremeDbContext>()
            .UseSqlite(connection)
            .Options;

        await CreateDailyPuzzlesTable(options);

        var puzzleDate = new DateOnly(2026, 4, 21);

        await using var dbContext = new WordleTrackerSupremeDbContext(options);
        dbContext.DailyPuzzles.AddRange(
            new DailyPuzzle
            {
                Id = Guid.NewGuid(),
                PuzzleDate = puzzleDate,
                Stream = PuzzleStream.TrackerSupreme,
                Solution = "CRANE"
            },
            new DailyPuzzle
            {
                Id = Guid.NewGuid(),
                PuzzleDate = puzzleDate,
                Stream = PuzzleStream.NewYorkTimes,
                Solution = "SLATE"
            });

        await dbContext.SaveChangesAsync();

        dbContext.DailyPuzzles.Should().HaveCount(2);
    }

    [Fact]
    public async Task DailyPuzzles_rejects_same_date_and_stream_duplicate()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<WordleTrackerSupremeDbContext>()
            .UseSqlite(connection)
            .Options;

        await CreateDailyPuzzlesTable(options);

        var puzzleDate = new DateOnly(2026, 4, 21);

        await using var dbContext = new WordleTrackerSupremeDbContext(options);
        dbContext.DailyPuzzles.AddRange(
            new DailyPuzzle
            {
                Id = Guid.NewGuid(),
                PuzzleDate = puzzleDate,
                Stream = PuzzleStream.NewYorkTimes,
                Solution = "CRANE"
            },
            new DailyPuzzle
            {
                Id = Guid.NewGuid(),
                PuzzleDate = puzzleDate,
                Stream = PuzzleStream.NewYorkTimes,
                Solution = "SLATE"
            });

        var act = async () => await dbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task GetPuzzleByDate_returns_requested_stream()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<WordleTrackerSupremeDbContext>()
            .UseSqlite(connection)
            .Options;

        await CreateDailyPuzzlesTable(options);

        var puzzleDate = new DateOnly(2026, 4, 21);
        await using (var seedContext = new WordleTrackerSupremeDbContext(options))
        {
            seedContext.DailyPuzzles.AddRange(
                new DailyPuzzle
                {
                    Id = Guid.NewGuid(),
                    PuzzleDate = puzzleDate,
                    Stream = PuzzleStream.TrackerSupreme,
                    Solution = "CRANE"
                },
                new DailyPuzzle
                {
                    Id = Guid.NewGuid(),
                    PuzzleDate = puzzleDate,
                    Stream = PuzzleStream.NewYorkTimes,
                    Solution = "SLATE"
                });
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = new WordleTrackerSupremeDbContext(options);
        var repository = new GameRepository(dbContext);

        var puzzle = await repository.GetPuzzleByDate(puzzleDate, PuzzleStream.NewYorkTimes, CancellationToken.None);

        puzzle.Should().NotBeNull();
        puzzle!.Solution.Should().Be("SLATE");
    }

    [Fact]
    public async Task SaveChanges_translates_duplicate_attempt_unique_index_violation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<WordleTrackerSupremeDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setupContext = new WordleTrackerSupremeDbContext(options))
        {
            await setupContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE Attempts (
                    Id TEXT NOT NULL PRIMARY KEY,
                    PlayerId TEXT NOT NULL,
                    DailyPuzzleId TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    PlayedInHardMode INTEGER NOT NULL,
                    CreatedOn TEXT NOT NULL,
                    CompletedOn TEXT NULL
                );

                CREATE UNIQUE INDEX IX_Attempts_PlayerId_DailyPuzzleId
                    ON Attempts (PlayerId, DailyPuzzleId);
                """);
        }

        var playerId = Guid.NewGuid();
        var puzzleId = Guid.NewGuid();

        await using (var seedContext = new WordleTrackerSupremeDbContext(options))
        {
            await seedContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO Attempts (Id, PlayerId, DailyPuzzleId, Status, PlayedInHardMode, CreatedOn, CompletedOn)
                VALUES ({Guid.NewGuid()}, {playerId}, {puzzleId}, {(int)AttemptStatus.InProgress}, {true}, {DateTime.UtcNow}, NULL);
                """);
        }

        await using var duplicateContext = new WordleTrackerSupremeDbContext(options);
        var repository = new GameRepository(duplicateContext);
        await repository.AddAttempt(
            new PlayerPuzzleAttempt
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                DailyPuzzleId = puzzleId,
                Status = AttemptStatus.InProgress,
                PlayedInHardMode = true,
                CreatedOn = DateTime.UtcNow
            },
            CancellationToken.None);

        var act = async () => await repository.SaveChanges(CancellationToken.None);

        await act.Should().ThrowAsync<DuplicatePuzzleAttemptException>()
            .WithMessage("You already have an attempt for today's puzzle. Refresh to continue.");
    }

    private static async Task CreateDailyPuzzlesTable(DbContextOptions<WordleTrackerSupremeDbContext> options)
    {
        await using var setupContext = new WordleTrackerSupremeDbContext(options);
        await setupContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE DailyPuzzles (
                Id TEXT NOT NULL PRIMARY KEY,
                PuzzleDate TEXT NOT NULL,
                Stream INTEGER NOT NULL,
                Solution TEXT NULL,
                IsArchived INTEGER NOT NULL DEFAULT 0
            );

            CREATE UNIQUE INDEX IX_DailyPuzzles_PuzzleDate_Stream
                ON DailyPuzzles (PuzzleDate, Stream);
            """);
    }

    [Fact]
    public async Task SaveChanges_translates_duplicate_guess_unique_index_violation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<WordleTrackerSupremeDbContext>()
            .UseSqlite(connection)
            .Options;

        var attemptId = Guid.NewGuid();

        await using (var setupContext = new WordleTrackerSupremeDbContext(options))
        {
            await setupContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE Guesses (
                    Id TEXT NOT NULL PRIMARY KEY,
                    PlayerPuzzleAttemptId TEXT NOT NULL,
                    GuessNumber INTEGER NOT NULL,
                    GuessWord TEXT NOT NULL
                );

                CREATE UNIQUE INDEX IX_Guesses_PlayerPuzzleAttemptId_GuessNumber
                    ON Guesses (PlayerPuzzleAttemptId, GuessNumber);
                """);
        }

        await using (var seedContext = new WordleTrackerSupremeDbContext(options))
        {
            var existingId = Guid.NewGuid();
            var guessWord = "CRANE";
            await seedContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO Guesses (Id, PlayerPuzzleAttemptId, GuessNumber, GuessWord)
                VALUES ({existingId}, {attemptId}, {1}, {guessWord});
                """);
        }

        await using var duplicateContext = new WordleTrackerSupremeDbContext(options);
        var repository = new GameRepository(duplicateContext);
        await repository.AddGuess(
            new GuessAttempt
            {
                Id = Guid.NewGuid(),
                PlayerPuzzleAttemptId = attemptId,
                GuessNumber = 1,
                GuessWord = "PLANT",
                Feedback = []
            },
            [],
            CancellationToken.None);

        var act = async () => await repository.SaveChanges(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A guess was already recorded for this slot. Refresh to see the current state.");
    }
}
