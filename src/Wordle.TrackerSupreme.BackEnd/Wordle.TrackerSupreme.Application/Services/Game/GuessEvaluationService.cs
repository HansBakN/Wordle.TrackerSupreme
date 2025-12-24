using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Application.Services.Game;

public class GuessEvaluationService(GameOptions options, IWordValidator wordValidator) : IGuessEvaluationService
{
    private readonly GameOptions _options = options;
    private readonly IWordValidator _wordValidator = wordValidator;

    public string NormalizeGuess(string guessWord)
    {
        if (string.IsNullOrWhiteSpace(guessWord))
        {
            throw new ArgumentException("Guess cannot be empty.", nameof(guessWord));
        }

        var cleaned = guessWord.Trim().ToUpperInvariant();
        if (cleaned.Length != _options.WordLength)
        {
            throw new ArgumentException($"Guess must be {_options.WordLength} letters long.", nameof(guessWord));
        }

        if (!cleaned.All(char.IsLetter))
        {
            throw new ArgumentException("Guess must contain only letters.", nameof(guessWord));
        }

        if (!_wordValidator.IsValid(cleaned))
        {
            throw new ArgumentException("Guess must be a valid English word.", nameof(guessWord));
        }

        return cleaned;
    }

    public IReadOnlyList<LetterEvaluation> EvaluateGuess(string solution, string guess)
    {
        solution = solution.ToUpperInvariant();
        guess = guess.ToUpperInvariant();

        var feedback = new LetterEvaluation[_options.WordLength];
        var solutionChars = solution.ToCharArray();
        var guessChars = guess.ToCharArray();

        for (int i = 0; i < _options.WordLength; i++)
        {
            if (guessChars[i] == solutionChars[i])
            {
                feedback[i] = new LetterEvaluation
                {
                    Id = Guid.NewGuid(),
                    Position = i,
                    Letter = guessChars[i],
                    Result = LetterResult.Correct
                };
                solutionChars[i] = '*';
            }
        }

        for (int i = 0; i < _options.WordLength; i++)
        {
            if (feedback[i] is not null)
            {
                continue;
            }

            var letter = guessChars[i];
            var index = Array.IndexOf(solutionChars, letter);
            var result = index >= 0 ? LetterResult.Present : LetterResult.Absent;

            feedback[i] = new LetterEvaluation
            {
                Id = Guid.NewGuid(),
                Position = i,
                Letter = letter,
                Result = result
            };

            if (index >= 0)
            {
                solutionChars[index] = '*';
            }
        }

        return feedback.ToList();
    }
}
