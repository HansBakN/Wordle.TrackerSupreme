using FluentAssertions;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;
using Wordle.TrackerSupreme.Domain.Services.Game;
using Wordle.TrackerSupreme.Tests.Fakes;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class DailyPuzzleServiceTests
{
    private static DailyPuzzleService CreateService(FakeGameRepository repo, string solution = "CRANE")
    {
        var selector = new FakeWordSelector(solution);
        return new DailyPuzzleService(repo, selector);
    }

    [Fact]
    public async Task Creates_new_puzzle_when_missing()
    {
        var repo = new FakeGameRepository();
        var service = CreateService(repo);
        var date = new DateOnly(2025, 1, 1);

        var puzzle = await service.GetOrCreatePuzzle(date, CancellationToken.None);

        puzzle.PuzzleDate.Should().Be(date);
        puzzle.Solution.Should().Be("CRANE");
        repo.Puzzles.Should().ContainSingle();
    }

    [Fact]
    public async Task Fills_missing_solution_on_existing_puzzle()
    {
        var repo = new FakeGameRepository();
        var existing = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = new DateOnly(2025, 1, 2),
            Solution = ""
        };
        await repo.AddPuzzle(existing, CancellationToken.None);

        var service = CreateService(repo, "PLANT");
        var puzzle = await service.GetOrCreatePuzzle(existing.PuzzleDate, CancellationToken.None);

        puzzle.Id.Should().Be(existing.Id);
        puzzle.Solution.Should().Be("PLANT");
        repo.Puzzles.Should().HaveCount(1);
    }
}
