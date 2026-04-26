namespace Wordle.TrackerSupreme.Domain.Models.Analyzer;

public sealed record AnalyzerResult
{
    public required string AnalyzerVersion { get; init; }
    public required AnalyzerMode Mode { get; init; }
    public required string Answer { get; init; }
    public required int PlayerSteps { get; init; }
    public required bool Solved { get; init; }
    public required IReadOnlyList<AnalyzerTurn> Turns { get; init; }
}
