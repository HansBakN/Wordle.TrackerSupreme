using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Api.Models.Game;

public record PuzzleHistoryEntryResponse(
    DateOnly PuzzleDate,
    string? Solution,
    AttemptStatus Status,
    bool PlayedInHardMode,
    bool IsAfterReveal,
    int GuessCount,
    IReadOnlyList<GuessResponse> Guesses);
