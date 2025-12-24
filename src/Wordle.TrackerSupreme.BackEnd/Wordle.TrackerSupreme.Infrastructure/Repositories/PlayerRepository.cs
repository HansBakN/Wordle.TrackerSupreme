using Microsoft.EntityFrameworkCore;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;
using Wordle.TrackerSupreme.Infrastructure.Database;

namespace Wordle.TrackerSupreme.Infrastructure.Repositories;

public class PlayerRepository(WordleTrackerSupremeDbContext dbContext) : IPlayerRepository
{
    public Task<Player?> GetPlayer(Guid playerId, CancellationToken cancellationToken)
        => dbContext.Players.FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);

    public Task<Player?> GetPlayerWithAttempts(Guid playerId, CancellationToken cancellationToken)
        => dbContext.Players
            .Include(p => p.Attempts)
                .ThenInclude(a => a.DailyPuzzle)
            .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);

    public Task<Player?> GetPlayerWithAttemptsAndGuesses(Guid playerId, CancellationToken cancellationToken)
        => dbContext.Players
            .Include(p => p.Attempts)
                .ThenInclude(a => a.DailyPuzzle)
            .Include(p => p.Attempts)
                .ThenInclude(a => a.Guesses)
                    .ThenInclude(g => g.Feedback)
            .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);

    public Task<List<Player>> GetPlayers(CancellationToken cancellationToken)
        => dbContext.Players.ToListAsync(cancellationToken);

    public Task<List<Player>> GetPlayersWithAttempts(CancellationToken cancellationToken)
        => dbContext.Players
            .Include(p => p.Attempts)
                .ThenInclude(a => a.DailyPuzzle)
            .ToListAsync(cancellationToken);

    public Task<bool> IsDisplayNameTaken(string displayName, Guid? excludePlayerId, CancellationToken cancellationToken)
    {
        return dbContext.Players.AnyAsync(
            p => p.DisplayName == displayName && (excludePlayerId == null || p.Id != excludePlayerId),
            cancellationToken);
    }

    public Task<bool> IsEmailTaken(string email, Guid? excludePlayerId, CancellationToken cancellationToken)
    {
        return dbContext.Players.AnyAsync(
            p => p.Email == email && (excludePlayerId == null || p.Id != excludePlayerId),
            cancellationToken);
    }

    public async Task SaveChanges(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
