using Microsoft.EntityFrameworkCore;
using Wordle.TrackerSupreme.Domain.Exceptions;
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
        catch (DbUpdateException ex)
        {
            if (await IsDuplicateAttemptConflict(ex, cancellationToken))
            {
                throw new DuplicatePuzzleAttemptException();
            }

            if (await IsDuplicateGuessConflict(ex, cancellationToken))
            {
                throw new InvalidOperationException("A guess was already recorded for this slot. Refresh to see the current state.");
            }

            throw;
        }
    }

    private async Task<bool> IsDuplicateAttemptConflict(DbUpdateException ex, CancellationToken cancellationToken)
    {
        var pendingAttempts = ex.Entries
            .Select(entry => entry.Entity)
            .OfType<PlayerPuzzleAttempt>()
            .Where(attempt => attempt.PlayerId != Guid.Empty && attempt.DailyPuzzleId != Guid.Empty)
            .ToList();

        if (pendingAttempts.Count == 0)
        {
            return false;
        }

        foreach (var attempt in pendingAttempts)
        {
            var hasExistingAttempt = await dbContext.Attempts
                .AsNoTracking()
                .AnyAsync(
                    existing => existing.Id != attempt.Id &&
                                existing.PlayerId == attempt.PlayerId &&
                                existing.DailyPuzzleId == attempt.DailyPuzzleId,
                    cancellationToken);

            if (hasExistingAttempt)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> IsDuplicateGuessConflict(DbUpdateException ex, CancellationToken cancellationToken)
    {
        var pendingGuesses = ex.Entries
            .Select(entry => entry.Entity)
            .OfType<GuessAttempt>()
            .Where(g => g.PlayerPuzzleAttemptId != Guid.Empty && g.GuessNumber > 0)
            .ToList();

        if (pendingGuesses.Count == 0)
        {
            return false;
        }

        foreach (var guess in pendingGuesses)
        {
            var hasExistingGuess = await dbContext.Guesses
                .AsNoTracking()
                .AnyAsync(
                    existing => existing.Id != guess.Id &&
                                existing.PlayerPuzzleAttemptId == guess.PlayerPuzzleAttemptId &&
                                existing.GuessNumber == guess.GuessNumber,
                    cancellationToken);

            if (hasExistingGuess)
            {
                return true;
            }
        }

        return false;
    }
}
