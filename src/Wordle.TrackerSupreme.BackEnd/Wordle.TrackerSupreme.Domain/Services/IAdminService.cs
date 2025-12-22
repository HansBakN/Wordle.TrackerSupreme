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
}
