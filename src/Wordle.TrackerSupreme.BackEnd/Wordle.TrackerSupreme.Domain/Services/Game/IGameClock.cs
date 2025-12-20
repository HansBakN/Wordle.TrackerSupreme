using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Domain.Services.Game;

public interface IGameClock
{
    DateOnly Today { get; }
    DateTimeOffset Now { get; }
    DateTimeOffset GetRevealInstant(DateOnly puzzleDate);
    bool HasRevealPassed(DateOnly puzzleDate);
    bool IsAfterReveal(PlayerPuzzleAttempt attempt);
}
