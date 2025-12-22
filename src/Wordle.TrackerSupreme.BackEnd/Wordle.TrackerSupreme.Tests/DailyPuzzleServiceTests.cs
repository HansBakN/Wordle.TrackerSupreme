using System;
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Services.Game;
using Wordle.TrackerSupreme.Tests.Fakes;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class DailyPuzzleServiceTests
{
    [Fact]
    public async Task Uses_official_solution_in_production()
    {
        var repo = new FakeGameRepository();
        var hostEnvironment = new FakeHostEnvironment { EnvironmentName = Environments.Production };
        var officialProvider = new FakeOfficialWordProvider("RANGE");
        var service = CreateService(repo, hostEnvironment, officialProvider);

        var puzzle = await service.GetOrCreatePuzzle(new DateOnly(2025, 1, 1), CancellationToken.None);

        puzzle.Solution.Should().Be("RANGE");
        officialProvider.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Fills_existing_puzzle_with_official_solution()
    {
        var repo = new FakeGameRepository();
        var hostEnvironment = new FakeHostEnvironment { EnvironmentName = Environments.Production };
        var officialProvider = new FakeOfficialWordProvider("PLANT");
        var existing = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = new DateOnly(2025, 1, 2),
            Solution = string.Empty
        };
        await repo.AddPuzzle(existing, CancellationToken.None);
        var service = CreateService(repo, hostEnvironment, officialProvider);

        var puzzle = await service.GetOrCreatePuzzle(existing.PuzzleDate, CancellationToken.None);

        puzzle.Id.Should().Be(existing.Id);
        puzzle.Solution.Should().Be("PLANT");
        officialProvider.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Falls_back_to_selector_when_official_provider_fails()
    {
        var repo = new FakeGameRepository();
        var hostEnvironment = new FakeHostEnvironment { EnvironmentName = Environments.Production };
        var officialProvider = new FakeOfficialWordProvider((_, _) => throw new InvalidOperationException("boom"));
        var service = CreateService(repo, hostEnvironment, officialProvider, new FakeWordSelector("SHINE"));

        var puzzle = await service.GetOrCreatePuzzle(new DateOnly(2025, 1, 3), CancellationToken.None);

        puzzle.Solution.Should().Be("SHINE");
        officialProvider.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Uses_selector_in_development_environment()
    {
        var repo = new FakeGameRepository();
        var hostEnvironment = new FakeHostEnvironment { EnvironmentName = Environments.Development };
        var officialProvider = new FakeOfficialWordProvider((_, _) => throw new InvalidOperationException("should not be called"));
        var service = CreateService(repo, hostEnvironment, officialProvider, new FakeWordSelector("GLASS"));

        var puzzle = await service.GetOrCreatePuzzle(new DateOnly(2025, 1, 4), CancellationToken.None);

        puzzle.Solution.Should().Be("GLASS");
        officialProvider.CallCount.Should().Be(0);
    }

    private static DailyPuzzleService CreateService(
        FakeGameRepository repository,
        IHostEnvironment hostEnvironment,
        FakeOfficialWordProvider officialWordProvider,
        IWordSelector? wordSelector = null)
    {
        var logger = NullLogger<DailyPuzzleService>.Instance;
        wordSelector ??= new FakeWordSelector("CRANE");
        return new DailyPuzzleService(repository, wordSelector, officialWordProvider, hostEnvironment, logger);
    }
}
