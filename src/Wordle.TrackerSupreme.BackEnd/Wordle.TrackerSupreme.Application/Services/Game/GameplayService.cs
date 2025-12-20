using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;
using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Application.Services.Game;

public class GameplayService(
    IGameRepository gameRepository,
    IDailyPuzzleService puzzleService,
    IGameClock gameClock,
    GameOptions options)
    : IGameplayService
{
    private readonly GameOptions _options = options;

    public async Task<GameplayState> GetState(Guid playerId, CancellationToken cancellationToken = default)
    {
        var puzzle = await puzzleService.GetOrCreatePuzzle(gameClock.Today, cancellationToken);
        var attempt = await LoadAttempt(playerId, puzzle.Id, cancellationToken);

        var cutoffPassed = gameClock.HasRevealPassed(puzzle.PuzzleDate);
        var solutionRevealed = cutoffPassed || attempt?.Status is AttemptStatus.Solved or AttemptStatus.Failed;

        return new GameplayState(
            puzzle,
            attempt,
            cutoffPassed,
            solutionRevealed,
            AllowLatePlay: true,
            _options.WordLength,
            _options.MaxGuesses);
    }

    public async Task<GameplayState> SubmitGuess(Guid playerId, string guessWord, CancellationToken cancellationToken = default)
    {
        var normalizedGuess = NormalizeGuess(guessWord);
        var puzzle = await puzzleService.GetOrCreatePuzzle(gameClock.Today, cancellationToken);

        var attempt = await LoadAttempt(playerId, puzzle.Id, cancellationToken);
        if (attempt is null)
        {
            attempt = new PlayerPuzzleAttempt
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                DailyPuzzleId = puzzle.Id,
                Status = AttemptStatus.InProgress,
                CreatedOn = DateTime.UtcNow,
                PlayedInHardMode = false
            };
            await gameRepository.AddAttempt(attempt, cancellationToken);
        }

        if (attempt.Status is AttemptStatus.Solved or AttemptStatus.Failed)
        {
            throw new InvalidOperationException("Puzzle already completed for today.");
        }

        if (attempt.Guesses.Count >= _options.MaxGuesses)
        {
            attempt.Status = AttemptStatus.Failed;
            attempt.CompletedOn = attempt.CompletedOn ?? DateTime.UtcNow;
            await gameRepository.SaveChanges(cancellationToken);
            throw new InvalidOperationException("No guesses remaining.");
        }

        var guessNumber = attempt.Guesses.Count + 1;
        var feedback = EvaluateGuess(puzzle.Solution ?? string.Empty, normalizedGuess);

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
        else if (guessNumber >= _options.MaxGuesses)
        {
            attempt.Status = AttemptStatus.Failed;
            attempt.CompletedOn = DateTime.UtcNow;
        }

        await gameRepository.SaveChanges(cancellationToken);

        var cutoffPassed = gameClock.HasRevealPassed(puzzle.PuzzleDate);
        var solutionRevealed = cutoffPassed || attempt.Status != AttemptStatus.InProgress;

        return new GameplayState(
            puzzle,
            attempt,
            cutoffPassed,
            solutionRevealed,
            AllowLatePlay: true,
            _options.WordLength,
            _options.MaxGuesses);
    }

    public async Task<SolutionsSnapshot> GetSolutions(CancellationToken cancellationToken = default)
    {
        var puzzle = await puzzleService.GetOrCreatePuzzle(gameClock.Today, cancellationToken);
        var cutoffPassed = gameClock.HasRevealPassed(puzzle.PuzzleDate);

        var attempts = await gameRepository.GetAttemptsForPuzzle(puzzle.Id, cancellationToken);

        return new SolutionsSnapshot(puzzle, cutoffPassed, attempts);
    }

    private async Task<PlayerPuzzleAttempt?> LoadAttempt(Guid playerId, Guid puzzleId, CancellationToken cancellationToken)
    {
        return await gameRepository.GetAttempt(playerId, puzzleId, cancellationToken);
    }

    private string NormalizeGuess(string guessWord)
    {
        if (string.IsNullOrWhiteSpace(guessWord))
        {
            throw new ArgumentException("Guess cannot be empty.", nameof(guessWord));
        }

        var cleaned = guessWord.Trim().ToUpperInvariant();
        if (cleaned.Length != _options.WordLength)
        {
            throw new ArgumentException($"Guess must be {_options.WordLength} letters long.", nameof(guessWord));
        }

        if (!cleaned.All(char.IsLetter))
        {
            throw new ArgumentException("Guess must contain only letters.", nameof(guessWord));
        }

        return cleaned;
    }

    private List<LetterEvaluation> EvaluateGuess(string solution, string guess)
    {
        solution = solution.ToUpperInvariant();
        guess = guess.ToUpperInvariant();

        var feedback = new LetterEvaluation[_options.WordLength];
        var solutionChars = solution.ToCharArray();
        var guessChars = guess.ToCharArray();

        // First pass: correct positions.
        for (int i = 0; i < _options.WordLength; i++)
        {
            if (guessChars[i] == solutionChars[i])
            {
                feedback[i] = new LetterEvaluation
                {
                    Id = Guid.NewGuid(),
                    Position = i,
                    Letter = guessChars[i],
                    Result = LetterResult.Correct
                };
                solutionChars[i] = '*';
            }
        }

        // Second pass: present vs absent.
        for (int i = 0; i < _options.WordLength; i++)
        {
            if (feedback[i] is not null)
            {
                continue;
            }

            var letter = guessChars[i];
            var index = Array.IndexOf(solutionChars, letter);
            var result = index >= 0 ? LetterResult.Present : LetterResult.Absent;

            feedback[i] = new LetterEvaluation
            {
                Id = Guid.NewGuid(),
                Position = i,
                Letter = letter,
                Result = result
            };

            if (index >= 0)
            {
                solutionChars[index] = '*';
            }
        }

        return feedback.ToList();
    }
}
