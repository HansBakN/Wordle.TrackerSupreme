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
