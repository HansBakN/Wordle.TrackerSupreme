namespace Wordle.TrackerSupreme.Domain.Models;

public class Player
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public ICollection<PlayerPuzzleAttempt> Attempts { get; set; } = [];
}
