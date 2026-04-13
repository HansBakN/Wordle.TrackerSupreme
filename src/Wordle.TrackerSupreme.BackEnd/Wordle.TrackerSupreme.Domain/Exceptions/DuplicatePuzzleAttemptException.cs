namespace Wordle.TrackerSupreme.Domain.Exceptions;

public class DuplicatePuzzleAttemptException : InvalidOperationException
{
    public DuplicatePuzzleAttemptException()
        : base("You already have an attempt for today's puzzle. Refresh to continue.")
    {
    }
}
