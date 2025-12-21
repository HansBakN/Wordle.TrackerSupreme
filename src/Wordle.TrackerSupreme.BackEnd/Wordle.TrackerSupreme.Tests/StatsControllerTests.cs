using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Wordle.TrackerSupreme.Api.Controllers;
using Wordle.TrackerSupreme.Api.Models.Game;
using Wordle.TrackerSupreme.Application.Services;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;
using Wordle.TrackerSupreme.Tests.Fakes;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class StatsControllerTests
{
    [Fact]
    public async Task GetPlayers_defaults_to_hard_mode_only()
    {
        var hardPlayer = CreatePlayer("Hardy");
        hardPlayer.Attempts.Add(CreateAttempt(hardPlayer, new DateOnly(2025, 1, 1), AttemptStatus.Solved, true, 2));

        var easyPlayer = CreatePlayer("Easy");
        easyPlayer.Attempts.Add(CreateAttempt(easyPlayer, new DateOnly(2025, 1, 1), AttemptStatus.Solved, false, 3));

        var repo = new FakePlayerRepository([hardPlayer, easyPlayer]);
        var controller = new StatsController(repo, new PlayerStatisticsService(), new FakeGameClock(new DateOnly(2025, 1, 2)));

        var result = await controller.GetPlayers(null, CancellationToken.None);
        var okResult = result.Result as OkObjectResult;

        okResult.Should().NotBeNull();
        var payload = okResult!.Value.Should().BeAssignableTo<IReadOnlyList<PlayerStatsEntryResponse>>().Subject;

        payload.Should().ContainSingle(entry => entry.DisplayName == "Hardy" && entry.Stats.TotalAttempts == 1);
        payload.Should().ContainSingle(entry => entry.DisplayName == "Easy" && entry.Stats.TotalAttempts == 0);
    }

    [Fact]
    public async Task GetLeaderboard_orders_by_win_rate_then_average()
    {
        var consistent = CreatePlayer("Consistent");
        consistent.Attempts.Add(CreateAttempt(consistent, new DateOnly(2025, 1, 1), AttemptStatus.Solved, true, 2));
        consistent.Attempts.Add(CreateAttempt(consistent, new DateOnly(2025, 1, 2), AttemptStatus.Failed, true, 4));

        var sharp = CreatePlayer("Sharp");
        sharp.Attempts.Add(CreateAttempt(sharp, new DateOnly(2025, 1, 1), AttemptStatus.Solved, true, 2));
        sharp.Attempts.Add(CreateAttempt(sharp, new DateOnly(2025, 1, 2), AttemptStatus.Solved, true, 3));

        var repo = new FakePlayerRepository([consistent, sharp]);
        var controller = new StatsController(repo, new PlayerStatisticsService(), new FakeGameClock(new DateOnly(2025, 1, 2)));

        var result = await controller.GetLeaderboard(CancellationToken.None);
        var okResult = result.Result as OkObjectResult;

        okResult.Should().NotBeNull();
        var payload = okResult!.Value.Should().BeAssignableTo<IReadOnlyList<LeaderboardEntryResponse>>().Subject;

        payload.Should().HaveCount(2);
        payload.First().DisplayName.Should().Be("Sharp");
        payload.First().Rank.Should().Be(1);
        payload.Last().DisplayName.Should().Be("Consistent");
        payload.Last().Rank.Should().Be(2);
    }

    private static Player CreatePlayer(string name)
    {
        return new Player
        {
            Id = Guid.NewGuid(),
            DisplayName = name,
            Email = $"{name.ToLowerInvariant()}@example.com",
            PasswordHash = "hash",
            Attempts = []
        };
    }

    private static PlayerPuzzleAttempt CreateAttempt(
        Player player,
        DateOnly puzzleDate,
        AttemptStatus status,
        bool hardMode,
        int guessCount)
    {
        var puzzle = new DailyPuzzle { Id = Guid.NewGuid(), PuzzleDate = puzzleDate };
        var attempt = new PlayerPuzzleAttempt
        {
            Id = Guid.NewGuid(),
            Player = player,
            PlayerId = player.Id,
            DailyPuzzle = puzzle,
            DailyPuzzleId = puzzle.Id,
            Status = status,
            PlayedInHardMode = hardMode,
            CreatedOn = puzzleDate.ToDateTime(TimeOnly.MinValue),
            Guesses = []
        };

        attempt.Guesses = Enumerable.Range(1, guessCount)
            .Select(index => new GuessAttempt
            {
                Id = Guid.NewGuid(),
                GuessNumber = index,
                GuessWord = "CRANE",
                PlayerPuzzleAttempt = attempt
            })
            .ToList();

        return attempt;
    }

    private sealed class FakePlayerRepository : IPlayerRepository
    {
        private readonly List<Player> _players;

        public FakePlayerRepository(List<Player> players)
        {
            _players = players;
        }

        public Task<Player?> GetPlayerWithAttempts(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.FirstOrDefault(player => player.Id == playerId));
        }

        public Task<List<Player>> GetPlayersWithAttempts(CancellationToken cancellationToken)
        {
            return Task.FromResult(_players);
        }
    }
}
