using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Infrastructure.Database;

namespace Wordle.TrackerSupreme.Api.Services.Game;

public record GameplayState(
    DailyPuzzle Puzzle,
    PlayerPuzzleAttempt? Attempt,
    bool CutoffPassed,
    bool SolutionRevealed,
    bool AllowLatePlay,
    int WordLength,
    int MaxGuesses);

public record SolutionsSnapshot(
    DailyPuzzle Puzzle,
    bool CutoffPassed,
    IReadOnlyCollection<PlayerPuzzleAttempt> Attempts);

public class GameplayService(
    WordleTrackerSupremeDbContext dbContext,
    DailyPuzzleService puzzleService,
    GameClock gameClock,
    IOptions<GameOptions> options)
{
    private readonly GameOptions _options = options.Value;

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
            dbContext.Attempts.Add(attempt);
        }

        await dbContext.Entry(attempt)
            .Collection(a => a.Guesses)
            .Query()
            .Include(g => g.Feedback)
            .LoadAsync(cancellationToken);

        if (attempt.Status is AttemptStatus.Solved or AttemptStatus.Failed)
        {
            throw new InvalidOperationException("Puzzle already completed for today.");
        }

        if (attempt.Guesses.Count >= _options.MaxGuesses)
        {
            attempt.Status = AttemptStatus.Failed;
            attempt.CompletedOn = attempt.CompletedOn ?? DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
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
        dbContext.Guesses.Add(guessAttempt);
        dbContext.LetterEvaluations.AddRange(feedback);

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

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var details = ex.Entries
                .Select(e => $"{e.Entity.GetType().Name} (state: {e.State})")
                .ToList();
            throw new InvalidOperationException($"Could not save guess due to a stale game state. Entities: {string.Join(", ", details)}", ex);
        }

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

        var attempts = await dbContext.Attempts
            .Include(a => a.Player)
            .Include(a => a.Guesses)
            .Where(a => a.DailyPuzzleId == puzzle.Id)
            .ToListAsync(cancellationToken);

        return new SolutionsSnapshot(puzzle, cutoffPassed, attempts);
    }

    private async Task<PlayerPuzzleAttempt?> LoadAttempt(Guid playerId, Guid puzzleId, CancellationToken cancellationToken)
    {
        return await dbContext.Attempts
            .Include(a => a.Guesses)
                .ThenInclude(g => g.Feedback)
            .FirstOrDefaultAsync(a => a.PlayerId == playerId && a.DailyPuzzleId == puzzleId, cancellationToken);
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
