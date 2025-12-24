namespace Wordle.TrackerSupreme.Domain.Services.Game;

public interface IWordSelector
{
    string GetSolutionFor(DateOnly puzzleDate);
}
