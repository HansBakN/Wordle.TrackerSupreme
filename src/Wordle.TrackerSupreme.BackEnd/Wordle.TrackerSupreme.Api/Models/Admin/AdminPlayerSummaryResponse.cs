namespace Wordle.TrackerSupreme.Api.Models.Admin;

public record AdminPlayerSummaryResponse(
    Guid Id,
    string DisplayName,
    string Email,
    DateTime CreatedOn,
    bool IsAdmin,
    int AttemptCount);
