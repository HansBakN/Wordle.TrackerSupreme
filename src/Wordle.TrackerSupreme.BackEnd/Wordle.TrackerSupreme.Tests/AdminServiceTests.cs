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
    public async Task UpdatePlayerProfile_rejects_blank_display_name()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator(["CRANE"]);
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var passwordHasher = new PasswordHasher<Player>();
        var player = new Player
        {
            Id = Guid.NewGuid(),
            DisplayName = "Alpha",
            Email = "alpha@example.com",
            PasswordHash = "hash"
        };
        var playerRepo = new FakeAdminPlayerRepository([player]);
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var action = () => service.UpdatePlayerProfile(player.Id, " ", "alpha@example.com", CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Display name is required.*");
    }

    [Fact]
    public async Task UpdatePlayerProfile_rejects_invalid_email()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator(["CRANE"]);
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var passwordHasher = new PasswordHasher<Player>();
        var player = new Player
        {
            Id = Guid.NewGuid(),
            DisplayName = "Alpha",
            Email = "alpha@example.com",
            PasswordHash = "hash"
        };
        var playerRepo = new FakeAdminPlayerRepository([player]);
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var action = () => service.UpdatePlayerProfile(player.Id, "Alpha", "not-an-email", CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Email must be a valid email address.*");
    }

    [Fact]
    public async Task ResetPassword_rejects_short_password()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator(["CRANE"]);
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var passwordHasher = new PasswordHasher<Player>();
        var player = new Player
        {
            Id = Guid.NewGuid(),
            DisplayName = "Alpha",
            Email = "alpha@example.com",
            PasswordHash = "hash"
        };
        var playerRepo = new FakeAdminPlayerRepository([player]);
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var action = () => service.ResetPassword(player.Id, "abc", CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Password must be between 6 and 100 characters.*");
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

    [Fact]
    public async Task CreatePuzzle_adds_puzzle_with_normalized_solution()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator(["CRANE"]);
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var playerRepo = new FakeAdminPlayerRepository([]);
        var passwordHasher = new PasswordHasher<Player>();
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var puzzle = await service.CreatePuzzle(new DateOnly(2025, 6, 1), "crane", CancellationToken.None);

        puzzle.Solution.Should().Be("CRANE");
        puzzle.PuzzleDate.Should().Be(new DateOnly(2025, 6, 1));
        gameRepo.Puzzles.Should().ContainSingle(p => p.Id == puzzle.Id);
    }

    [Fact]
    public async Task CreatePuzzle_rejects_wrong_length_solution()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator();
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var playerRepo = new FakeAdminPlayerRepository([]);
        var passwordHasher = new PasswordHasher<Player>();
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var action = () => service.CreatePuzzle(new DateOnly(2025, 6, 1), "AB", CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*exactly 5 letters*");
    }

    [Fact]
    public async Task CreatePuzzle_rejects_non_letter_solution()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator();
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var playerRepo = new FakeAdminPlayerRepository([]);
        var passwordHasher = new PasswordHasher<Player>();
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var action = () => service.CreatePuzzle(new DateOnly(2025, 6, 1), "A B C", CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*only letters*");
    }

    [Fact]
    public async Task CreatePuzzle_rejects_duplicate_date()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator();
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var playerRepo = new FakeAdminPlayerRepository([]);
        var passwordHasher = new PasswordHasher<Player>();
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        await gameRepo.AddPuzzle(new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = new DateOnly(2025, 6, 1),
            Solution = "CRANE"
        }, CancellationToken.None);

        var action = () => service.CreatePuzzle(new DateOnly(2025, 6, 1), "PLANT", CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdatePuzzle_rejects_non_letter_solution()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator();
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var playerRepo = new FakeAdminPlayerRepository([]);
        var passwordHasher = new PasswordHasher<Player>();
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = DateOnly.FromDateTime(DateTime.Now).AddDays(1),
            Solution = "CRANE"
        };
        await gameRepo.AddPuzzle(puzzle, CancellationToken.None);

        var action = () => service.UpdatePuzzle(puzzle.Id, new DateOnly(2025, 6, 1), "12345", CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*only letters*");
    }

    [Fact]
    public async Task UpdatePuzzle_rejects_modification_when_attempts_exist()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator();
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var playerRepo = new FakeAdminPlayerRepository([]);
        var passwordHasher = new PasswordHasher<Player>();
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = new DateOnly(2025, 6, 1),
            Solution = "CRANE",
            Attempts = [new PlayerPuzzleAttempt
            {
                Id = Guid.NewGuid(),
                PlayerId = Guid.NewGuid(),
                DailyPuzzleId = Guid.NewGuid(),
                Status = AttemptStatus.Solved,
                CreatedOn = DateTime.UtcNow
            }]
        };
        await gameRepo.AddPuzzle(puzzle, CancellationToken.None);

        var action = () => service.UpdatePuzzle(puzzle.Id, new DateOnly(2025, 6, 2), "PLANT", CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has player attempts*");
    }

    [Fact]
    public async Task UpdatePuzzle_rejects_modification_for_today_puzzle_without_attempts()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator();
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var playerRepo = new FakeAdminPlayerRepository([]);
        var passwordHasher = new PasswordHasher<Player>();
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);
        var today = DateOnly.FromDateTime(DateTime.Now);

        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = today,
            Solution = "CRANE"
        };
        await gameRepo.AddPuzzle(puzzle, CancellationToken.None);

        var action = () => service.UpdatePuzzle(puzzle.Id, today.AddDays(1), "PLANT", CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*live*");
    }

    [Fact]
    public async Task DeletePuzzle_rejects_deletion_for_past_puzzle_without_attempts()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator();
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var playerRepo = new FakeAdminPlayerRepository([]);
        var passwordHasher = new PasswordHasher<Player>();
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = DateOnly.FromDateTime(DateTime.Now).AddDays(-1),
            Solution = "CRANE"
        };
        await gameRepo.AddPuzzle(puzzle, CancellationToken.None);

        var action = () => service.DeletePuzzle(puzzle.Id, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*live*");
    }

    [Fact]
    public async Task DeletePuzzle_removes_puzzle_without_attempts()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator();
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var playerRepo = new FakeAdminPlayerRepository([]);
        var passwordHasher = new PasswordHasher<Player>();
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = DateOnly.FromDateTime(DateTime.Now).AddDays(1),
            Solution = "CRANE"
        };
        await gameRepo.AddPuzzle(puzzle, CancellationToken.None);

        await service.DeletePuzzle(puzzle.Id, CancellationToken.None);

        gameRepo.Puzzles.Should().BeEmpty();
    }

    [Fact]
    public async Task DeletePuzzle_rejects_deletion_when_attempts_exist()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator();
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var playerRepo = new FakeAdminPlayerRepository([]);
        var passwordHasher = new PasswordHasher<Player>();
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = new DateOnly(2025, 6, 1),
            Solution = "CRANE",
            Attempts = [new PlayerPuzzleAttempt
            {
                Id = Guid.NewGuid(),
                PlayerId = Guid.NewGuid(),
                DailyPuzzleId = Guid.NewGuid(),
                Status = AttemptStatus.Solved,
                CreatedOn = DateTime.UtcNow
            }]
        };
        await gameRepo.AddPuzzle(puzzle, CancellationToken.None);

        var action = () => service.DeletePuzzle(puzzle.Id, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has player attempts*");
    }

    [Fact]
    public async Task GetPlayersPage_returns_paged_results_matching_search()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator([]);
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var passwordHasher = new PasswordHasher<Player>();

        var players = new List<Player>
        {
            new() { Id = Guid.NewGuid(), DisplayName = "Alice", Email = "alice@example.com", PasswordHash = "x" },
            new() { Id = Guid.NewGuid(), DisplayName = "Bob", Email = "bob@example.com", PasswordHash = "x" },
            new() { Id = Guid.NewGuid(), DisplayName = "Charlie", Email = "charlie@example.com", PasswordHash = "x" }
        };
        var playerRepo = new FakeAdminPlayerRepository(players);
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var (paged, total) = await service.GetPlayersPage("ali", 1, 10, CancellationToken.None);

        total.Should().Be(1);
        paged.Should().ContainSingle(p => p.DisplayName == "Alice");
    }

    [Fact]
    public async Task GetPlayersPage_paginates_correctly()
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = new FakeWordValidator([]);
        var guessService = new GuessEvaluationService(options, validator);
        var gameRepo = new FakeGameRepository();
        var passwordHasher = new PasswordHasher<Player>();

        var players = Enumerable.Range(1, 25)
            .Select(i => new Player { Id = Guid.NewGuid(), DisplayName = $"Player{i:D2}", Email = $"p{i}@e.com", PasswordHash = "x" })
            .ToList();
        var playerRepo = new FakeAdminPlayerRepository(players);
        var service = new AdminService(playerRepo, gameRepo, guessService, passwordHasher, options);

        var (page1, total) = await service.GetPlayersPage(null, 1, 20, CancellationToken.None);
        var (page2, _) = await service.GetPlayersPage(null, 2, 20, CancellationToken.None);

        total.Should().Be(25);
        page1.Should().HaveCount(20);
        page2.Should().HaveCount(5);
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

        public Task<Player?> GetPlayerByEmail(string email, CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.FirstOrDefault(player => player.Email == email));
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

        public Task AddPlayer(Player player, CancellationToken cancellationToken)
        {
            _players.Add(player);
            return Task.CompletedTask;
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

        public Task<(List<Player> Players, int TotalCount)> GetPlayersPage(string? search, int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = _players.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.DisplayName.ToLower().Contains(term) || p.Email.ToLower().Contains(term));
            }
            var all = query.OrderBy(p => p.DisplayName).ToList();
            var paged = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((paged, all.Count));
        }

        public Task SaveChanges(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
