using FluentAssertions;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Services.Game;
using Wordle.TrackerSupreme.Tests.Fakes;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class PracticeServiceTests
{
    private static readonly Guid PlayerId = Guid.NewGuid();

    private static (PracticeService Service, FakeGameRepository Repository) CreateService(string solution = "CRANE")
    {
        var repository = new FakeGameRepository();
        var wordSelector = new FakeWordSelector(solution);
        var options = new GameOptions();
        var evaluationService = new GuessEvaluationService(options, new FakeWordValidator());
        var service = new PracticeService(repository, wordSelector, evaluationService, options);
        return (service, repository);
    }

    [Fact]
    public async Task StartNewGame_creates_practice_puzzle_and_attempt()
    {
        var (service, repository) = CreateService();

        var state = await service.StartNewGame(PlayerId);

        state.Should().NotBeNull();
        state.Puzzle.IsPractice.Should().BeTrue();
        state.Puzzle.Solution.Should().Be("CRANE");
        state.Attempt.Status.Should().Be(AttemptStatus.InProgress);
        state.Attempt.PlayerId.Should().Be(PlayerId);
        state.SolutionRevealed.Should().BeFalse();
        repository.Puzzles.Should().HaveCount(1);
        repository.Attempts.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetActiveGame_returns_null_when_no_active_practice()
    {
        var (service, _) = CreateService();

        var state = await service.GetActiveGame(PlayerId);

        state.Should().BeNull();
    }

    [Fact]
    public async Task SubmitGuess_evaluates_and_returns_updated_state()
    {
        var (service, _) = CreateService("CRANE");
        await service.StartNewGame(PlayerId);

        var state = await service.SubmitGuess(PlayerId, "SLATE");

        state.Should().NotBeNull();
        state.Attempt.Guesses.Should().HaveCount(1);
        state.Attempt.Status.Should().Be(AttemptStatus.InProgress);
        state.SolutionRevealed.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitGuess_marks_solved_on_correct_answer()
    {
        var (service, _) = CreateService("CRANE");
        await service.StartNewGame(PlayerId);

        var state = await service.SubmitGuess(PlayerId, "CRANE");

        state.Attempt.Status.Should().Be(AttemptStatus.Solved);
        state.SolutionRevealed.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitGuess_marks_failed_after_max_guesses()
    {
        var (service, _) = CreateService("CRANE");
        await service.StartNewGame(PlayerId);

        for (var i = 0; i < 5; i++)
        {
            await service.SubmitGuess(PlayerId, "SLATE");
        }

        var state = await service.SubmitGuess(PlayerId, "GHOST");

        state.Attempt.Status.Should().Be(AttemptStatus.Failed);
        state.SolutionRevealed.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitGuess_throws_when_no_active_game()
    {
        var (service, _) = CreateService();

        var act = () => service.SubmitGuess(PlayerId, "CRANE");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active practice game*");
    }

    [Fact]
    public async Task Multiple_games_can_be_started_sequentially()
    {
        var (service, repository) = CreateService();

        var first = await service.StartNewGame(PlayerId);
        await service.SubmitGuess(PlayerId, "CRANE");

        var second = await service.StartNewGame(PlayerId);

        second.Puzzle.Id.Should().NotBe(first.Puzzle.Id);
        repository.Puzzles.Should().HaveCount(2);
    }
}
