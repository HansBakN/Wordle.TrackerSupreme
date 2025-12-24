namespace Wordle.TrackerSupreme.Domain.Services.Game;

public interface IWordListProvider
{
    IReadOnlyList<string> Words { get; }
}
