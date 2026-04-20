namespace Wordle.TrackerSupreme.Domain.Services.Game;

public interface IPracticeService
{
    Task<PracticeState> StartNewGame(Guid playerId, CancellationToken cancellationToken = default);
    Task<PracticeState?> GetActiveGame(Guid playerId, CancellationToken cancellationToken = default);
    Task<PracticeState> SubmitGuess(Guid playerId, string guessWord, CancellationToken cancellationToken = default);
}
