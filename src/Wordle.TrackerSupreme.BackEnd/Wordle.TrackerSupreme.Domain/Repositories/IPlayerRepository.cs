using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Domain.Repositories;

public interface IPlayerRepository
{
    Task<Player?> GetPlayer(Guid playerId, CancellationToken cancellationToken);
    Task<Player?> GetPlayerWithAttempts(Guid playerId, CancellationToken cancellationToken);
    Task<Player?> GetPlayerWithAttemptsAndGuesses(Guid playerId, CancellationToken cancellationToken);
    Task<List<Player>> GetPlayers(CancellationToken cancellationToken);
    Task<List<Player>> GetPlayersWithAttempts(CancellationToken cancellationToken);
    Task<bool> IsDisplayNameTaken(string displayName, Guid? excludePlayerId, CancellationToken cancellationToken);
    Task<bool> IsEmailTaken(string email, Guid? excludePlayerId, CancellationToken cancellationToken);
    Task SaveChanges(CancellationToken cancellationToken);
}
