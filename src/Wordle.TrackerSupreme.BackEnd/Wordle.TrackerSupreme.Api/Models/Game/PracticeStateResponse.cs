namespace Wordle.TrackerSupreme.Api.Models.Game;

public record PracticeStateResponse(
    Guid PuzzleId,
    bool SolutionRevealed,
    int WordLength,
    int MaxGuesses,
    bool CanGuess,
    AttemptResponse? Attempt,
    string? Solution);
