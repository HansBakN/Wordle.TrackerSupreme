namespace Wordle.TrackerSupreme.Application.Services.Import;

public class NytImportOptions
{
    public const string SectionName = "NytImport";
    public int SessionLifetimeMinutes { get; set; } = 10;
    public int MaxStates { get; set; } = 2000;
}
