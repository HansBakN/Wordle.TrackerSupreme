using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
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

        var result = await controller.GetLeaderboard(cancellationToken: CancellationToken.None);
        var okResult = result.Result as OkObjectResult;

        okResult.Should().NotBeNull();
        var payload = okResult!.Value.Should().BeOfType<LeaderboardPageResponse>().Subject;

        payload.Items.Should().HaveCount(2);
        payload.Total.Should().Be(2);
        payload.Items.First().DisplayName.Should().Be("Sharp");
        payload.Items.First().Rank.Should().Be(1);
        payload.Items.Last().DisplayName.Should().Be("Consistent");
        payload.Items.Last().Rank.Should().Be(2);
    }

    [Fact]
    public async Task GetLeaderboard_sorts_by_requested_metric()
    {
        var alpha = CreatePlayer("Alpha");
        alpha.Attempts.Add(CreateAttempt(alpha, new DateOnly(2025, 1, 1), AttemptStatus.Solved, true, 2));
        alpha.Attempts.Add(CreateAttempt(alpha, new DateOnly(2025, 1, 2), AttemptStatus.Failed, true, 6));

        var beta = CreatePlayer("Beta");
        beta.Attempts.Add(CreateAttempt(beta, new DateOnly(2025, 1, 1), AttemptStatus.Solved, true, 3));
        beta.Attempts.Add(CreateAttempt(beta, new DateOnly(2025, 1, 2), AttemptStatus.Solved, true, 4));
        beta.Attempts.Add(CreateAttempt(beta, new DateOnly(2025, 1, 3), AttemptStatus.Solved, true, 5));

        var repo = new FakePlayerRepository([alpha, beta]);
        var controller = new StatsController(repo, new PlayerStatisticsService(), new FakeGameClock(new DateOnly(2025, 1, 4)));

        var result = await controller.GetLeaderboard(sortBy: "wins", cancellationToken: CancellationToken.None);
        var okResult = result.Result as OkObjectResult;

        okResult.Should().NotBeNull();
        var payload = okResult!.Value.Should().BeOfType<LeaderboardPageResponse>().Subject;

        payload.Items.Should().HaveCount(2);
        payload.Items.Select(item => item.DisplayName).Should().Equal("Beta", "Alpha");
        payload.Items.Select(item => item.Rank).Should().Equal(1, 2);
    }

    [Fact]
    public async Task GetPlayers_respects_easy_mode_and_after_reveal_filters()
    {
        var easyPlayer = CreatePlayer("Easy");
        easyPlayer.Attempts.Add(CreateAttempt(easyPlayer, new DateOnly(2025, 1, 1), AttemptStatus.Solved, false, 3));

        var repo = new FakePlayerRepository([easyPlayer]);
        var controller = new StatsController(
            repo,
            new PlayerStatisticsService(),
            new FakeGameClock(new DateOnly(2025, 1, 2), revealPassed: true));

        var request = new PlayerStatsFilterRequest(
            IncludeHardMode: false,
            IncludeEasyMode: true,
            IncludeBeforeReveal: false,
            IncludeAfterReveal: true,
            IncludeSolved: true,
            IncludeFailed: true,
            IncludeInProgress: false,
            CountPracticeAttempts: true);

        var result = await controller.GetPlayers(request, CancellationToken.None);
        var okResult = result.Result as OkObjectResult;

        okResult.Should().NotBeNull();
        var payload = okResult!.Value.Should().BeAssignableTo<IReadOnlyList<PlayerStatsEntryResponse>>().Subject;

        payload.Should().ContainSingle();
        payload.Single().Stats.TotalAttempts.Should().Be(1);
    }

    [Fact]
    public async Task GetLeaderboard_excludes_players_without_attempts()
    {
        var emptyPlayer = CreatePlayer("Idle");
        var activePlayer = CreatePlayer("Active");
        activePlayer.Attempts.Add(CreateAttempt(activePlayer, new DateOnly(2025, 1, 1), AttemptStatus.Solved, true, 2));

        var repo = new FakePlayerRepository([emptyPlayer, activePlayer]);
        var controller = new StatsController(repo, new PlayerStatisticsService(), new FakeGameClock(new DateOnly(2025, 1, 2)));

        var result = await controller.GetLeaderboard(cancellationToken: CancellationToken.None);
        var okResult = result.Result as OkObjectResult;

        okResult.Should().NotBeNull();
        var payload = okResult!.Value.Should().BeOfType<LeaderboardPageResponse>().Subject;

        payload.Items.Should().HaveCount(1);
        payload.Items.Single().DisplayName.Should().Be("Active");
        payload.Total.Should().Be(1);
    }

    [Fact]
    public async Task GetLeaderboard_returns_correct_page_slice_and_metadata()
    {
        var players = Enumerable.Range(1, 5)
            .Select(i =>
            {
                var p = CreatePlayer($"Player{i}");
                p.Attempts.Add(CreateAttempt(p, new DateOnly(2025, 1, i), AttemptStatus.Solved, true, i));
                return p;
            })
            .ToList();

        var repo = new FakePlayerRepository(players);
        var controller = new StatsController(repo, new PlayerStatisticsService(), new FakeGameClock(new DateOnly(2025, 1, 6)));

        var result = await controller.GetLeaderboard(page: 2, pageSize: 2, cancellationToken: CancellationToken.None);
        var okResult = result.Result as OkObjectResult;

        okResult.Should().NotBeNull();
        var payload = okResult!.Value.Should().BeOfType<LeaderboardPageResponse>().Subject;

        payload.Total.Should().Be(5);
        payload.Page.Should().Be(2);
        payload.PageSize.Should().Be(2);
        payload.TotalPages.Should().Be(3);
        payload.Items.Should().HaveCount(2);
        // Page 2 has global ranks 3 and 4
        payload.Items.First().Rank.Should().Be(3);
        payload.Items.Last().Rank.Should().Be(4);
    }

    [Fact]
    public async Task GetLeaderboard_clamps_page_to_last_page_when_out_of_range()
    {
        var player = CreatePlayer("Solo");
        player.Attempts.Add(CreateAttempt(player, new DateOnly(2025, 1, 1), AttemptStatus.Solved, true, 2));

        var repo = new FakePlayerRepository([player]);
        var controller = new StatsController(repo, new PlayerStatisticsService(), new FakeGameClock(new DateOnly(2025, 1, 2)));

        var result = await controller.GetLeaderboard(page: 99, pageSize: 10, cancellationToken: CancellationToken.None);
        var okResult = result.Result as OkObjectResult;

        var payload = okResult!.Value.Should().BeOfType<LeaderboardPageResponse>().Subject;
        payload.Page.Should().Be(1);
        payload.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTodayLeaderboard_orders_solved_then_failed_then_in_progress_for_current_puzzle()
    {
        var anchorDate = new DateOnly(2025, 1, 2);

        var speedy = CreatePlayer("Speedy");
        speedy.Attempts.Add(CreateAttempt(speedy, anchorDate, AttemptStatus.Solved, true, 2));

        var steady = CreatePlayer("Steady");
        steady.Attempts.Add(CreateAttempt(steady, anchorDate, AttemptStatus.Solved, false, 3));

        var unlucky = CreatePlayer("Unlucky");
        unlucky.Attempts.Add(CreateAttempt(unlucky, anchorDate, AttemptStatus.Failed, true, 6));

        var stillPlaying = CreatePlayer("StillPlaying");
        stillPlaying.Attempts.Add(CreateAttempt(stillPlaying, anchorDate, AttemptStatus.InProgress, true, 4));

        var yesterday = CreatePlayer("Yesterday");
        yesterday.Attempts.Add(CreateAttempt(yesterday, anchorDate.AddDays(-1), AttemptStatus.Solved, true, 1));

        var repo = new FakePlayerRepository([speedy, steady, unlucky, stillPlaying, yesterday]);
        var controller = new StatsController(repo, new PlayerStatisticsService(), new FakeGameClock(anchorDate));

        var result = await controller.GetTodayLeaderboard(CancellationToken.None);
        var okResult = result.Result as OkObjectResult;

        okResult.Should().NotBeNull();
        var payload = okResult!.Value.Should().BeAssignableTo<IReadOnlyList<TodayLeaderboardEntryResponse>>().Subject;

        payload.Select(entry => entry.DisplayName).Should().Equal("Speedy", "Steady", "Unlucky", "StillPlaying");
        payload.Select(entry => entry.Rank).Should().Equal(1, 2, 3, 4);
        payload.Select(entry => entry.Result).Should().Equal("Solved", "Solved", "Failed", "In progress");
        payload.Select(entry => entry.GuessCount).Should().Equal(2, 3, 6, 4);
        payload.Select(entry => entry.PlayedInHardMode).Should().Equal(true, false, true, true);
    }

    [Fact]
    public async Task GetTodayLeaderboard_excludes_players_without_a_current_puzzle_attempt()
    {
        var anchorDate = new DateOnly(2025, 1, 2);

        var idle = CreatePlayer("Idle");
        var yesterday = CreatePlayer("Yesterday");
        yesterday.Attempts.Add(CreateAttempt(yesterday, anchorDate.AddDays(-1), AttemptStatus.Solved, true, 2));

        var active = CreatePlayer("Active");
        active.Attempts.Add(CreateAttempt(active, anchorDate, AttemptStatus.InProgress, false, 1));

        var repo = new FakePlayerRepository([idle, yesterday, active]);
        var controller = new StatsController(repo, new PlayerStatisticsService(), new FakeGameClock(anchorDate));

        var result = await controller.GetTodayLeaderboard(CancellationToken.None);
        var okResult = result.Result as OkObjectResult;

        okResult.Should().NotBeNull();
        var payload = okResult!.Value.Should().BeAssignableTo<IReadOnlyList<TodayLeaderboardEntryResponse>>().Subject;

        payload.Should().ContainSingle();
        payload.Single().DisplayName.Should().Be("Active");
    }

    private static StatsController CreateControllerForPlayer(
        Player player,
        FakeGameClock clock)
    {
        var repo = new FakePlayerRepository([player]);
        var controller = new StatsController(repo, new PlayerStatisticsService(), clock)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim("playerId", player.Id.ToString())], "Test"))
                }
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetMyHistory_returns_unauthorized_when_player_id_claim_is_missing()
    {
        var repo = new FakePlayerRepository([]);
        var controller = new StatsController(repo, new PlayerStatisticsService(), new FakeGameClock(new DateOnly(2025, 1, 2)))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal()
                }
            }
        };

        var result = await controller.GetMyHistory(CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetMyHistory_returns_unauthorized_when_player_not_found()
    {
        var repo = new FakePlayerRepository([]);
        var controller = new StatsController(repo, new PlayerStatisticsService(), new FakeGameClock(new DateOnly(2025, 1, 2)))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim("playerId", Guid.NewGuid().ToString())], "Test"))
                }
            }
        };

        var result = await controller.GetMyHistory(CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetMyHistory_returns_empty_list_when_player_has_no_attempts()
    {
        var player = CreatePlayer("Alice");
        var clock = new FakeGameClock(new DateOnly(2025, 1, 2));
        var controller = CreateControllerForPlayer(player, clock);

        var result = await controller.GetMyHistory(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Which;
        var history = ok.Value.Should().BeAssignableTo<IReadOnlyList<PuzzleHistoryEntryResponse>>().Subject;
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyHistory_returns_attempts_ordered_most_recent_first()
    {
        var player = CreatePlayer("Alice");
        player.Attempts.Add(CreateAttempt(player, new DateOnly(2025, 1, 1), AttemptStatus.Solved, true, 3));
        player.Attempts.Add(CreateAttempt(player, new DateOnly(2025, 1, 3), AttemptStatus.Failed, false, 6));
        player.Attempts.Add(CreateAttempt(player, new DateOnly(2025, 1, 2), AttemptStatus.Solved, true, 4));

        var clock = new FakeGameClock(new DateOnly(2025, 1, 4), revealPassed: true);
        var controller = CreateControllerForPlayer(player, clock);

        var result = await controller.GetMyHistory(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Which;
        var history = ok.Value.Should().BeAssignableTo<IReadOnlyList<PuzzleHistoryEntryResponse>>().Subject;

        history.Should().HaveCount(3);
        history.Select(h => h.PuzzleDate).Should().Equal(
            new DateOnly(2025, 1, 3),
            new DateOnly(2025, 1, 2),
            new DateOnly(2025, 1, 1));
    }

    [Fact]
    public async Task GetMyHistory_includes_guesses_with_feedback()
    {
        var player = CreatePlayer("Alice");
        var puzzle = new DailyPuzzle { Id = Guid.NewGuid(), PuzzleDate = new DateOnly(2025, 1, 1), Solution = "CRANE" };
        var attempt = new PlayerPuzzleAttempt
        {
            Id = Guid.NewGuid(),
            Player = player,
            PlayerId = player.Id,
            DailyPuzzle = puzzle,
            DailyPuzzleId = puzzle.Id,
            Status = AttemptStatus.Solved,
            PlayedInHardMode = true,
            CreatedOn = new DateTime(2025, 1, 1),
            Guesses = []
        };

        var guess = new GuessAttempt
        {
            Id = Guid.NewGuid(),
            GuessNumber = 1,
            GuessWord = "CRANE",
            PlayerPuzzleAttempt = attempt,
            Feedback =
            [
                new LetterEvaluation { Id = Guid.NewGuid(), Position = 0, Letter = 'C', Result = LetterResult.Correct },
                new LetterEvaluation { Id = Guid.NewGuid(), Position = 1, Letter = 'R', Result = LetterResult.Correct },
                new LetterEvaluation { Id = Guid.NewGuid(), Position = 2, Letter = 'A', Result = LetterResult.Correct },
                new LetterEvaluation { Id = Guid.NewGuid(), Position = 3, Letter = 'N', Result = LetterResult.Correct },
                new LetterEvaluation { Id = Guid.NewGuid(), Position = 4, Letter = 'E', Result = LetterResult.Correct }
            ]
        };

        attempt.Guesses.Add(guess);
        player.Attempts.Add(attempt);

        var clock = new FakeGameClock(new DateOnly(2025, 1, 2), revealPassed: true);
        var controller = CreateControllerForPlayer(player, clock);

        var result = await controller.GetMyHistory(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Which;
        var history = ok.Value.Should().BeAssignableTo<IReadOnlyList<PuzzleHistoryEntryResponse>>().Subject;

        history.Should().HaveCount(1);
        var entry = history.Single();
        entry.Status.Should().Be(AttemptStatus.Solved);
        entry.GuessCount.Should().Be(1);
        entry.Guesses.Should().HaveCount(1);
        entry.Guesses.Single().GuessWord.Should().Be("CRANE");
        entry.Guesses.Single().Feedback.Should().HaveCount(5);
        entry.Guesses.Single().Feedback.All(f => f.Result == LetterResult.Correct).Should().BeTrue();
    }

    [Fact]
    public async Task GetMyHistory_hides_solution_when_reveal_has_not_passed()
    {
        var player = CreatePlayer("Alice");
        var puzzle = new DailyPuzzle { Id = Guid.NewGuid(), PuzzleDate = new DateOnly(2025, 1, 1), Solution = "CRANE" };
        var attempt = new PlayerPuzzleAttempt
        {
            Id = Guid.NewGuid(),
            Player = player,
            PlayerId = player.Id,
            DailyPuzzle = puzzle,
            DailyPuzzleId = puzzle.Id,
            Status = AttemptStatus.InProgress,
            PlayedInHardMode = false,
            CreatedOn = new DateTime(2025, 1, 1),
            Guesses = []
        };
        player.Attempts.Add(attempt);

        // revealPassed = false so solution should be null
        var clock = new FakeGameClock(new DateOnly(2025, 1, 1), revealPassed: false);
        var controller = CreateControllerForPlayer(player, clock);

        var result = await controller.GetMyHistory(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Which;
        var history = ok.Value.Should().BeAssignableTo<IReadOnlyList<PuzzleHistoryEntryResponse>>().Subject;

        history.Single().Solution.Should().BeNull();
    }

    [Fact]
    public async Task GetMyHistory_exposes_solution_when_reveal_has_passed()
    {
        var player = CreatePlayer("Alice");
        var puzzle = new DailyPuzzle { Id = Guid.NewGuid(), PuzzleDate = new DateOnly(2025, 1, 1), Solution = "CRANE" };
        var attempt = new PlayerPuzzleAttempt
        {
            Id = Guid.NewGuid(),
            Player = player,
            PlayerId = player.Id,
            DailyPuzzle = puzzle,
            DailyPuzzleId = puzzle.Id,
            Status = AttemptStatus.Failed,
            PlayedInHardMode = false,
            CreatedOn = new DateTime(2025, 1, 1),
            Guesses = []
        };
        player.Attempts.Add(attempt);

        // revealPassed = true so solution should be visible
        var clock = new FakeGameClock(new DateOnly(2025, 1, 2), revealPassed: true);
        var controller = CreateControllerForPlayer(player, clock);

        var result = await controller.GetMyHistory(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Which;
        var history = ok.Value.Should().BeAssignableTo<IReadOnlyList<PuzzleHistoryEntryResponse>>().Subject;

        history.Single().Solution.Should().Be("CRANE");
    }

    [Fact]
    public async Task GetMyCalendar_returns_daily_outcomes_for_date_range()
    {
        var today = new DateOnly(2025, 1, 10);
        var player = CreatePlayer("Tester");
        player.Attempts.Add(CreateAttempt(player, new DateOnly(2025, 1, 8), AttemptStatus.Solved, true, 3));
        player.Attempts.Add(CreateAttempt(player, new DateOnly(2025, 1, 9), AttemptStatus.Failed, true, 6));

        var repo = new FakePlayerRepository([player]);
        var clock = new FakeGameClock(today);
        var controller = new StatsController(repo, new PlayerStatisticsService(), clock)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim("playerId", player.Id.ToString())
                    }, "Test"))
                }
            }
        };

        var result = await controller.GetMyCalendar(days: 5, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Which;
        var calendar = ok.Value.Should().BeOfType<CalendarResponse>().Which;
        calendar.Days.Should().HaveCount(5);
        calendar.Days.Should().Contain(d => d.Date == new DateOnly(2025, 1, 8) && d.Outcome == "won" && d.GuessCount == 3);
        calendar.Days.Should().Contain(d => d.Date == new DateOnly(2025, 1, 9) && d.Outcome == "failed");
        calendar.Days.Should().Contain(d => d.Date == new DateOnly(2025, 1, 10) && d.Outcome == "none");
    }

    [Fact]
    public async Task GetMine_counts_practice_wins_in_personal_stats()
    {
        var player = CreatePlayer("Practitioner");
        player.Attempts.Add(CreateAttempt(player, new DateOnly(2025, 1, 1), AttemptStatus.Solved, true, 3));

        var repo = new FakePlayerRepository([player]);
        // revealPassed = true means the attempt is classified as practice
        var clock = new FakeGameClock(new DateOnly(2025, 1, 2), revealPassed: true);
        var controller = new StatsController(repo, new PlayerStatisticsService(), clock)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim("playerId", player.Id.ToString())],
                        "Test"))
                }
            }
        };

        var result = await controller.GetMine(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Which;
        var stats = ok.Value.Should().BeOfType<PlayerStatsResponse>().Which;
        stats.Wins.Should().Be(1, "practice wins should count in personal stats");
        stats.TotalAttempts.Should().Be(1);
        stats.PracticeAttempts.Should().Be(1);
        stats.CurrentStreak.Should().Be(0, "practice wins must not extend streaks");
        stats.LongestStreak.Should().Be(0, "practice wins must not extend streaks");
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

        public Task<Player?> GetPlayer(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.FirstOrDefault(player => player.Id == playerId));
        }

        public Task<Player?> GetPlayerByEmail(string email, CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.FirstOrDefault(player => player.Email == email));
        }

        public Task<Player?> GetPlayerWithAttempts(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.FirstOrDefault(player => player.Id == playerId));
        }

        public Task<Player?> GetPlayerWithAttemptsAndGuesses(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.FirstOrDefault(player => player.Id == playerId));
        }

        public Task<List<Player>> GetPlayers(CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.ToList());
        }

        public Task<List<Player>> GetPlayersWithAttempts(CancellationToken cancellationToken)
        {
            return Task.FromResult(_players);
        }

        public Task AddPlayer(Player player, CancellationToken cancellationToken)
        {
            _players.Add(player);
            return Task.CompletedTask;
        }

        public Task<bool> IsDisplayNameTaken(string displayName, Guid? excludePlayerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.Any(player =>
                player.DisplayName == displayName && (excludePlayerId == null || player.Id != excludePlayerId)));
        }

        public Task<bool> IsEmailTaken(string email, Guid? excludePlayerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_players.Any(player =>
                player.Email == email && (excludePlayerId == null || player.Id != excludePlayerId)));
        }

        public Task<(List<Player> Players, int TotalCount)> GetPlayersPage(string? search, int page, int pageSize, CancellationToken cancellationToken)
        {
            var all = _players.OrderBy(p => p.DisplayName).ToList();
            var paged = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((paged, all.Count));
        }

        public Task SaveChanges(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
