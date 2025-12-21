namespace Wordle.TrackerSupreme.Domain.Models;

public record PlayerStatisticsFilter
{
    public bool IncludeHardMode { get; init; } = true;
    public bool IncludeEasyMode { get; init; } = true;
    public bool IncludeBeforeReveal { get; init; } = true;
    public bool IncludeAfterReveal { get; init; } = true;
    public bool IncludeSolved { get; init; } = true;
    public bool IncludeFailed { get; init; } = true;
    public bool IncludeInProgress { get; init; } = true;
    public bool CountPracticeAttempts { get; init; } = false;
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public int? MinGuessCount { get; init; }
    public int? MaxGuessCount { get; init; }
}
