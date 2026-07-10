using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Wordle.TrackerSupreme.Api.Auth;
using Wordle.TrackerSupreme.Api.Models.Import;
using Wordle.TrackerSupreme.Domain.Services.Import;

namespace Wordle.TrackerSupreme.Api.Controllers;

[ApiController]
[Route("api/import/nyt")]
public class NytImportController(INytImportService importService) : ControllerBase
{
    [Authorize]
    [EnableRateLimiting(NytImportRateLimiting.SessionPolicy)]
    [HttpPost("session")]
    public async Task<ActionResult<NytImportSessionResponse>> CreateSession(CancellationToken cancellationToken)
    {
        if (!TryGetPlayerId(out var playerId))
        {
            return Unauthorized();
        }
        var created = await importService.CreateSession(playerId, cancellationToken);
        return Ok(new NytImportSessionResponse(created.SessionId, created.Code, created.ExpiresAt));
    }

    [Authorize]
    [HttpGet("session/{sessionId:guid}")]
    public async Task<ActionResult<NytImportSessionStatusResponse>> GetSession(Guid sessionId, CancellationToken cancellationToken)
    {
        if (!TryGetPlayerId(out var playerId))
        {
            return Unauthorized();
        }
        var status = await importService.GetSession(playerId, sessionId, cancellationToken);
        return status is null ? NotFound() : Ok(new NytImportSessionStatusResponse(status.SessionId, status.ExpiresAt,
            status.CompletedAt, status.AggregateGamesPlayed, status.Requested, status.Imported, status.Duplicates,
            status.Conflicts, status.Rejected, status.MissingFromNyt));
    }

    [AllowAnonymous]
    [EnableRateLimiting(NytImportRateLimiting.SubmissionPolicy)]
    [HttpGet("catalogue")]
    public async Task<ActionResult<IReadOnlyList<NytPuzzleCatalogueEntryResponse>>> GetCatalogue(
        [FromQuery] string importCode, CancellationToken cancellationToken)
    {
        try
        {
            var entries = await importService.GetPublishedCatalogue(importCode, cancellationToken);
            return Ok(entries.Select(entry => new NytPuzzleCatalogueEntryResponse(entry.NytPuzzleId, entry.PrintDate,
                entry.PublicPuzzleNumber)).ToList());
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new ProblemDetails { Status = 401, Detail = exception.Message });
        }
    }

    [AllowAnonymous]
    [EnableRateLimiting(NytImportRateLimiting.SubmissionPolicy)]
    [RequestSizeLimit(2_000_000)]
    [HttpPost]
    public async Task<ActionResult<NytImportResponse>> Import(NytImportSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await importService.Import(new NytImportRequest(request.SchemaVersion, request.ImportCode,
                request.AggregateGamesPlayed, request.States.Select(state => state is null ? null : new NytImportedState(
                    state.NytPuzzleId, state.PrintDate, state.Timestamp, state.Status, state.HardMode,
                    state.IsPlayingArchive, state.BoardState)).ToList()), cancellationToken);
            return Ok(new NytImportResponse(result.Requested, result.Imported, result.Duplicates, result.Conflicts,
                result.Rejected, result.MissingFromNyt, result.Results.Select(item => new NytImportRecordResponse(
                    item.NytPuzzleId, item.PrintDate, item.Outcome, item.Reason)).ToList()));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new ProblemDetails { Status = 401, Detail = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Status = 400, Detail = exception.Message });
        }
    }

    private bool TryGetPlayerId(out Guid playerId)
        => Guid.TryParse(User.FindFirstValue("playerId"), out playerId);
}
