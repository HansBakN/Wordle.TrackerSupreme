namespace Wordle.TrackerSupreme.Domain.Models;

public class GuessAttempt
{
    public Guid Id { get; set; }
    public Guid PlayerPuzzleAttemptId { get; set; }
    public PlayerPuzzleAttempt PlayerPuzzleAttempt { get; set; } = null!;
    public int GuessNumber { get; set; }
    public required string GuessWord { get; set; }
    public ICollection<LetterEvaluation> Feedback { get; set; } = [];
}
