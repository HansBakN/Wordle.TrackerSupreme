namespace Wordle.TrackerSupreme.Seeder;

public class SeederOptions
{
    public const string SectionName = "Seeder";

    public int RandomSeed { get; set; } = 12345;
    public int PlayerCount { get; set; } = 20;
    public int MinSolvedPuzzles { get; set; } = 30;
    public int MaxSolvedPuzzles { get; set; } = 200;
    public int FailedPuzzlesMin { get; set; } = 2;
    public int FailedPuzzlesMax { get; set; } = 12;
    public int InProgressPuzzlesMin { get; set; } = 1;
    public int InProgressPuzzlesMax { get; set; } = 4;
    public int PuzzleDays { get; set; } = 280;
    public string DefaultPassword { get; set; } = "dev-password";
    public bool AllowReseed { get; set; }
    public string? AnchorDate { get; set; }
}
