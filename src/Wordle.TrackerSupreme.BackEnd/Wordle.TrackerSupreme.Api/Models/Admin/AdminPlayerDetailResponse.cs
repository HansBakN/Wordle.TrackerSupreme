using Wordle.TrackerSupreme.Api.Models.Game;
using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Api.Models.Admin;

public record AdminPlayerDetailResponse(
    Guid Id,
    string DisplayName,
    string Email,
    DateTime CreatedOn,
    bool IsAdmin,
    IReadOnlyList<AdminPlayerAttemptResponse> Attempts);

public record AdminPlayerAttemptResponse(
    Guid AttemptId,
    DateOnly PuzzleDate,
    AttemptStatus Status,
    bool PlayedInHardMode,
    DateTime CreatedOn,
    DateTime? CompletedOn,
    IReadOnlyList<GuessResponse> Guesses);
