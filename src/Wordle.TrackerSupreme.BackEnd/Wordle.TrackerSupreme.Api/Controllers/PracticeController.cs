using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wordle.TrackerSupreme.Api.Models.Game;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PracticeController(
    IPracticeService practiceService,
    ILogger<PracticeController> logger) : ControllerBase
{
    [HttpPost("start")]
    public async Task<ActionResult<PracticeStateResponse>> StartGame(CancellationToken cancellationToken)
    {
        var playerId = GetPlayerId();
        if (playerId is null)
        {
            return Unauthorized();
        }

        var state = await practiceService.StartNewGame(playerId.Value, cancellationToken);
        logger.LogInformation("Practice game started. PlayerId={PlayerId}", playerId);
        return Ok(MapState(state));
    }

    [HttpGet("state")]
    public async Task<ActionResult<PracticeStateResponse>> GetState(CancellationToken cancellationToken)
    {
        var playerId = GetPlayerId();
        if (playerId is null)
        {
            return Unauthorized();
        }

        var state = await practiceService.GetActiveGame(playerId.Value, cancellationToken);
        if (state is null)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Detail = "No active practice game." });
        }

        return Ok(MapState(state));
    }

    [HttpPost("guess")]
    public async Task<ActionResult<PracticeStateResponse>> SubmitGuess([FromBody] SubmitGuessRequest request, CancellationToken cancellationToken)
    {
        var playerId = GetPlayerId();
        if (playerId is null)
        {
            return Unauthorized();
        }

        try
        {
            var state = await practiceService.SubmitGuess(playerId.Value, request.Guess, cancellationToken);
            return Ok(MapState(state));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Detail = ex.Message });
        }
    }

    private Guid? GetPlayerId()
    {
        var claim = User.FindFirstValue("playerId");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static PracticeStateResponse MapState(PracticeState state)
    {
        AttemptResponse? attemptResponse = null;
        if (state.Attempt is not null)
        {
            attemptResponse = new AttemptResponse(
                state.Attempt.Id,
                state.Attempt.Status,
                true,
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

        var canGuess = state.Attempt is not null &&
                       state.Attempt.Status == AttemptStatus.InProgress &&
                       state.Attempt.Guesses.Count < state.MaxGuesses;

        var solution = state.SolutionRevealed ? state.Puzzle.Solution : null;

        return new PracticeStateResponse(
            state.Puzzle.Id,
            state.SolutionRevealed,
            state.WordLength,
            state.MaxGuesses,
            canGuess,
            attemptResponse,
            solution);
    }
}
