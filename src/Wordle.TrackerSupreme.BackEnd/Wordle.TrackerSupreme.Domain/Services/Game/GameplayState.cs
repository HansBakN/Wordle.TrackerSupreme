using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Domain.Services.Game;

public record GameplayState(
    DailyPuzzle Puzzle,
    PlayerPuzzleAttempt? Attempt,
    bool CutoffPassed,
    bool SolutionRevealed,
    bool AllowLatePlay,
    int WordLength,
    int MaxGuesses);

public record SolutionsSnapshot(
    DailyPuzzle Puzzle,
    bool CutoffPassed,
    IReadOnlyCollection<PlayerPuzzleAttempt> Attempts);
