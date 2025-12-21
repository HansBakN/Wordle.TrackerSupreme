namespace Wordle.TrackerSupreme.Api.Models.Game;

public record GameStateResponse(
    DateOnly PuzzleDate,
    bool CutoffPassed,
    bool SolutionRevealed,
    bool AllowLatePlay,
    int WordLength,
    int MaxGuesses,
    bool IsHardMode,
    bool CanGuess,
    AttemptResponse? Attempt,
    string? Solution);
