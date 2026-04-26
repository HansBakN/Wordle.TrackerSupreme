using Wordle.TrackerSupreme.Domain.Models.Analyzer;

namespace Wordle.TrackerSupreme.Domain.Services.Analyzer;

public interface IAnalyzerService
{
    AnalyzerResult Analyze(AnalyzerInput input);
}
