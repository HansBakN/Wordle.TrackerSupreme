namespace Wordle.TrackerSupreme.Domain.Services.Game;

public interface IGameplayService
{
    Task<GameplayState> GetState(Guid playerId, CancellationToken cancellationToken = default);
    Task<GameplayState> SubmitGuess(Guid playerId, string guessWord, CancellationToken cancellationToken = default);
    Task<SolutionsSnapshot> GetSolutions(CancellationToken cancellationToken = default);
}
