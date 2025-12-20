using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wordle.TrackerSupreme.Api.Models.Game;
using Wordle.TrackerSupreme.Api.Services.Game;
using Wordle.TrackerSupreme.Domain.Services;
using Wordle.TrackerSupreme.Infrastructure.Database;

namespace Wordle.TrackerSupreme.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController(
    WordleTrackerSupremeDbContext dbContext,
    PlayerStatisticsService statisticsService,
    GameClock gameClock) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<PlayerStatsResponse>> GetMine(CancellationToken cancellationToken)
    {
        var playerIdClaim = User.FindFirstValue("playerId");
        if (!Guid.TryParse(playerIdClaim, out var playerId))
        {
            return Unauthorized();
        }

        var player = await dbContext.Players
            .Include(p => p.Attempts)
                .ThenInclude(a => a.DailyPuzzle)
            .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);

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
