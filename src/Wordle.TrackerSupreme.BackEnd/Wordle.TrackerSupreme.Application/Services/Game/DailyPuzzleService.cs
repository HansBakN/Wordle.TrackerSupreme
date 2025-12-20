using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;
using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Application.Services.Game;

public class DailyPuzzleService(
    IGameRepository gameRepository,
    IWordSelector wordSelector)
    : IDailyPuzzleService
{
    public async Task<DailyPuzzle> GetOrCreatePuzzle(DateOnly puzzleDate, CancellationToken cancellationToken)
    {
        var existing = await gameRepository.GetPuzzleByDate(puzzleDate, cancellationToken);
        if (existing is not null)
        {
            if (string.IsNullOrWhiteSpace(existing.Solution))
            {
                existing.Solution = wordSelector.GetSolutionFor(puzzleDate);
                await gameRepository.SaveChanges(cancellationToken);
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

        await gameRepository.AddPuzzle(puzzle, cancellationToken);
        await gameRepository.SaveChanges(cancellationToken);
        return puzzle;
    }
}
