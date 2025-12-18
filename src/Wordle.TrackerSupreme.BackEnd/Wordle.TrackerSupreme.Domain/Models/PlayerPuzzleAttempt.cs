namespace Wordle.TrackerSupreme.Domain.Models;

public class PlayerPuzzleAttempt
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public Guid DailyPuzzleId { get; set; }
    public DailyPuzzle DailyPuzzle { get; set; } = null!;
    public AttemptStatus Status { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedOn { get; set; }
    public bool PlayedInHardMode { get; set; }
    public ICollection<GuessAttempt> Guesses { get; set; } = [];

    public int? GuessCount => Guesses?.Count;
}
