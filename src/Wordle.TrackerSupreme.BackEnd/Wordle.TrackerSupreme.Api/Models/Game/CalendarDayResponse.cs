namespace Wordle.TrackerSupreme.Api.Models.Game;

public record CalendarDayResponse(
    DateOnly Date,
    string Outcome,
    int? GuessCount,
    bool IsAfterReveal);

public record CalendarResponse(IReadOnlyList<CalendarDayResponse> Days);
