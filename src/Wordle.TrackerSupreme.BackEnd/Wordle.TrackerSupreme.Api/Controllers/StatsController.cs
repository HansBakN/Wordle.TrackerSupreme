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

        var stats = statisticsService.Calculate(player, null, attempt => gameClock.IsAfterReveal(attempt));

        return Ok(new PlayerStatsResponse(
            stats.TotalAttempts,
            stats.Wins,
            stats.Failures,
            stats.PracticeAttempts,
            stats.CurrentStreak,
            stats.LongestStreak,
            stats.AverageGuessCount));
    }

    [HttpPost("players")]
    public async Task<ActionResult<IReadOnlyList<PlayerStatsEntryResponse>>> GetPlayers(
        [FromBody] PlayerStatsFilterRequest? request,
        CancellationToken cancellationToken)
    {
        var filter = (request ?? new PlayerStatsFilterRequest()).ToFilter();
        var players = await playerRepository.GetPlayersWithAttempts(cancellationToken);
        var entries = players
            .Select(player =>
            {
                var stats = statisticsService.Calculate(player, filter, attempt => gameClock.IsAfterReveal(attempt));
                var response = new PlayerStatsResponse(
                    stats.TotalAttempts,
                    stats.Wins,
                    stats.Failures,
                    stats.PracticeAttempts,
                    stats.CurrentStreak,
                    stats.LongestStreak,
                    stats.AverageGuessCount);
                return new PlayerStatsEntryResponse(player.Id, player.DisplayName, response);
            })
            .OrderBy(entry => entry.DisplayName)
            .ToList();

        return Ok(entries);
    }

    [HttpGet("leaderboard")]
    public async Task<ActionResult<IReadOnlyList<LeaderboardEntryResponse>>> GetLeaderboard(CancellationToken cancellationToken)
    {
        var filter = new PlayerStatsFilterRequest(
            IncludeHardMode: true,
            IncludeEasyMode: false,
            IncludeBeforeReveal: true,
            IncludeAfterReveal: false,
            IncludeSolved: true,
            IncludeFailed: true,
            IncludeInProgress: false).ToFilter();

        var players = await playerRepository.GetPlayersWithAttempts(cancellationToken);
        var ranked = players
            .Select(player =>
            {
                var stats = statisticsService.Calculate(player, filter, attempt => gameClock.IsAfterReveal(attempt));
                var winRate = stats.TotalAttempts > 0
                    ? stats.Wins / (double)stats.TotalAttempts
                    : (double?)null;
                return new
                {
                    Player = player,
                    Stats = stats,
                    WinRate = winRate
                };
            })
            .Where(entry => entry.Stats.TotalAttempts > 0)
            .OrderByDescending(entry => entry.WinRate ?? 0)
            .ThenBy(entry => entry.Stats.AverageGuessCount ?? double.MaxValue)
            .ThenByDescending(entry => entry.Stats.Wins)
            .ThenBy(entry => entry.Player.DisplayName)
            .ToList();

        var leaderboard = new List<LeaderboardEntryResponse>();
        var rank = 1;

        foreach (var entry in ranked)
        {
            leaderboard.Add(new LeaderboardEntryResponse(
                rank,
                entry.Player.Id,
                entry.Player.DisplayName,
                entry.Stats.TotalAttempts,
                entry.Stats.Wins,
                entry.Stats.Failures,
                entry.Stats.CurrentStreak,
                entry.Stats.LongestStreak,
                entry.Stats.PracticeAttempts,
                entry.Stats.AverageGuessCount,
                entry.WinRate));
            rank += 1;
        }

        return Ok(leaderboard);
    }
}
