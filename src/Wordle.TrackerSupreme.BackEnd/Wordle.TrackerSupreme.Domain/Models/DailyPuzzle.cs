namespace Wordle.TrackerSupreme.Domain.Models;

public class DailyPuzzle
{
    public Guid Id { get; set; }
    public DateOnly PuzzleDate { get; set; }
    public PuzzleStream Stream { get; set; } = PuzzleStream.NewYorkTimes;
    public string? Solution { get; set; }
    public int? NytPuzzleId { get; set; }
    public int? PublicPuzzleNumber { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPractice { get; set; }
    public ICollection<PlayerPuzzleAttempt> Attempts { get; set; } = [];
}
