namespace Wordle.TrackerSupreme.Api.Models.Admin;

public record AdminPuzzleResponse(
    Guid Id,
    DateOnly PuzzleDate,
    string Solution,
    int AttemptCount);
