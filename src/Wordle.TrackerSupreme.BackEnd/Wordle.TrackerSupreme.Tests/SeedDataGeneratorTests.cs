using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Services.Game;
using Wordle.TrackerSupreme.Seeder;
using Wordle.TrackerSupreme.Tests.Fakes;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class SeedDataGeneratorTests
{
    [Fact]
    public void Generate_creates_representative_puzzles_and_attempts_for_each_stream()
    {
        var generator = new SeedDataGenerator(
            new SeederOptions
            {
                PlayerCount = 1,
                MinSolvedPuzzles = 1,
                MaxSolvedPuzzles = 1,
                FailedPuzzlesMin = 0,
                FailedPuzzlesMax = 0,
                InProgressPuzzlesMin = 0,
                InProgressPuzzlesMax = 0,
                PuzzleDays = 2
            },
            new GameOptions(),
            new FakeWordSelector("CRANE"),
            new TestWordListProvider(),
            new GuessEvaluationService(new GameOptions(), new FakeWordValidator()),
            new PasswordHasher<Player>());

        var data = generator.Generate(new DateOnly(2025, 2, 1));

        data.Puzzles.Should().NotBeEmpty();
        data.Puzzles.Select(puzzle => puzzle.Stream).Should().Contain(PuzzleStream.TrackerSupreme);
        data.Puzzles.Select(puzzle => puzzle.Stream).Should().Contain(PuzzleStream.NewYorkTimes);
        data.Puzzles
            .GroupBy(puzzle => puzzle.PuzzleDate)
            .Should()
            .OnlyContain(group => group.Select(puzzle => puzzle.Stream).ToHashSet()
                .SetEquals(new[] { PuzzleStream.TrackerSupreme, PuzzleStream.NewYorkTimes }));
        data.Attempts.Select(attempt => attempt.DailyPuzzle.Stream).Should().Contain(PuzzleStream.TrackerSupreme);
        data.Attempts.Select(attempt => attempt.DailyPuzzle.Stream).Should().Contain(PuzzleStream.NewYorkTimes);
    }

    private sealed class TestWordListProvider : IWordListProvider
    {
        public IReadOnlyList<string> Words { get; } =
        [
            "SLATE",
            "CRANE",
            "BRICK",
            "PLANT",
            "MOUSE"
        ];
    }
}
