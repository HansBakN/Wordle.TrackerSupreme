using System;
using System.Threading;
using System.Threading.Tasks;
using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Domain.Services.Game;

public interface IOfficialWordProvider
{
    Task<string> GetSolutionForDateAsync(DateOnly puzzleDate, CancellationToken cancellationToken);
    Task<NytPuzzleMetadata> GetMetadataForDateAsync(DateOnly puzzleDate, CancellationToken cancellationToken);
}
