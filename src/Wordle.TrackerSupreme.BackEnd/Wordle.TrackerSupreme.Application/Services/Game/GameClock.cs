using Wordle.TrackerSupreme.Domain.Models;

using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Application.Services.Game;

public class GameClock(GameOptions options) : IGameClock
{
    private readonly GameOptions _options = options;

    public DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.Now.Date);

    public DateTimeOffset Now => DateTimeOffset.Now;

    public DateTimeOffset GetRevealInstant(DateOnly puzzleDate)
    {
        var revealLocal = puzzleDate.ToDateTime(new TimeOnly(_options.RevealHourLocal, 0), DateTimeKind.Local);
        return new DateTimeOffset(revealLocal);
    }

    public bool HasRevealPassed(DateOnly puzzleDate)
        => Now >= GetRevealInstant(puzzleDate);

    public bool IsAfterReveal(PlayerPuzzleAttempt attempt)
    {
        var puzzleDate = attempt.DailyPuzzle?.PuzzleDate ?? DateOnly.FromDateTime(attempt.CreatedOn);
        var createdUtc = DateTime.SpecifyKind(attempt.CreatedOn, DateTimeKind.Utc);
        var revealUtc = GetRevealInstant(puzzleDate).ToUniversalTime().UtcDateTime;
        return createdUtc >= revealUtc;
    }
}
