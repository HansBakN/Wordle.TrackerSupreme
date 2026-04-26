namespace Wordle.TrackerSupreme.Domain.Models.Analyzer;

public sealed record AnalyzerTurn
{
    public required string Guess { get; init; }
    public required IReadOnlyList<LetterResult> Feedback { get; init; }
    public required int PossibleAnswersRemainingCount { get; init; }
    public required IReadOnlyList<RemainingAnswer> PossibleAnswersRemaining { get; init; }
}
