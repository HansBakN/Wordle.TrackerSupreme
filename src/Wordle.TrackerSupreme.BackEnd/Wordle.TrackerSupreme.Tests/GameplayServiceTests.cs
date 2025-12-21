using FluentAssertions;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Tests.Fakes;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class GameplayServiceTests
{
    private GameplayService CreateService(FakeGameRepository repo, FakeGameClock clock, string solution = "APPLE", FakeWordValidator? wordValidator = null)
    {
        var puzzleService = new DailyPuzzleService(repo, new FakeWordSelector(solution));
        var options = new GameOptions { MaxGuesses = 6, WordLength = 5 };
        var validator = wordValidator ?? new FakeWordValidator();
        return new GameplayService(repo, puzzleService, clock, new GuessEvaluationService(options, validator), options);
    }

    [Fact]
    public async Task SubmitGuess_creates_attempt_and_marks_solved_on_correct_guess()
    {
        var repo = new FakeGameRepository();
        var clock = new FakeGameClock(new DateOnly(2025, 1, 3));
        var gameplay = CreateService(repo, clock, "PLANT");
        var playerId = Guid.NewGuid();

        var state = await gameplay.SubmitGuess(playerId, "PLANT");

        state.Attempt.Should().NotBeNull();
        state.Attempt!.Status.Should().Be(AttemptStatus.Solved);
        state.Attempt.PlayedInHardMode.Should().BeTrue();
        state.Attempt.Guesses.Should().ContainSingle();
        state.Attempt.Guesses.First().GuessWord.Should().Be("PLANT");
        repo.Attempts.Should().ContainSingle(a => a.PlayerId == playerId);
    }

    [Fact]
    public async Task SubmitGuess_fails_after_exceeding_max_guesses()
    {
        var repo = new FakeGameRepository();
        var clock = new FakeGameClock(new DateOnly(2025, 1, 4));
        var gameplay = CreateService(repo, clock, "CRANE");
        var playerId = Guid.NewGuid();

        for (int i = 0; i < 6; i++)
        {
            await gameplay.SubmitGuess(playerId, "SLATE");
        }

        var act = async () => await gameplay.SubmitGuess(playerId, "SLATE");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Puzzle already completed for today.");

        var attempt = repo.Attempts.Single();
        attempt.Status.Should().Be(AttemptStatus.Failed);
        attempt.Guesses.Should().HaveCount(6);
    }

    [Fact]
    public async Task GetState_returns_existing_attempt_when_present()
    {
        var repo = new FakeGameRepository();
        var clock = new FakeGameClock(new DateOnly(2025, 1, 5));
        var gameplay = CreateService(repo, clock, "ROAST");
        var playerId = Guid.NewGuid();

        await gameplay.SubmitGuess(playerId, "STORM");
        var state = await gameplay.GetState(playerId);

        state.Attempt.Should().NotBeNull();
        state.Attempt!.Guesses.Should().HaveCount(1);
        state.CutoffPassed.Should().BeFalse();
        state.SolutionRevealed.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitGuess_rejects_invalid_length()
    {
        var repo = new FakeGameRepository();
        var gameplay = CreateService(repo, new FakeGameClock(new DateOnly(2025, 1, 6)), "BREAD");
        var playerId = Guid.NewGuid();

        var act = async () => await gameplay.SubmitGuess(playerId, "TOO-LONG");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Guess must be 5 letters long.*");
    }

    [Fact]
    public async Task SubmitGuess_rejects_unknown_words()
    {
        var repo = new FakeGameRepository();
        var validator = new FakeWordValidator(["CRANE"]);
        var gameplay = CreateService(repo, new FakeGameClock(new DateOnly(2025, 1, 6)), "CRANE", validator);
        var playerId = Guid.NewGuid();

        var act = async () => await gameplay.SubmitGuess(playerId, "ZZZZZ");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Guess must be a valid English word.*");
    }

    [Fact]
    public async Task SubmitGuess_handles_repeated_letters_correctly()
    {
        var repo = new FakeGameRepository();
        var gameplay = CreateService(repo, new FakeGameClock(new DateOnly(2025, 1, 7)), "APPLE");
        var playerId = Guid.NewGuid();

        var state = await gameplay.SubmitGuess(playerId, "ALLEY");

        var feedback = state.Attempt!.Guesses.First().Feedback.OrderBy(f => f.Position).ToList();
        feedback[0].Result.Should().Be(LetterResult.Correct); // A in correct spot
        feedback[1].Result.Should().Be(LetterResult.Present); // first L present (matches single L)
        feedback[2].Result.Should().Be(LetterResult.Absent);  // second L absent after first consumed
        feedback[3].Result.Should().Be(LetterResult.Present); // E present
        feedback[4].Result.Should().Be(LetterResult.Absent);  // Y absent
    }

    [Fact]
    public async Task SubmitGuess_rejects_guess_that_ignores_revealed_positions_in_hard_mode()
    {
        var repo = new FakeGameRepository();
        var gameplay = CreateService(repo, new FakeGameClock(new DateOnly(2025, 1, 8)), "APPLE");
        var playerId = Guid.NewGuid();

        await gameplay.SubmitGuess(playerId, "ALLEY");

        var act = async () => await gameplay.SubmitGuess(playerId, "PASTE");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Hard mode: guess must keep revealed letters in their exact positions.");
    }

    [Fact]
    public async Task SubmitGuess_rejects_guess_that_omits_revealed_letters_in_hard_mode()
    {
        var repo = new FakeGameRepository();
        var gameplay = CreateService(repo, new FakeGameClock(new DateOnly(2025, 1, 9)), "APPLE");
        var playerId = Guid.NewGuid();

        await gameplay.SubmitGuess(playerId, "ALLEY");

        var act = async () => await gameplay.SubmitGuess(playerId, "AMASS");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Hard mode: guess must include all revealed letters.");
    }

    [Fact]
    public async Task EnableEasyMode_disables_hard_mode_for_current_attempt()
    {
        var repo = new FakeGameRepository();
        var gameplay = CreateService(repo, new FakeGameClock(new DateOnly(2025, 1, 10)), "APPLE");
        var playerId = Guid.NewGuid();

        await gameplay.SubmitGuess(playerId, "ALLEY");
        var state = await gameplay.EnableEasyMode(playerId);

        state.Attempt.Should().NotBeNull();
        state.Attempt!.PlayedInHardMode.Should().BeFalse();

        var relaxed = await gameplay.SubmitGuess(playerId, "PASTE");

        relaxed.Attempt!.Guesses.Should().HaveCount(2);
        relaxed.Attempt.PlayedInHardMode.Should().BeFalse();
    }
}
