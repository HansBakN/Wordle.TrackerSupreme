using System;
using System.Threading;
using System.Threading.Tasks;
using Wordle.TrackerSupreme.Domain.Services.Game;

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
}
