using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Api.Models.Game;

public record PlayerStatsFilterRequest(
    bool IncludeHardMode = true,
    bool IncludeEasyMode = false,
    bool IncludeBeforeReveal = true,
    bool IncludeAfterReveal = false,
    bool IncludeSolved = true,
    bool IncludeFailed = true,
    bool IncludeInProgress = false,
    bool CountPracticeAttempts = false,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int? MinGuessCount = null,
    int? MaxGuessCount = null,
    IReadOnlyList<PuzzleStream>? Streams = null)
{
    public PlayerStatisticsFilter ToFilter()
        => new()
        {
            Streams = NormalizeStreams(Streams),
            IncludeHardMode = IncludeHardMode,
            IncludeEasyMode = IncludeEasyMode,
            IncludeBeforeReveal = IncludeBeforeReveal,
            IncludeAfterReveal = IncludeAfterReveal,
            IncludeSolved = IncludeSolved,
            IncludeFailed = IncludeFailed,
            IncludeInProgress = IncludeInProgress,
            CountPracticeAttempts = CountPracticeAttempts,
            FromDate = FromDate,
            ToDate = ToDate,
            MinGuessCount = MinGuessCount,
            MaxGuessCount = MaxGuessCount
        };

    private static IReadOnlyList<PuzzleStream> NormalizeStreams(IReadOnlyList<PuzzleStream>? streams)
    {
        if (streams is null || streams.Count == 0)
        {
            return [PuzzleStream.TrackerSupreme];
        }

        return streams
            .Distinct()
            .OrderBy(stream => stream)
            .ToList();
    }
}
