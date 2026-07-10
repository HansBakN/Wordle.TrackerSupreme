using System;
using System.Threading;
using System.Threading.Tasks;
using Wordle.TrackerSupreme.Domain.Services.Game;
using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Tests.Fakes;

public class FakeOfficialWordProvider : IOfficialWordProvider
{
    private readonly Func<DateOnly, CancellationToken, Task<string>> _resolver;

    public FakeOfficialWordProvider(Func<DateOnly, CancellationToken, Task<string>> resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public FakeOfficialWordProvider(string solution)
        : this((_, _) => Task.FromResult(solution))
    {
    }

    public int CallCount { get; private set; }

    public Task<string> GetSolutionForDateAsync(DateOnly puzzleDate, CancellationToken cancellationToken)
    {
        CallCount++;
        return _resolver(puzzleDate, cancellationToken);
    }

    public async Task<NytPuzzleMetadata> GetMetadataForDateAsync(DateOnly puzzleDate, CancellationToken cancellationToken)
    {
        var solution = await GetSolutionForDateAsync(puzzleDate, cancellationToken);
        var number = puzzleDate.DayNumber - new DateOnly(2021, 6, 19).DayNumber;
        return new NytPuzzleMetadata(number + 1, puzzleDate, number, solution);
    }
}
