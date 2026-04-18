using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Domain.Repositories;

public interface IPlayerRepository
{
    Task<Player?> GetPlayer(Guid playerId, CancellationToken cancellationToken);
    Task<Player?> GetPlayerByEmail(string email, CancellationToken cancellationToken);
    Task<Player?> GetPlayerWithAttempts(Guid playerId, CancellationToken cancellationToken);
    Task<Player?> GetPlayerWithAttemptsAndGuesses(Guid playerId, CancellationToken cancellationToken);
    Task<List<Player>> GetPlayers(CancellationToken cancellationToken);
    Task<List<Player>> GetPlayersWithAttempts(CancellationToken cancellationToken);
    Task<(List<Player> Players, int TotalCount)> GetPlayersPage(string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task AddPlayer(Player player, CancellationToken cancellationToken);
    Task<bool> IsDisplayNameTaken(string displayName, Guid? excludePlayerId, CancellationToken cancellationToken);
    Task<bool> IsEmailTaken(string email, Guid? excludePlayerId, CancellationToken cancellationToken);
    Task SaveChanges(CancellationToken cancellationToken);
}
