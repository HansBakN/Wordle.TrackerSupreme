using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Domain.Repositories;

public interface IGameRepository
{
    Task<DailyPuzzle?> GetPuzzleByDate(DateOnly puzzleDate, CancellationToken cancellationToken);
    Task AddPuzzle(DailyPuzzle puzzle, CancellationToken cancellationToken);
    Task<PlayerPuzzleAttempt?> GetAttempt(Guid playerId, Guid puzzleId, CancellationToken cancellationToken);
    Task AddAttempt(PlayerPuzzleAttempt attempt, CancellationToken cancellationToken);
    Task AddGuess(GuessAttempt guessAttempt, IReadOnlyCollection<LetterEvaluation> feedback, CancellationToken cancellationToken);
    Task<List<PlayerPuzzleAttempt>> GetAttemptsForPuzzle(Guid puzzleId, CancellationToken cancellationToken);
    Task SaveChanges(CancellationToken cancellationToken);
}
