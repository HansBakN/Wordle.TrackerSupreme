namespace Wordle.TrackerSupreme.Application.Services.Analyzer;

public class AnalyzerOptions
{
    public const string SectionName = "Analyzer";

    public string CurrentVersion { get; set; } = "v0.1";

    public int MaxRemainingAnswersInResult { get; set; } = 50;
}
