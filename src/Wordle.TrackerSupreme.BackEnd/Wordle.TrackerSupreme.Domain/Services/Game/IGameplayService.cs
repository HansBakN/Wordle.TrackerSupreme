namespace Wordle.TrackerSupreme.Domain.Services.Game;

using Wordle.TrackerSupreme.Domain.Models;

public interface IGameplayService
{
    Task<GameplayState> GetState(
        Guid playerId,
        PuzzleStream stream = PuzzleStream.TrackerSupreme,
        CancellationToken cancellationToken = default);

    Task<GameplayState> SubmitGuess(
        Guid playerId,
        string guessWord,
        PuzzleStream stream = PuzzleStream.TrackerSupreme,
        CancellationToken cancellationToken = default);

    Task<GameplayState> EnableEasyMode(
        Guid playerId,
        PuzzleStream stream = PuzzleStream.TrackerSupreme,
        CancellationToken cancellationToken = default);

    Task<SolutionsSnapshot> GetSolutions(
        PuzzleStream stream = PuzzleStream.TrackerSupreme,
        CancellationToken cancellationToken = default);
}
