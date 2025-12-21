using Microsoft.EntityFrameworkCore;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;
using Wordle.TrackerSupreme.Infrastructure.Database;

namespace Wordle.TrackerSupreme.Infrastructure.Repositories;

public class PlayerRepository(WordleTrackerSupremeDbContext dbContext) : IPlayerRepository
{
    public Task<Player?> GetPlayerWithAttempts(Guid playerId, CancellationToken cancellationToken)
        => dbContext.Players
            .Include(p => p.Attempts)
                .ThenInclude(a => a.DailyPuzzle)
            .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);

    public Task<List<Player>> GetPlayersWithAttempts(CancellationToken cancellationToken)
        => dbContext.Players
            .Include(p => p.Attempts)
                .ThenInclude(a => a.DailyPuzzle)
            .ToListAsync(cancellationToken);
}
