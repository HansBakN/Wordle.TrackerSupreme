using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Tests.Fakes;

public class FakeGameClock(DateOnly today, bool revealPassed = false) : IGameClock
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
    public DateOnly Today => today;
    public DateTimeOffset GetRevealInstant(DateOnly puzzleDate) => new DateTimeOffset(puzzleDate.ToDateTime(TimeOnly.MinValue)).AddHours(12);
    public bool HasRevealPassed(DateOnly puzzleDate) => revealPassed;
    public bool IsAfterReveal(PlayerPuzzleAttempt attempt)
    {
        var date = attempt.DailyPuzzle?.PuzzleDate ?? Today;
        return HasRevealPassed(date);
    }
}
