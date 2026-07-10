namespace Wordle.TrackerSupreme.Domain.Models;

public class NytImportSession
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public required string CodeHash { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? AggregateGamesPlayed { get; set; }
    public int Requested { get; set; }
    public int Imported { get; set; }
    public int Duplicates { get; set; }
    public int Conflicts { get; set; }
    public int Rejected { get; set; }
    public int MissingFromNyt { get; set; }
    public ICollection<PlayerPuzzleAttempt> ImportedAttempts { get; set; } = [];
}
