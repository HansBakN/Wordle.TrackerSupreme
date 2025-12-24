using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Api.Models.Game;

public record AttemptResponse(
    Guid AttemptId,
    AttemptStatus Status,
    bool IsAfterReveal,
    DateTime CreatedOn,
    DateTime? CompletedOn,
    IReadOnlyCollection<GuessResponse> Guesses);
