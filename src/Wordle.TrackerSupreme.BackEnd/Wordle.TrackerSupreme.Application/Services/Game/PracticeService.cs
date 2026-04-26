using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;
using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Application.Services.Game;

public class PracticeService(
    IGameRepository gameRepository,
    IWordSelector wordSelector,
    IGuessEvaluationService guessEvaluationService,
    GameOptions options)
    : IPracticeService
{
    public async Task<PracticeState> StartNewGame(Guid playerId, CancellationToken cancellationToken = default)
    {
        var word = wordSelector.SelectRandomWord();

        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Solution = word,
            IsPractice = true
        };
        await gameRepository.AddPuzzle(puzzle, cancellationToken);

        var attempt = new PlayerPuzzleAttempt
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            DailyPuzzleId = puzzle.Id,
            Status = AttemptStatus.InProgress,
            CreatedOn = DateTime.UtcNow,
            PlayedInHardMode = true
        };
        await gameRepository.AddAttempt(attempt, cancellationToken);
        await gameRepository.SaveChanges(cancellationToken);

        attempt.DailyPuzzle = puzzle;

        return new PracticeState(puzzle, attempt, false, options.WordLength, options.MaxGuesses);
    }

    public async Task<PracticeState?> GetActiveGame(Guid playerId, CancellationToken cancellationToken = default)
    {
        var attempt = await gameRepository.GetActivePracticeAttempt(playerId, cancellationToken);
        if (attempt is null)
        {
            return null;
        }

        return new PracticeState(attempt.DailyPuzzle, attempt, false, options.WordLength, options.MaxGuesses);
    }

    public async Task<PracticeState> SubmitGuess(Guid playerId, string guessWord, CancellationToken cancellationToken = default)
    {
        var normalizedGuess = guessEvaluationService.NormalizeGuess(guessWord);

        var attempt = await gameRepository.GetActivePracticeAttempt(playerId, cancellationToken);
        if (attempt is null)
        {
            throw new InvalidOperationException("No active practice game. Start a new one first.");
        }

        if (attempt.Guesses.Count >= options.MaxGuesses)
        {
            attempt.Status = AttemptStatus.Failed;
            attempt.CompletedOn = DateTime.UtcNow;
            await gameRepository.SaveChanges(cancellationToken);
            throw new InvalidOperationException("No guesses remaining.");
        }

        var guessNumber = attempt.Guesses.Count + 1;
        var feedback = guessEvaluationService.EvaluateGuess(attempt.DailyPuzzle.Solution ?? string.Empty, normalizedGuess)
            .ToList();

        var guessAttempt = new GuessAttempt
        {
            Id = Guid.NewGuid(),
            PlayerPuzzleAttemptId = attempt.Id,
            GuessNumber = guessNumber,
            GuessWord = normalizedGuess,
            Feedback = feedback
        };

        foreach (var fb in feedback)
        {
            fb.GuessAttemptId = guessAttempt.Id;
            fb.GuessAttempt = guessAttempt;
        }

        attempt.Guesses.Add(guessAttempt);
        await gameRepository.AddGuess(guessAttempt, feedback, cancellationToken);

        var solved = feedback.All(e => e.Result == LetterResult.Correct);
        if (solved)
        {
            attempt.Status = AttemptStatus.Solved;
            attempt.CompletedOn = DateTime.UtcNow;
        }
        else if (guessNumber >= options.MaxGuesses)
        {
            attempt.Status = AttemptStatus.Failed;
            attempt.CompletedOn = DateTime.UtcNow;
        }

        await gameRepository.SaveChanges(cancellationToken);

        var solutionRevealed = attempt.Status is AttemptStatus.Solved or AttemptStatus.Failed;

        return new PracticeState(attempt.DailyPuzzle, attempt, solutionRevealed, options.WordLength, options.MaxGuesses);
    }
}
