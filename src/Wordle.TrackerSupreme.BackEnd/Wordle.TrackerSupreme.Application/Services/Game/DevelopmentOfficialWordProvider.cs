using System;
using System.Threading;
using System.Threading.Tasks;
using Wordle.TrackerSupreme.Domain.Services.Game;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Application.Services.Import;

namespace Wordle.TrackerSupreme.Application.Services.Game;

public class DevelopmentOfficialWordProvider : IOfficialWordProvider
{
    private readonly IWordSelector _wordSelector;
    private readonly NytPuzzleCatalogue _catalogue;

    public DevelopmentOfficialWordProvider(IWordSelector wordSelector, NytPuzzleCatalogue catalogue)
    {
        _wordSelector = wordSelector;
        _catalogue = catalogue;
    }

    public Task<string> GetSolutionForDateAsync(DateOnly puzzleDate, CancellationToken cancellationToken)
    {
        return Task.FromResult(_wordSelector.GetSolutionFor(puzzleDate));
    }

    public Task<NytPuzzleMetadata> GetMetadataForDateAsync(DateOnly puzzleDate, CancellationToken cancellationToken)
    {
        var metadata = _catalogue.Published.SingleOrDefault(entry => entry.PrintDate == puzzleDate)
            ?? throw new KeyNotFoundException($"No published NYT puzzle metadata is available for {puzzleDate:yyyy-MM-dd}.");
        return Task.FromResult(metadata with { Solution = _wordSelector.GetSolutionFor(puzzleDate) });
    }
}
