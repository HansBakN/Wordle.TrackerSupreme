using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Tests.Fakes;

public class FakeWordSelector(string solution) : IWordSelector
{
    public string GetSolutionFor(DateOnly puzzleDate) => solution;
}
