namespace Wordle.TrackerSupreme.Domain.Models.Analyzer;

public sealed record AnalyzerInput
{
    public required IReadOnlyList<string> Guesses { get; init; }
    public required string Answer { get; init; }
    public required AnalyzerMode Mode { get; init; }
    public required string AnalyzerVersion { get; init; }
}
