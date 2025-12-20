using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wordle.TrackerSupreme.Api.Models.Game;
using Wordle.TrackerSupreme.Domain.Repositories;
using Wordle.TrackerSupreme.Domain.Services;
using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController(
    IPlayerRepository playerRepository,
    IPlayerStatisticsService statisticsService,
    IGameClock gameClock) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<PlayerStatsResponse>> GetMine(CancellationToken cancellationToken)
    {
        var playerIdClaim = User.FindFirstValue("playerId");
        if (!Guid.TryParse(playerIdClaim, out var playerId))
        {
            return Unauthorized();
        }

        var player = await playerRepository.GetPlayerWithAttempts(playerId, cancellationToken);

        if (player is null)
        {
            return Unauthorized();
        }

        var stats = statisticsService.Calculate(player, attempt => gameClock.IsAfterReveal(attempt));

        return Ok(new PlayerStatsResponse(
            stats.TotalAttempts,
            stats.Wins,
            stats.Failures,
            stats.PracticeAttempts,
            stats.CurrentStreak,
            stats.LongestStreak,
            stats.AverageGuessCount));
    }
}
