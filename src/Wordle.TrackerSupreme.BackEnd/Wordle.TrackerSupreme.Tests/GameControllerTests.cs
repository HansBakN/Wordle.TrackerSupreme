using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Wordle.TrackerSupreme.Api.Controllers;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Tests.Fakes;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class GameControllerTests
{
    private static GameController CreateController(
        FakeGameRepository repo,
        FakeGameClock clock,
        string solution = "CRANE",
        FakeWordValidator? wordValidator = null)
    {
        var options = new GameOptions { WordLength = 5, MaxGuesses = 6 };
        var validator = wordValidator ?? new FakeWordValidator();
        var hostEnvironment = new FakeHostEnvironment { EnvironmentName = Environments.Development };
        var officialWordProvider = new FakeOfficialWordProvider(solution);
        var gameplay = new GameplayService(
            repo,
            new DailyPuzzleService(
                repo,
                new FakeWordSelector(solution),
                officialWordProvider,
                hostEnvironment,
                NullLogger<DailyPuzzleService>.Instance),
            clock,
            new GuessEvaluationService(options, validator),
            options);
        var controller = new GameController(gameplay, clock)
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

        var result = await controller.GetState(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var state = (result.Result as OkObjectResult)!.Value as Api.Models.Game.GameStateResponse;
        state.Should().NotBeNull();
        state!.PuzzleDate.Should().Be(clock.Today);
        state.WordLength.Should().Be(5);
        state.MaxGuesses.Should().Be(6);
    }

    [Fact]
    public async Task SubmitGuess_returns_conflict_when_guessing_after_completion()
    {
        var clock = new FakeGameClock(new DateOnly(2025, 1, 11));
        var repo = new FakeGameRepository();
        var controller = CreateController(repo, clock, "PLANT");

        await controller.SubmitGuess(new Api.Models.Game.SubmitGuessRequest { Guess = "PLANT" }, CancellationToken.None);
        var second = await controller.SubmitGuess(new Api.Models.Game.SubmitGuessRequest { Guess = "PLANT" }, CancellationToken.None);

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
            CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task EnableEasyMode_returns_state_with_hard_mode_disabled()
    {
        var clock = new FakeGameClock(new DateOnly(2025, 1, 12));
        var repo = new FakeGameRepository();
        var controller = CreateController(repo, clock, "CRANE");

        var result = await controller.EnableEasyMode(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var state = (result.Result as OkObjectResult)!.Value as Api.Models.Game.GameStateResponse;
        state.Should().NotBeNull();
        state!.IsHardMode.Should().BeFalse();
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
