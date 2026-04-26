using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wordle.TrackerSupreme.Api.Models.Game;
using Wordle.TrackerSupreme.Domain.Models;
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

        var filter = new PlayerStatisticsFilter { CountPracticeAttempts = true };
        var stats = statisticsService.Calculate(player, filter, attempt => gameClock.IsAfterReveal(attempt));

        return Ok(MapStats(stats));
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
                var response = MapStats(stats);
                return new PlayerStatsEntryResponse(player.Id, player.DisplayName, response);
            })
            .OrderBy(entry => entry.DisplayName)
            .ToList();

        return Ok(entries);
    }

    [HttpGet("leaderboard")]
    public async Task<ActionResult<LeaderboardPageResponse>> GetLeaderboard(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int minGames = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        minGames = Math.Max(minGames, 0);

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
            .Where(entry => entry.Stats.TotalAttempts >= minGames && entry.Stats.TotalAttempts > 0)
            .OrderByDescending(entry => entry.WinRate ?? 0)
            .ThenBy(entry => entry.Stats.AverageGuessCount ?? double.MaxValue)
            .ThenByDescending(entry => entry.Stats.Wins)
            .ThenBy(entry => entry.Player.DisplayName)
            .ToList();

        var total = ranked.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling((double)total / pageSize);
        page = Math.Min(page, totalPages);

        var leaderboard = new List<LeaderboardEntryResponse>();
        var globalRankStart = (page - 1) * pageSize + 1;
        var rank = globalRankStart;

        foreach (var entry in ranked.Skip((page - 1) * pageSize).Take(pageSize))
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

        return Ok(new LeaderboardPageResponse(leaderboard, total, page, pageSize, totalPages));
    }

    [HttpGet("leaderboard/today")]
    public async Task<ActionResult<IReadOnlyList<TodayLeaderboardEntryResponse>>> GetTodayLeaderboard(CancellationToken cancellationToken)
    {
        var today = gameClock.Today;
        var players = await playerRepository.GetPlayersWithAttempts(cancellationToken);
        var ranked = players
            .Select(player => player.Attempts
                .Where(attempt => attempt.DailyPuzzle?.PuzzleDate == today)
                .OrderByDescending(attempt => attempt.CreatedOn)
                .FirstOrDefault())
            .Where(attempt => attempt is not null)
            .Select(attempt => attempt!)
            .OrderBy(attempt => attempt.Status switch
            {
                AttemptStatus.Solved => 0,
                AttemptStatus.Failed => 1,
                _ => 2
            })
            .ThenBy(attempt => attempt.Status == AttemptStatus.InProgress ? int.MaxValue : attempt.GuessCount ?? int.MaxValue)
            .ThenBy(attempt => attempt.CompletedOn ?? DateTime.MaxValue)
            .ThenByDescending(attempt => attempt.Status == AttemptStatus.InProgress ? attempt.GuessCount ?? 0 : 0)
            .ThenBy(attempt => attempt.Player.DisplayName)
            .ToList();

        var leaderboard = new List<TodayLeaderboardEntryResponse>();
        var rank = 1;

        foreach (var attempt in ranked)
        {
            leaderboard.Add(new TodayLeaderboardEntryResponse(
                rank,
                attempt.PlayerId,
                attempt.Player.DisplayName,
                attempt.Status switch
                {
                    AttemptStatus.Solved => "Solved",
                    AttemptStatus.Failed => "Failed",
                    _ => "In progress"
                },
                attempt.GuessCount ?? 0,
                attempt.PlayedInHardMode));
            rank += 1;
        }

        return Ok(leaderboard);
    }

    private static PlayerStatsResponse MapStats(PlayerStatistics stats)
    {
        return new PlayerStatsResponse(
            stats.TotalAttempts,
            stats.Wins,
            stats.Failures,
            stats.PracticeAttempts,
            stats.CurrentStreak,
            stats.LongestStreak,
            stats.AverageGuessCount,
            stats.GuessDistribution);
    }
}
