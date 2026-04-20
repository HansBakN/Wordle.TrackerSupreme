using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Domain.Services;

public interface IAdminService
{
    Task<IReadOnlyList<Player>> GetPlayers(CancellationToken cancellationToken);
    Task<Player?> GetPlayer(Guid playerId, CancellationToken cancellationToken);
    Task<Player> UpdatePlayerProfile(Guid playerId, string displayName, string email, CancellationToken cancellationToken);
    Task<Player> ResetPassword(Guid playerId, string newPassword, CancellationToken cancellationToken);
    Task<Player> SetAdminStatus(Guid playerId, bool isAdmin, CancellationToken cancellationToken);
    Task<PlayerPuzzleAttempt> UpdateAttempt(Guid attemptId, IReadOnlyList<string> guesses, bool playedInHardMode, CancellationToken cancellationToken);
    Task DeleteAttempt(Guid attemptId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DailyPuzzle>> GetPuzzles(CancellationToken cancellationToken);
    Task<DailyPuzzle?> GetPuzzle(Guid puzzleId, CancellationToken cancellationToken);
    Task<DailyPuzzle> CreatePuzzle(DateOnly puzzleDate, string solution, CancellationToken cancellationToken);
    Task<DailyPuzzle> UpdatePuzzle(Guid puzzleId, DateOnly puzzleDate, string solution, CancellationToken cancellationToken);
    Task DeletePuzzle(Guid puzzleId, CancellationToken cancellationToken);
}
