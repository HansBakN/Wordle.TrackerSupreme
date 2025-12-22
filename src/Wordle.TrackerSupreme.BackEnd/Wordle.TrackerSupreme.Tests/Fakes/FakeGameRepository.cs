using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;

namespace Wordle.TrackerSupreme.Tests.Fakes;

public class FakeGameRepository : IGameRepository
{
    private readonly List<DailyPuzzle> _puzzles = [];
    private readonly List<PlayerPuzzleAttempt> _attempts = [];
    private readonly List<GuessAttempt> _guesses = [];

    public Task AddAttempt(PlayerPuzzleAttempt attempt, CancellationToken cancellationToken)
    {
        _attempts.Add(attempt);
        return Task.CompletedTask;
    }

    public Task AddGuess(GuessAttempt guessAttempt, IReadOnlyCollection<LetterEvaluation> feedback, CancellationToken cancellationToken)
    {
        _guesses.Add(guessAttempt);
        return Task.CompletedTask;
    }

    public Task AddPuzzle(DailyPuzzle puzzle, CancellationToken cancellationToken)
    {
        _puzzles.Add(puzzle);
        return Task.CompletedTask;
    }

    public Task<PlayerPuzzleAttempt?> GetAttempt(Guid playerId, Guid puzzleId, CancellationToken cancellationToken)
    {
        var attempt = _attempts.FirstOrDefault(a => a.PlayerId == playerId && a.DailyPuzzleId == puzzleId);
        return Task.FromResult<PlayerPuzzleAttempt?>(attempt);
    }

    public Task<PlayerPuzzleAttempt?> GetAttemptWithDetails(Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = _attempts.FirstOrDefault(a => a.Id == attemptId);
        if (attempt is not null)
        {
            attempt.Guesses = _guesses
                .Where(g => g.PlayerPuzzleAttemptId == attempt.Id)
                .OrderBy(g => g.GuessNumber)
                .ToList();
        }

        return Task.FromResult<PlayerPuzzleAttempt?>(attempt);
    }

    public Task<List<PlayerPuzzleAttempt>> GetAttemptsForPuzzle(Guid puzzleId, CancellationToken cancellationToken)
    {
        var attempts = _attempts.Where(a => a.DailyPuzzleId == puzzleId).ToList();
        foreach (var attempt in attempts)
        {
            attempt.Guesses = _guesses.Where(g => g.PlayerPuzzleAttemptId == attempt.Id).OrderBy(g => g.GuessNumber).ToList();
        }

        return Task.FromResult(attempts);
    }

    public Task<DailyPuzzle?> GetPuzzleByDate(DateOnly puzzleDate, CancellationToken cancellationToken)
    {
        var puzzle = _puzzles.FirstOrDefault(p => p.PuzzleDate == puzzleDate);
        return Task.FromResult<DailyPuzzle?>(puzzle);
    }

    public Task RemoveGuesses(IReadOnlyCollection<GuessAttempt> guesses, CancellationToken cancellationToken)
    {
        foreach (var guess in guesses)
        {
            _guesses.Remove(guess);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAttempt(PlayerPuzzleAttempt attempt, CancellationToken cancellationToken)
    {
        _attempts.Remove(attempt);
        return Task.CompletedTask;
    }

    public Task SaveChanges(CancellationToken cancellationToken) => Task.CompletedTask;

    public IReadOnlyCollection<DailyPuzzle> Puzzles => _puzzles;
    public IReadOnlyCollection<PlayerPuzzleAttempt> Attempts => _attempts;
    public IReadOnlyCollection<GuessAttempt> Guesses => _guesses;
}
