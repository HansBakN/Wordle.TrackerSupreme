using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Domain.Repositories;

public interface IPlayerRepository
{
    Task<Player?> GetPlayerWithAttempts(Guid playerId, CancellationToken cancellationToken);
}
