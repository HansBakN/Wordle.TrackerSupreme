using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Wordle.TrackerSupreme.Api.Controllers;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Exceptions;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Services.Game;
using Wordle.TrackerSupreme.Tests.Fakes;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class GameControllerTests
{
    private static GameController CreateController(
        FakeGameRepository repo,
        FakeGameClock clock,
        string solution = "CRANE",
        FakeWordValidator? wordValidator = null,
        IOfficialWordProvider? officialWordProvider = null)
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = wordValidator ?? new FakeWordValidator();
        officialWordProvider ??= new FakeOfficialWordProvider(solution);
        var gameplay = new GameplayService(
            repo,
            new DailyPuzzleService(
                repo,
                new FakeWordSelector(solution),
                officialWordProvider,
                NullLogger<DailyPuzzleService>.Instance),
            clock,
            new GuessEvaluationService(options, validator),
            options);
        var controller = new GameController(gameplay, clock, NullLogger<GameController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim("playerId", Guid.NewGuid().ToString())
                    }, "Test"))
                }
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetState_returns_ok_with_puzzle_data()
    {
        var clock = new FakeGameClock(new DateOnly(2025, 1, 10));
        var repo = new FakeGameRepository();
        var controller = CreateController(repo, clock);

        var result = await controller.GetState(null, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var state = (result.Result as OkObjectResult)!.Value as Api.Models.Game.GameStateResponse;
        state.Should().NotBeNull();
        state!.PuzzleDate.Should().Be(clock.Today);
        state.WordLength.Should().Be(5);
        state.MaxGuesses.Should().Be(6);
    }

    [Fact]
    public async Task GetState_returns_service_unavailable_when_official_provider_fails()
    {
        var clock = new FakeGameClock(new DateOnly(2025, 1, 12));
        var repo = new FakeGameRepository();
        var failingProvider = new FakeOfficialWordProvider((_, _) => throw new InvalidOperationException("boom"));
        var controller = CreateController(repo, clock, officialWordProvider: failingProvider);

        var result = await controller.GetState(null, CancellationToken.None);

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Which;
        objectResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        var pd = objectResult.Value.Should().BeOfType<ProblemDetails>().Which;
        pd.Detail.Should().Be("Unable to retrieve today's puzzle. Please try again later.");
    }

    [Fact]
    public async Task SubmitGuess_returns_conflict_when_guessing_after_completion()
    {
        var clock = new FakeGameClock(new DateOnly(2025, 1, 11));
        var repo = new FakeGameRepository();
        var controller = CreateController(repo, clock, "PLANT");

        await controller.SubmitGuess(new Api.Models.Game.SubmitGuessRequest { Guess = "PLANT" }, null, CancellationToken.None);
        var second = await controller.SubmitGuess(new Api.Models.Game.SubmitGuessRequest { Guess = "PLANT" }, null, CancellationToken.None);

        second.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task SubmitGuess_returns_bad_request_for_unknown_word()
    {
        var clock = new FakeGameClock(new DateOnly(2025, 1, 11));
        var repo = new FakeGameRepository();
        var validator = new FakeWordValidator(["CRANE"]);
        var controller = CreateController(repo, clock, "CRANE", validator);

        var result = await controller.SubmitGuess(
            new Api.Models.Game.SubmitGuessRequest { Guess = "ZZZZZ" },
            null,
            CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SubmitGuess_returns_conflict_with_duplicate_attempt_message_when_save_conflicts()
    {
        var clock = new FakeGameClock(new DateOnly(2025, 1, 11));
        var repo = new FakeGameRepository
        {
            SaveChangesHandler = _ => Task.FromException(new DuplicatePuzzleAttemptException())
        };
        var controller = CreateController(repo, clock, "CRANE");

        var result = await controller.SubmitGuess(
            new Api.Models.Game.SubmitGuessRequest { Guess = "CRANE" },
            null,
            CancellationToken.None);

        var conflict = result.Result.Should().BeOfType<ConflictObjectResult>().Which;
        var pd = conflict.Value.Should().BeOfType<ProblemDetails>().Which;
        pd.Detail.Should().Be("You already have an attempt for today's puzzle. Refresh to continue.");
    }

    [Fact]
    public async Task EnableEasyMode_returns_state_with_hard_mode_disabled()
    {
        var clock = new FakeGameClock(new DateOnly(2025, 1, 12));
        var repo = new FakeGameRepository();
        var controller = CreateController(repo, clock, "CRANE");

        var result = await controller.EnableEasyMode(null, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var state = (result.Result as OkObjectResult)!.Value as Api.Models.Game.GameStateResponse;
        state.Should().NotBeNull();
        state!.IsHardMode.Should().BeFalse();
    }

    [Fact]
    public async Task GetState_returns_replay_for_existing_past_puzzle()
    {
        var clock = new FakeGameClock(new DateOnly(2025, 1, 20));
        var repo = new FakeGameRepository();
        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = new DateOnly(2025, 1, 5),
            Solution = "GHOST",
            Stream = PuzzleStream.NewYorkTimes
        };
        await repo.AddPuzzle(puzzle, CancellationToken.None);
        var controller = CreateController(repo, clock, "GHOST");

        var result = await controller.GetState(puzzle.PuzzleDate, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var state = (result.Result as OkObjectResult)!.Value as Api.Models.Game.GameStateResponse;
        state.Should().NotBeNull();
        state!.PuzzleDate.Should().Be(puzzle.PuzzleDate);
        state.IsReplay.Should().BeTrue();
    }

    [Fact]
    public async Task GetState_returns_not_found_when_no_puzzle_exists_for_date()
    {
        var clock = new FakeGameClock(new DateOnly(2025, 1, 20));
        var repo = new FakeGameRepository();
        var controller = CreateController(repo, clock);

        var result = await controller.GetState(new DateOnly(2024, 12, 1), CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetState_returns_bad_request_for_future_date()
    {
        var clock = new FakeGameClock(new DateOnly(2025, 1, 20));
        var repo = new FakeGameRepository();
        var controller = CreateController(repo, clock);

        var result = await controller.GetState(new DateOnly(2025, 1, 25), CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetState_for_today_does_not_flag_replay()
    {
        var clock = new FakeGameClock(new DateOnly(2025, 1, 20));
        var repo = new FakeGameRepository();
        var controller = CreateController(repo, clock);

        var result = await controller.GetState(clock.Today, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var state = (result.Result as OkObjectResult)!.Value as Api.Models.Game.GameStateResponse;
        state.Should().NotBeNull();
        state!.IsReplay.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitGuess_persists_replay_attempt_against_past_puzzle()
    {
        var clock = new FakeGameClock(new DateOnly(2025, 1, 20));
        var repo = new FakeGameRepository();
        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = new DateOnly(2025, 1, 5),
            Solution = "GHOST",
            Stream = PuzzleStream.NewYorkTimes
        };
        await repo.AddPuzzle(puzzle, CancellationToken.None);
        var controller = CreateController(repo, clock, "GHOST");

        var result = await controller.SubmitGuess(
            new Api.Models.Game.SubmitGuessRequest { Guess = "GHOST" },
            puzzle.PuzzleDate,
            CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var state = (result.Result as OkObjectResult)!.Value as Api.Models.Game.GameStateResponse;
        state.Should().NotBeNull();
        state!.IsReplay.Should().BeTrue();
        state.PuzzleDate.Should().Be(puzzle.PuzzleDate);
        repo.Attempts.Should().ContainSingle(a => a.DailyPuzzleId == puzzle.Id);
    }

    [Fact]
    public async Task GetSolutions_returns_forbidden_before_cutoff()
    {
        var clock = new FakeGameClock(new DateOnly(2025, 1, 12), revealPassed: false);
        var repo = new FakeGameRepository();
        var controller = CreateController(repo, clock, "CRANE");

        var result = await controller.GetSolutions(CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task GetSolutions_returns_entries_after_cutoff()
    {
        var clock = new FakeGameClock(new DateOnly(2025, 1, 13), revealPassed: true);
        var repo = new FakeGameRepository();
        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = clock.Today,
            Solution = "CRANE"
        };
        await repo.AddPuzzle(puzzle, CancellationToken.None);

        var attempt = new PlayerPuzzleAttempt
        {
            Id = Guid.NewGuid(),
            PlayerId = Guid.NewGuid(),
            Player = new Player
            {
                Id = Guid.NewGuid(),
                DisplayName = "Tester",
                Email = "tester@example.com",
                PasswordHash = "hashed"
            },
            DailyPuzzleId = puzzle.Id,
            DailyPuzzle = puzzle,
            Status = AttemptStatus.Solved,
            CreatedOn = puzzle.PuzzleDate.ToDateTime(TimeOnly.MinValue).AddHours(8),
            CompletedOn = puzzle.PuzzleDate.ToDateTime(TimeOnly.MinValue).AddHours(9),
            Guesses = []
        };

        var guess = new GuessAttempt
        {
            Id = Guid.NewGuid(),
            PlayerPuzzleAttemptId = attempt.Id,
            GuessNumber = 1,
            GuessWord = "CRANE",
            Feedback = []
        };

        await repo.AddAttempt(attempt, CancellationToken.None);
        await repo.AddGuess(guess, [], CancellationToken.None);

        var controller = CreateController(repo, clock, puzzle.Solution!);

        var response = await controller.GetSolutions(CancellationToken.None);

        var ok = response.Result.Should().BeOfType<OkObjectResult>().Which;
        var payload = ok.Value as Api.Models.Game.SolutionsResponse;
        payload.Should().NotBeNull();
        payload!.Entries.Should().ContainSingle(e => e.DisplayName == "Tester" && e.GuessCount == 1);
        payload.PuzzleDate.Should().Be(clock.Today);
        payload.CutoffPassed.Should().BeTrue();
    }
}
