using FluentAssertions;
using Wordle.TrackerSupreme.Application.Services;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Application.Services.Import;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Services.Import;
using Wordle.TrackerSupreme.Tests.Fakes;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class NytImportServiceTests
{
    private static readonly DateOnly PuzzleDate = new(2024, 10, 6);
    private readonly FixedTimeProvider _time = new(new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.Zero));
    private readonly FakeNytImportRepository _imports = new();
    private readonly FakeGameRepository _games = new();
    private readonly NytPuzzleMetadata _metadata;
    private readonly NytImportService _service;

    public NytImportServiceTests()
    {
        var id = PuzzleDate.DayNumber - new DateOnly(2021, 6, 19).DayNumber + 1;
        _metadata = new NytPuzzleMetadata(id, PuzzleDate, 1205, "CIGAR");
        _service = new NytImportService(_imports, _games, new MetadataProvider(_metadata),
            new GuessEvaluationService(new GameOptions(), new FakeWordValidator()),
            new NytPuzzleCatalogue([_metadata]), new NytImportOptions(), _time);
    }

    [Fact]
    public async Task Creates_hashed_expiring_session_and_rejects_expired_code()
    {
        var playerId = Guid.NewGuid();
        var created = await _service.CreateSession(playerId, CancellationToken.None);
        created.Code.Should().MatchRegex("^[A-Z2-9]{4}-[A-Z2-9]{4}$");
        _imports.Sessions.Single().CodeHash.Should().NotContain(created.Code.Replace("-", string.Empty));
        created.ExpiresAt.Should().Be(_time.UtcNow.UtcDateTime.AddMinutes(10));

        _time.UtcNow = _time.UtcNow.AddMinutes(11);
        var action = () => _service.Import(Request(created.Code, WinState()), CancellationToken.None);
        await action.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*expired*");
    }

    [Fact]
    public async Task Imports_win_in_uppercase_with_regenerated_feedback_and_source_timestamp()
    {
        var created = await _service.CreateSession(Guid.NewGuid(), CancellationToken.None);
        var state = WinState(["rebut", "cigar"], timestamp: 1728235420);
        var result = await _service.Import(Request(created.Code, state), CancellationToken.None);

        result.Imported.Should().Be(1);
        var attempt = _games.Attempts.Single();
        attempt.Status.Should().Be(AttemptStatus.Solved);
        attempt.PlayedInHardMode.Should().BeTrue();
        attempt.CreatedOn.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1728235420).UtcDateTime);
        _games.Guesses.Select(guess => guess.GuessWord).Should().Equal("REBUT", "CIGAR");
        _games.Guesses.Last().Feedback.Should().OnlyContain(item => item.Result == LetterResult.Correct);
        _games.Puzzles.Single().Stream.Should().Be(PuzzleStream.NewYorkTimes);
        _games.Puzzles.Single().NytPuzzleId.Should().Be(_metadata.NytPuzzleId);
    }

    [Fact]
    public async Task Imports_failed_game_and_classifies_archive_timestamp_as_practice()
    {
        var created = await _service.CreateSession(Guid.NewGuid(), CancellationToken.None);
        var sourceTimestamp = new DateTimeOffset(2024, 10, 6, 8, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var state = WinState(["REBUT", "SISSY", "HUMPH", "AWAKE", "BLUSH", "FOCAL"], status: "FAIL",
            timestamp: sourceTimestamp, archive: true);
        var result = await _service.Import(Request(created.Code, state), CancellationToken.None);
        result.Imported.Should().Be(1);
        var attempt = _games.Attempts.Single();
        attempt.Status.Should().Be(AttemptStatus.Failed);
        attempt.IsImportedArchive.Should().BeTrue();
        attempt.CreatedOn.Should().Be(DateTimeOffset.FromUnixTimeSeconds(sourceTimestamp).UtcDateTime);

        var player = new Player { Id = attempt.PlayerId, DisplayName = "Player", Email = "p@example.com", PasswordHash = "x", Attempts = [attempt] };
        var stats = new PlayerStatisticsService();
        stats.Calculate(player, new PlayerStatisticsFilter { IncludeImportedNyt = true }).TotalAttempts.Should().Be(0);
        var includingPractice = stats.Calculate(player, new PlayerStatisticsFilter { IncludeImportedNyt = true, CountPracticeAttempts = true });
        includingPractice.PracticeAttempts.Should().Be(1);
        includingPractice.CurrentStreak.Should().Be(0);
    }

    [Fact]
    public async Task Is_idempotent_and_reports_conflicts_without_overwriting()
    {
        var playerId = Guid.NewGuid();
        var first = await _service.CreateSession(playerId, CancellationToken.None);
        (await _service.Import(Request(first.Code, WinState()), CancellationToken.None)).Imported.Should().Be(1);
        var second = await _service.CreateSession(playerId, CancellationToken.None);
        (await _service.Import(Request(second.Code, WinState()), CancellationToken.None)).Duplicates.Should().Be(1);
        var third = await _service.CreateSession(playerId, CancellationToken.None);
        var conflict = WinState(["REBUT", "CIGAR"]);
        (await _service.Import(Request(third.Code, conflict), CancellationToken.None)).Conflicts.Should().Be(1);
        _games.Attempts.Should().HaveCount(1);

        var reused = () => _service.Import(Request(first.Code, WinState()), CancellationToken.None);
        await reused.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*already been used*");
    }

    [Fact]
    public async Task Tracker_supreme_attempt_on_same_date_does_not_conflict_with_nyt_import()
    {
        var playerId = Guid.NewGuid();
        var trackerPuzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = PuzzleDate,
            Stream = PuzzleStream.TrackerSupreme,
            Solution = "CRANE"
        };
        var trackerAttempt = new PlayerPuzzleAttempt
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            DailyPuzzleId = trackerPuzzle.Id,
            DailyPuzzle = trackerPuzzle,
            Status = AttemptStatus.Solved,
            Guesses = [new GuessAttempt { GuessWord = "CRANE", GuessNumber = 1 }]
        };
        await _games.AddPuzzle(trackerPuzzle, CancellationToken.None);
        await _games.AddAttempt(trackerAttempt, CancellationToken.None);

        var created = await _service.CreateSession(playerId, CancellationToken.None);
        var result = await _service.Import(Request(created.Code, WinState()), CancellationToken.None);

        result.Imported.Should().Be(1);
        _games.Attempts.Should().HaveCount(2);
        _games.Puzzles.Should().Contain(puzzle => puzzle.PuzzleDate == PuzzleDate && puzzle.Stream == PuzzleStream.NewYorkTimes);
    }

    [Fact]
    public async Task Rejects_malformed_unknown_and_future_records_but_imports_valid_record()
    {
        var future = _metadata with { NytPuzzleId = _metadata.NytPuzzleId + 1, PrintDate = new DateOnly(2027, 1, 1) };
        var service = new NytImportService(_imports, _games, new MetadataProvider(_metadata),
            new GuessEvaluationService(new GameOptions(), new FakeWordValidator()),
            new NytPuzzleCatalogue([_metadata, future]), new NytImportOptions(), _time);
        var created = await service.CreateSession(Guid.NewGuid(), CancellationToken.None);
        var states = new NytImportedState?[]
        {
            WinState(),
            WinState(["TOO-LONG"]),
            WinState(["CIGAR", "CIGAR"]),
            WinState() with { BoardState = null },
            null,
            WinState() with { NytPuzzleId = 99999 },
            WinState() with { NytPuzzleId = future.NytPuzzleId, PrintDate = future.PrintDate }
        };
        var result = await service.Import(new NytImportRequest(1, created.Code, 7, states), CancellationToken.None);
        result.Imported.Should().Be(1);
        result.Rejected.Should().Be(6);
        result.MissingFromNyt.Should().Be(0);
        result.Results.Should().Contain(item => item.Reason != null && item.Reason.Contains("cannot continue after solving"));
        result.Results.Should().Contain(item => item.Reason != null && item.Reason.Contains("board state"));
    }

    [Fact]
    public async Task Enforces_state_limit_and_imported_stats_filter()
    {
        var strict = new NytImportService(_imports, _games, new MetadataProvider(_metadata),
            new GuessEvaluationService(new GameOptions(), new FakeWordValidator()), new NytPuzzleCatalogue([_metadata]),
            new NytImportOptions { MaxStates = 1 }, _time);
        var created = await strict.CreateSession(Guid.NewGuid(), CancellationToken.None);
        var oversized = () => strict.Import(new NytImportRequest(1, created.Code, null, [WinState(), WinState()]), CancellationToken.None);
        await oversized.Should().ThrowAsync<ArgumentException>().WithMessage("*at most 1*");

        var imported = new PlayerPuzzleAttempt
        {
            Id = Guid.NewGuid(),
            NytImportSessionId = Guid.NewGuid(),
            Status = AttemptStatus.Solved,
            DailyPuzzle = new DailyPuzzle { PuzzleDate = PuzzleDate },
            Guesses = [new GuessAttempt { GuessWord = "CIGAR" }]
        };
        var player = new Player { Id = Guid.NewGuid(), DisplayName = "Player", Email = "p@example.com", PasswordHash = "x", Attempts = [imported] };
        var stats = new PlayerStatisticsService();
        stats.Calculate(player, new PlayerStatisticsFilter { IncludeImportedNyt = false }).TotalAttempts.Should().Be(0);
        stats.Calculate(player, new PlayerStatisticsFilter { IncludeImportedNyt = true }).Wins.Should().Be(1);
    }

    private NytImportedState WinState(IReadOnlyList<string>? guesses = null, string status = "WIN", long? timestamp = 1728235420, bool archive = false)
        => new(_metadata.NytPuzzleId, PuzzleDate, timestamp, status, true, archive, guesses ?? ["CIGAR"]);
    private static NytImportRequest Request(string code, params NytImportedState?[] states) => new(1, code, states.Length, states);

    private sealed class MetadataProvider(NytPuzzleMetadata metadata) : Wordle.TrackerSupreme.Domain.Services.Game.IOfficialWordProvider
    {
        public Task<string> GetSolutionForDateAsync(DateOnly puzzleDate, CancellationToken cancellationToken) => Task.FromResult(metadata.Solution!);
        public Task<NytPuzzleMetadata> GetMetadataForDateAsync(DateOnly puzzleDate, CancellationToken cancellationToken) => Task.FromResult(metadata);
    }
}
