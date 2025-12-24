using Microsoft.EntityFrameworkCore;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;
using Wordle.TrackerSupreme.Infrastructure.Database;

namespace Wordle.TrackerSupreme.Infrastructure.Repositories;

public class GameRepository(WordleTrackerSupremeDbContext dbContext) : IGameRepository
{
    public Task<DailyPuzzle?> GetPuzzleByDate(DateOnly puzzleDate, CancellationToken cancellationToken)
        => dbContext.DailyPuzzles.FirstOrDefaultAsync(p => p.PuzzleDate == puzzleDate, cancellationToken);

    public Task AddPuzzle(DailyPuzzle puzzle, CancellationToken cancellationToken)
    {
        dbContext.DailyPuzzles.Add(puzzle);
        return Task.CompletedTask;
    }

    public Task<PlayerPuzzleAttempt?> GetAttempt(Guid playerId, Guid puzzleId, CancellationToken cancellationToken)
        => dbContext.Attempts
            .Include(a => a.Guesses)
                .ThenInclude(g => g.Feedback)
            .FirstOrDefaultAsync(a => a.PlayerId == playerId && a.DailyPuzzleId == puzzleId, cancellationToken);

    public Task<PlayerPuzzleAttempt?> GetAttemptWithDetails(Guid attemptId, CancellationToken cancellationToken)
        => dbContext.Attempts
            .Include(a => a.DailyPuzzle)
            .Include(a => a.Guesses)
                .ThenInclude(g => g.Feedback)
            .FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken);

    public Task AddAttempt(PlayerPuzzleAttempt attempt, CancellationToken cancellationToken)
    {
        dbContext.Attempts.Add(attempt);
        return Task.CompletedTask;
    }

    public Task AddGuess(GuessAttempt guessAttempt, IReadOnlyCollection<LetterEvaluation> feedback, CancellationToken cancellationToken)
    {
        dbContext.Guesses.Add(guessAttempt);
        dbContext.LetterEvaluations.AddRange(feedback);
        return Task.CompletedTask;
    }

    public Task<List<PlayerPuzzleAttempt>> GetAttemptsForPuzzle(Guid puzzleId, CancellationToken cancellationToken)
        => dbContext.Attempts
            .Include(a => a.Player)
            .Include(a => a.Guesses)
            .Where(a => a.DailyPuzzleId == puzzleId)
            .ToListAsync(cancellationToken);

    public Task RemoveGuesses(IReadOnlyCollection<GuessAttempt> guesses, CancellationToken cancellationToken)
    {
        if (guesses.Count == 0)
        {
            return Task.CompletedTask;
        }

        var feedback = guesses.SelectMany(g => g.Feedback).ToList();
        dbContext.LetterEvaluations.RemoveRange(feedback);
        dbContext.Guesses.RemoveRange(guesses);
        return Task.CompletedTask;
    }

    public Task RemoveAttempt(PlayerPuzzleAttempt attempt, CancellationToken cancellationToken)
    {
        dbContext.Attempts.Remove(attempt);
        return Task.CompletedTask;
    }

    public async Task SaveChanges(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var details = ex.Entries
                .Select(e => $"{e.Entity.GetType().Name} (state: {e.State})")
                .ToList();
            throw new InvalidOperationException(
                $"Could not save game changes due to a stale state. Entities: {string.Join(", ", details)}",
                ex);
        }
    }
}
