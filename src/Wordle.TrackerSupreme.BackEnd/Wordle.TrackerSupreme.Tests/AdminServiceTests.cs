using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Wordle.TrackerSupreme.Application.Services.Admin;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;
using Wordle.TrackerSupreme.Domain.Services.Game;
using Wordle.TrackerSupreme.Tests.Fakes;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class AdminServiceTests
{
    [Fact]
    public async Task UpdateAttempt_sets_solved_when_guess_matches_solution()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator(["CRANE"]);
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var playerRepo = new FakeAdminPlayerRepository([]);
        var passwordHasher = new PasswordHasher<Player>();
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = new DateOnly(2025, 1, 5),
            Solution = "CRANE"
        };
        var attempt = new PlayerPuzzleAttempt
        {
            Id = Guid.NewGuid(),
            PlayerId = Guid.NewGuid(),
            DailyPuzzleId = puzzle.Id,
            DailyPuzzle = puzzle,
            Status = AttemptStatus.InProgress,
            PlayedInHardMode = true,
            CreatedOn = DateTime.UtcNow
        };
        await gameRepo.AddPuzzle(puzzle, CancellationToken.None);
        await gameRepo.AddAttempt(attempt, CancellationToken.None);

        var updated = await service.UpdateAttempt(attempt.Id, ["crane"], playedInHardMode: false, CancellationToken.None);

        updated.Status.Should().Be(AttemptStatus.Solved);
        updated.CompletedOn.Should().NotBeNull();
        updated.PlayedInHardMode.Should().BeFalse();
        updated.Guesses.Should().ContainSingle();
        updated.Guesses.First().GuessWord.Should().Be("CRANE");
    }

    [Fact]
    public async Task UpdateAttempt_clears_completion_when_no_guesses()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator(["CRANE"]);
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var playerRepo = new FakeAdminPlayerRepository([]);
        var passwordHasher = new PasswordHasher<Player>();
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = new DateOnly(2025, 1, 6),
            Solution = "CRANE"
        };
        var attempt = new PlayerPuzzleAttempt
        {
            Id = Guid.NewGuid(),
            PlayerId = Guid.NewGuid(),
            DailyPuzzleId = puzzle.Id,
            DailyPuzzle = puzzle,
            Status = AttemptStatus.Failed,
            PlayedInHardMode = true,
            CreatedOn = DateTime.UtcNow,
            CompletedOn = DateTime.UtcNow
        };
        await gameRepo.AddPuzzle(puzzle, CancellationToken.None);
        await gameRepo.AddAttempt(attempt, CancellationToken.None);

        var updated = await service.UpdateAttempt(attempt.Id, [], playedInHardMode: true, CancellationToken.None);

        updated.Status.Should().Be(AttemptStatus.InProgress);
        updated.CompletedOn.Should().BeNull();
        updated.Guesses.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdatePlayerProfile_rejects_duplicate_display_name()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator(["CRANE"]);
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var passwordHasher = new PasswordHasher<Player>();
        var players = new List<Player>
        {
            new()
            {
                Id = Guid.NewGuid(),
                DisplayName = "Alpha",
                Email = "alpha@example.com",
                PasswordHash = "hash"
            },
            new()
            {
                Id = Guid.NewGuid(),
                DisplayName = "Beta",
                Email = "beta@example.com",
                PasswordHash = "hash"
            }
        };
        var playerRepo = new FakeAdminPlayerRepository(players);
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var action = () => service.UpdatePlayerProfile(players[1].Id, "Alpha", "beta@example.com", CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Display name is already taken.");
    }

    [Fact]
    public async Task DeleteAttempt_removes_attempt_and_guesses()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator(["CRANE"]);
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var playerRepo = new FakeAdminPlayerRepository([]);
        var passwordHasher = new PasswordHasher<Player>();
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = new DateOnly(2025, 1, 7),
            Solution = "CRANE"
        };
        var attempt = new PlayerPuzzleAttempt
        {
            Id = Guid.NewGuid(),
            PlayerId = Guid.NewGuid(),
            DailyPuzzleId = puzzle.Id,
            DailyPuzzle = puzzle,
            Status = AttemptStatus.InProgress,
            PlayedInHardMode = true,
            CreatedOn = DateTime.UtcNow
        };
        var guess = new GuessAttempt
        {
            Id = Guid.NewGuid(),
            PlayerPuzzleAttemptId = attempt.Id,
            GuessNumber = 1,
            GuessWord = "CRANE"
        };

        await gameRepo.AddPuzzle(puzzle, CancellationToken.None);
        await gameRepo.AddAttempt(attempt, CancellationToken.None);
        await gameRepo.AddGuess(guess, [], CancellationToken.None);

        await service.DeleteAttempt(attempt.Id, CancellationToken.None);

        gameRepo.Attempts.Should().BeEmpty();
        gameRepo.Guesses.Should().BeEmpty();
    }

    private sealed class FakeAdminPlayerRepository : IPlayerRepository
    {
        private readonly List<Player> _players;

        public FakeAdminPlayerRepository(List<Player> players)
        {
            _players = players;
        }

        public Task<Player?> GetPlayer(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.FirstOrDefault(player => player.Id == playerId));
        }

        public Task<Player?> GetPlayerWithAttempts(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.FirstOrDefault(player => player.Id == playerId));
        }

        public Task<Player?> GetPlayerWithAttemptsAndGuesses(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.FirstOrDefault(player => player.Id == playerId));
        }

        public Task<List<Player>> GetPlayers(CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.ToList());
        }

        public Task<List<Player>> GetPlayersWithAttempts(CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.ToList());
        }

        public Task<bool> IsDisplayNameTaken(string displayName, Guid? excludePlayerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.Any(player =>
                player.DisplayName == displayName && (excludePlayerId == null || player.Id != excludePlayerId)));
        }

        public Task<bool> IsEmailTaken(string email, Guid? excludePlayerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.Any(player =>
                player.Email == email && (excludePlayerId == null || player.Id != excludePlayerId)));
        }

        public Task SaveChanges(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
