namespace Wordle.TrackerSupreme.Api.Models.Game;

public record SolutionsResponse(
    DateOnly PuzzleDate,
    string Solution,
    bool CutoffPassed,
    IReadOnlyCollection<SolutionEntryResponse> Entries);
