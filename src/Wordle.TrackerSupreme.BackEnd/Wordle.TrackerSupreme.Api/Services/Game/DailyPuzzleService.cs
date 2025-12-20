using Microsoft.EntityFrameworkCore;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Infrastructure.Database;

namespace Wordle.TrackerSupreme.Api.Services.Game;

public class DailyPuzzleService(
    WordleTrackerSupremeDbContext dbContext,
    WordSelector wordSelector)
{
    public async Task<DailyPuzzle> GetOrCreatePuzzle(DateOnly puzzleDate, CancellationToken cancellationToken)
    {
        var existing = await dbContext.DailyPuzzles.FirstOrDefaultAsync(p => p.PuzzleDate == puzzleDate, cancellationToken);
        if (existing is not null)
        {
            if (string.IsNullOrWhiteSpace(existing.Solution))
            {
                existing.Solution = wordSelector.GetSolutionFor(puzzleDate);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return existing;
        }

        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = puzzleDate,
            Solution = wordSelector.GetSolutionFor(puzzleDate),
            IsArchived = false
        };

        dbContext.DailyPuzzles.Add(puzzle);
        await dbContext.SaveChangesAsync(cancellationToken);
        return puzzle;
    }
}
