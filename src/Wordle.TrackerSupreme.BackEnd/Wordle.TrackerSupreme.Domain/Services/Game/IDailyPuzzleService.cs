using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Domain.Services.Game;

public interface IDailyPuzzleService
{
    Task<DailyPuzzle> GetOrCreatePuzzle(DateOnly puzzleDate, CancellationToken cancellationToken);
}
