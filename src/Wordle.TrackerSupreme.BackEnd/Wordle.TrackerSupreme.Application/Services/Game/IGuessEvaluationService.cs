using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Application.Services.Game;

public interface IGuessEvaluationService
{
    string NormalizeGuess(string guessWord);
    IReadOnlyList<LetterEvaluation> EvaluateGuess(string solution, string guess);
}
