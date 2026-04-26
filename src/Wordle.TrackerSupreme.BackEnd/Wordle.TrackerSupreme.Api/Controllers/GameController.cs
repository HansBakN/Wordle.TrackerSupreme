using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wordle.TrackerSupreme.Api.Models.Game;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Exceptions;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GameController(
    IGameplayService gameplayService,
    IGameClock gameClock,
    ILogger<GameController> logger) : ControllerBase
{
    [HttpGet("state")]
    public async Task<ActionResult<GameStateResponse>> GetState(CancellationToken cancellationToken)
    {
        var playerId = GetPlayerId();
        if (playerId is null)
        {
            return Unauthorized();
        }

        try
        {
            var state = await gameplayService.GetState(playerId.Value, cancellationToken);
            return Ok(MapState(state));
        }
        catch (DailyPuzzleUnavailableException ex)
        {
            return PuzzleUnavailable(ex.Message);
        }
    }

    [HttpPost("guess")]
    public async Task<ActionResult<GameStateResponse>> SubmitGuess([FromBody] SubmitGuessRequest request, CancellationToken cancellationToken)
    {
        var playerId = GetPlayerId();
        if (playerId is null)
        {
            return Unauthorized();
        }

        try
        {
            var state = await gameplayService.SubmitGuess(playerId.Value, request.Guess, cancellationToken);
            logger.LogInformation("Guess submitted. PlayerId={PlayerId} PuzzleDate={PuzzleDate}", playerId, state.Puzzle.PuzzleDate);
            return Ok(MapState(state));
        }
        catch (DailyPuzzleUnavailableException ex)
        {
            return PuzzleUnavailable(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Detail = ex.Message });
        }
        catch (DuplicatePuzzleAttemptException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Detail = ex.Message });
        }
    }

    [HttpPost("easy-mode")]
    public async Task<ActionResult<GameStateResponse>> EnableEasyMode(CancellationToken cancellationToken)
    {
        var playerId = GetPlayerId();
        if (playerId is null)
        {
            return Unauthorized();
        }

        try
        {
            var state = await gameplayService.EnableEasyMode(playerId.Value, cancellationToken);
            logger.LogInformation("Easy mode enabled. PlayerId={PlayerId}", playerId);
            return Ok(MapState(state));
        }
        catch (DailyPuzzleUnavailableException ex)
        {
            return PuzzleUnavailable(ex.Message);
        }
        catch (DuplicatePuzzleAttemptException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Detail = ex.Message });
        }
    }

    [HttpGet("solutions")]
    public async Task<ActionResult<SolutionsResponse>> GetSolutions(CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await gameplayService.GetSolutions(cancellationToken);
            if (!snapshot.CutoffPassed)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ProblemDetails { Status = StatusCodes.Status403Forbidden, Detail = "Solutions unlock after 12:00 PM local time." });
            }

            return Ok(MapSolutions(snapshot));
        }
        catch (DailyPuzzleUnavailableException ex)
        {
            return PuzzleUnavailable(ex.Message);
        }
    }

    private ObjectResult PuzzleUnavailable(string detail)
    {
        var pd = new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Detail = detail
        };
        pd.Extensions["code"] = "puzzle_unavailable";
        return StatusCode(StatusCodes.Status503ServiceUnavailable, pd);
    }

    private Guid? GetPlayerId()
    {
        var claim = User.FindFirstValue("playerId");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private GameStateResponse MapState(GameplayState state)
    {
        AttemptResponse? attemptResponse = null;
        if (state.Attempt is not null)
        {
            var isAfterReveal = IsAfterReveal(state.Attempt.CreatedOn, state.Puzzle.PuzzleDate);
            attemptResponse = new AttemptResponse(
                state.Attempt.Id,
                state.Attempt.Status,
                isAfterReveal,
                state.Attempt.CreatedOn,
                state.Attempt.CompletedOn,
                state.Attempt.Guesses
                    .OrderBy(g => g.GuessNumber)
                    .Select(g => new GuessResponse(
                        g.Id,
                        g.GuessNumber,
                        g.GuessWord,
                        g.Feedback
                            .OrderBy(f => f.Position)
                            .Select(f => new LetterFeedbackResponse(f.Position, f.Letter, f.Result))
                            .ToList()))
                    .ToList());
        }

        var canGuess = state.Attempt is null ||
                       (state.Attempt.Status == AttemptStatus.InProgress && state.Attempt.Guesses.Count < state.MaxGuesses);

        var isHardMode = state.Attempt?.PlayedInHardMode ?? true;
        var solution = state.SolutionRevealed ? state.Puzzle.Solution : null;

        return new GameStateResponse(
            state.Puzzle.PuzzleDate,
            state.CutoffPassed,
            state.SolutionRevealed,
            state.AllowLatePlay,
            state.WordLength,
            state.MaxGuesses,
            isHardMode,
            canGuess,
            attemptResponse,
            solution);
    }

    private SolutionsResponse MapSolutions(SolutionsSnapshot snapshot)
    {
        var entries = snapshot.Attempts
            .OrderBy(a => a.CompletedOn ?? a.CreatedOn)
            .Select(attempt =>
            {
                var isAfterReveal = IsAfterReveal(attempt.CreatedOn, snapshot.Puzzle.PuzzleDate);
                return new SolutionEntryResponse(
                    attempt.Player.Id,
                    attempt.Player.DisplayName,
                    attempt.Status,
                    attempt.GuessCount,
                    isAfterReveal,
                    attempt.CompletedOn,
                    attempt.Guesses
                        .OrderBy(g => g.GuessNumber)
                        .Select(g => g.GuessWord)
                        .ToList());
            })
            .ToList();

        return new SolutionsResponse(
            snapshot.Puzzle.PuzzleDate,
            snapshot.Puzzle.Solution ?? string.Empty,
            snapshot.CutoffPassed,
            entries);
    }

    private bool IsAfterReveal(DateTime createdOn, DateOnly puzzleDate)
    {
        var revealUtc = gameClock.GetRevealInstant(puzzleDate).ToUniversalTime().UtcDateTime;
        var attemptCreatedUtc = DateTime.SpecifyKind(createdOn, DateTimeKind.Utc);
        return attemptCreatedUtc >= revealUtc;
    }
}
