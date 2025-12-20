using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Api.Models.Game;

public record SolutionEntryResponse(
    Guid PlayerId,
    string DisplayName,
    AttemptStatus Status,
    int? GuessCount,
    bool IsAfterReveal,
    DateTime? CompletedOn,
    IReadOnlyCollection<string> Guesses);
