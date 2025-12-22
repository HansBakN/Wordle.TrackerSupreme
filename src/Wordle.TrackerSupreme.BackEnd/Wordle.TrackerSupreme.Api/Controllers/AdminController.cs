using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wordle.TrackerSupreme.Api.Models.Admin;
using Wordle.TrackerSupreme.Api.Models.Game;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Services;

namespace Wordle.TrackerSupreme.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpGet("players")]
    public async Task<ActionResult<IReadOnlyList<AdminPlayerSummaryResponse>>> GetPlayers(CancellationToken cancellationToken)
    {
        var players = await adminService.GetPlayers(cancellationToken);
        var response = players
            .OrderBy(player => player.DisplayName)
            .Select(MapSummary)
            .ToList();
        return Ok(response);
    }

    [HttpGet("players/{playerId:guid}")]
    public async Task<ActionResult<AdminPlayerDetailResponse>> GetPlayer(Guid playerId, CancellationToken cancellationToken)
    {
        var player = await adminService.GetPlayer(playerId, cancellationToken);
        if (player is null)
        {
            return NotFound();
        }

        return Ok(MapDetail(player));
    }

    [HttpPut("players/{playerId:guid}")]
    public async Task<ActionResult<AdminPlayerDetailResponse>> UpdatePlayer(
        Guid playerId,
        [FromBody] AdminUpdatePlayerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var player = await adminService.UpdatePlayerProfile(playerId, request.DisplayName, request.Email, cancellationToken);
            var refreshed = await adminService.GetPlayer(player.Id, cancellationToken);
            if (refreshed is null)
            {
                return NotFound();
            }

            return Ok(MapDetail(refreshed));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("players/{playerId:guid}/password")]
    public async Task<IActionResult> ResetPassword(
        Guid playerId,
        [FromBody] AdminResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await adminService.ResetPassword(playerId, request.Password, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("players/{playerId:guid}/admin")]
    public async Task<ActionResult<AdminPlayerDetailResponse>> SetAdminStatus(
        Guid playerId,
        [FromBody] AdminSetAdminStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await adminService.SetAdminStatus(playerId, request.IsAdmin, cancellationToken);
            var refreshed = await adminService.GetPlayer(playerId, cancellationToken);
            if (refreshed is null)
            {
                return NotFound();
            }

            return Ok(MapDetail(refreshed));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("attempts/{attemptId:guid}")]
    public async Task<ActionResult<AdminPlayerAttemptResponse>> UpdateAttempt(
        Guid attemptId,
        [FromBody] AdminUpdateAttemptRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var attempt = await adminService.UpdateAttempt(attemptId, request.Guesses, request.PlayedInHardMode, cancellationToken);
            return Ok(MapAttempt(attempt));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("attempts/{attemptId:guid}")]
    public async Task<IActionResult> DeleteAttempt(Guid attemptId, CancellationToken cancellationToken)
    {
        try
        {
            await adminService.DeleteAttempt(attemptId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private static AdminPlayerSummaryResponse MapSummary(Player player)
        => new(
            player.Id,
            player.DisplayName,
            player.Email,
            player.CreatedOn,
            player.IsAdmin,
            player.Attempts.Count);

    private static AdminPlayerDetailResponse MapDetail(Player player)
    {
        var attempts = player.Attempts
            .OrderByDescending(attempt => attempt.DailyPuzzle.PuzzleDate)
            .Select(MapAttempt)
            .ToList();

        return new AdminPlayerDetailResponse(
            player.Id,
            player.DisplayName,
            player.Email,
            player.CreatedOn,
            player.IsAdmin,
            attempts);
    }

    private static AdminPlayerAttemptResponse MapAttempt(PlayerPuzzleAttempt attempt)
    {
        var guesses = attempt.Guesses
            .OrderBy(guess => guess.GuessNumber)
            .Select(guess => new GuessResponse(
                guess.Id,
                guess.GuessNumber,
                guess.GuessWord,
                guess.Feedback
                    .OrderBy(feedback => feedback.Position)
                    .Select(feedback => new LetterFeedbackResponse(
                        feedback.Position,
                        feedback.Letter,
                        feedback.Result))
                    .ToList()))
            .ToList();

        return new AdminPlayerAttemptResponse(
            attempt.Id,
            attempt.DailyPuzzle.PuzzleDate,
            attempt.Status,
            attempt.PlayedInHardMode,
            attempt.CreatedOn,
            attempt.CompletedOn,
            guesses);
    }
}
