using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Domain.Services.Game;

public record PracticeState(
    DailyPuzzle Puzzle,
    PlayerPuzzleAttempt Attempt,
    bool SolutionRevealed,
    int WordLength,
    int MaxGuesses);
