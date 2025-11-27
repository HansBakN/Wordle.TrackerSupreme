namespace Wordle.TrackerSupreme.Domain.Models;

public class LetterEvaluation
{
    public Guid Id { get; set; }
    public Guid GuessAttemptId { get; set; }
    public GuessAttempt GuessAttempt { get; set; } = null!;
    public char Letter { get; set; }
    public int Position { get; set; }
    public LetterResult Result { get; set; }
}
