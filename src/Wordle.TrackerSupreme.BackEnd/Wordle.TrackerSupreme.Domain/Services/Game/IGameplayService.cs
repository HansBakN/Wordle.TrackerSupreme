namespace Wordle.TrackerSupreme.Domain.Services.Game;

public interface IGameplayService
{
    Task<GameplayState> GetState(Guid playerId, DateOnly? puzzleDate = null, CancellationToken cancellationToken = default);
    Task<GameplayState> SubmitGuess(Guid playerId, string guessWord, DateOnly? puzzleDate = null, CancellationToken cancellationToken = default);
    Task<GameplayState> EnableEasyMode(Guid playerId, DateOnly? puzzleDate = null, CancellationToken cancellationToken = default);
    Task<SolutionsSnapshot> GetSolutions(CancellationToken cancellationToken = default);
}
