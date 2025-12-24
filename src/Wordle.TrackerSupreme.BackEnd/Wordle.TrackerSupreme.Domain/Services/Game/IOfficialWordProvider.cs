using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wordle.TrackerSupreme.Domain.Services.Game;

public interface IOfficialWordProvider
{
    Task<string> GetSolutionForDateAsync(DateOnly puzzleDate, CancellationToken cancellationToken);
}
