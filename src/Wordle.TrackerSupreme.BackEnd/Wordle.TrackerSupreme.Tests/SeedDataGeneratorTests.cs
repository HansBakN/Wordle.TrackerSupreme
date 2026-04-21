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
    public void Generate_assigns_tracker_supreme_stream_to_seeded_puzzles()
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
        data.Puzzles.Should().OnlyContain(puzzle => puzzle.Stream == PuzzleStream.TrackerSupreme);
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
