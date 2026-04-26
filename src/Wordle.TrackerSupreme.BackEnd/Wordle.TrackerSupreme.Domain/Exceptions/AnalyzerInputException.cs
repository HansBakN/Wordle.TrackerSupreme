namespace Wordle.TrackerSupreme.Domain.Exceptions;

public sealed class AnalyzerInputException : Exception
{
    public AnalyzerInputException(string message) : base(message)
    {
    }
}
