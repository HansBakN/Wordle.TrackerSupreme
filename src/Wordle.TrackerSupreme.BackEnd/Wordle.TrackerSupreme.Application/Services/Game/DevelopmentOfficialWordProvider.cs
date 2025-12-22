using System;
using System.Threading;
using System.Threading.Tasks;
using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Application.Services.Game;

public class DevelopmentOfficialWordProvider : IOfficialWordProvider
{
    private readonly IWordSelector _wordSelector;

    public DevelopmentOfficialWordProvider(IWordSelector wordSelector)
    {
        _wordSelector = wordSelector;
    }

    public Task<string> GetSolutionForDateAsync(DateOnly puzzleDate, CancellationToken cancellationToken)
    {
        return Task.FromResult(_wordSelector.GetSolutionFor(puzzleDate));
    }
}
