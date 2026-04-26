using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Exceptions;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Models.Analyzer;
using Wordle.TrackerSupreme.Domain.Services.Analyzer;
using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Application.Services.Analyzer;

/// <summary>
/// Foundation slice of <c>WordleBotSupreme</c>. Validates a completed game,
/// recomputes deterministic feedback for every player guess and surfaces the
/// posterior plausible-answer set after each turn under uniform priors.
///
/// Recommendation, skill, luck, analyzer solve path and explanations are
/// intentionally absent from this version — they arrive in subsequent slices
/// and will be guarded behind a higher <c>analyzerVersion</c>.
///
/// Plausible answers are drawn from <see cref="IAnswerPoolProvider"/> (the
/// analyzer's answer-domain lexicon), not from the full guess validator.
/// Issue #117 requires the returned remaining-answer probabilities to sum to 1
/// — when truncation kicks in, the visible items are renormalized so the
/// returned list is itself a valid probability distribution. The authoritative
/// total is always exposed via <c>PossibleAnswersRemainingCount</c>.
/// </summary>
public class AnalyzerService : IAnalyzerService
{
    private readonly AnalyzerOptions _options;
    private readonly GameOptions _gameOptions;
    private readonly IWordValidator _wordValidator;
    private readonly IAnswerPoolProvider _answerPoolProvider;

    public AnalyzerService(
        AnalyzerOptions options,
        GameOptions gameOptions,
        IWordValidator wordValidator,
        IAnswerPoolProvider answerPoolProvider)
    {
        _options = options;
        _gameOptions = gameOptions;
        _wordValidator = wordValidator;
        _answerPoolProvider = answerPoolProvider;
    }

    public AnalyzerResult Analyze(AnalyzerInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var (guesses, answer) = ValidateAndNormalize(input);
        var solved = string.Equals(guesses[^1], answer, StringComparison.Ordinal);

        var candidates = _answerPoolProvider.Answers
            .Select(word => word.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(word => word, StringComparer.Ordinal)
            .ToList();

        if (!candidates.Contains(answer, StringComparer.Ordinal))
        {
            throw new AnalyzerInputException(
                $"answer '{answer}' is not in the analyzer answer pool.");
        }

        var turns = new List<AnalyzerTurn>(guesses.Count);

        foreach (var guess in guesses)
        {
            var feedback = WordleFeedback.Compute(answer, guess);
            candidates = candidates
                .Where(candidate => WordleFeedback.Equal(WordleFeedback.Compute(candidate, guess), feedback))
                .ToList();

            var totalCount = candidates.Count;
            var truePosterior = totalCount == 0 ? 0d : 1d / totalCount;

            var ordered = candidates
                .OrderByDescending(_ => truePosterior)
                .ThenBy(word => word, StringComparer.Ordinal)
                .ToList();

            var truncated = totalCount > _options.MaxRemainingAnswersInResult;
            var visible = truncated
                ? ordered.Take(_options.MaxRemainingAnswersInResult).ToList()
                : ordered;

            // When the visible list is the full plausible set, the per-item
            // probability is the true posterior (1 / totalCount). When the
            // list is truncated we renormalize across the visible subset so
            // the returned list itself sums to 1 — see class doc comment.
            var visibleProbability = visible.Count == 0
                ? 0d
                : truncated
                    ? 1d / visible.Count
                    : truePosterior;

            var listed = visible
                .Select(word => new RemainingAnswer(word, visibleProbability))
                .ToList();

            turns.Add(new AnalyzerTurn
            {
                Guess = guess,
                Feedback = feedback,
                PossibleAnswersRemainingCount = totalCount,
                PossibleAnswersRemaining = listed,
            });
        }

        return new AnalyzerResult
        {
            AnalyzerVersion = _options.CurrentVersion,
            Mode = input.Mode,
            Answer = answer,
            PlayerSteps = guesses.Count,
            Solved = solved,
            Turns = turns,
        };
    }

    private (List<string> Guesses, string Answer) ValidateAndNormalize(AnalyzerInput input)
    {
        if (!string.Equals(input.AnalyzerVersion, _options.CurrentVersion, StringComparison.Ordinal))
        {
            throw new AnalyzerInputException(
                $"Unsupported analyzer version '{input.AnalyzerVersion}'. Expected '{_options.CurrentVersion}'.");
        }

        if (!Enum.IsDefined(input.Mode))
        {
            throw new AnalyzerInputException($"Unsupported analyzer mode '{input.Mode}'.");
        }

        if (input.Guesses is null || input.Guesses.Count == 0)
        {
            throw new AnalyzerInputException("At least one guess is required.");
        }

        if (input.Guesses.Count > _gameOptions.MaxGuesses)
        {
            throw new AnalyzerInputException(
                $"At most {_gameOptions.MaxGuesses} guesses are allowed.");
        }

        var answer = NormalizeWord(input.Answer, "answer");

        var normalizedGuesses = new List<string>(input.Guesses.Count);
        for (var i = 0; i < input.Guesses.Count; i++)
        {
            var guess = NormalizeWord(input.Guesses[i], $"guess #{i + 1}");
            normalizedGuesses.Add(guess);
        }

        var solveIndex = normalizedGuesses.FindIndex(g => string.Equals(g, answer, StringComparison.Ordinal));
        if (solveIndex >= 0 && solveIndex != normalizedGuesses.Count - 1)
        {
            throw new AnalyzerInputException(
                "Answer was already solved before the final guess; trim post-solve guesses before analysis.");
        }

        if (solveIndex < 0 && normalizedGuesses.Count < _gameOptions.MaxGuesses)
        {
            throw new AnalyzerInputException(
                "Game is incomplete: the answer was not solved and the maximum number of guesses was not used.");
        }

        return (normalizedGuesses, answer);
    }

    private string NormalizeWord(string word, string fieldDescription)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            throw new AnalyzerInputException($"{fieldDescription} cannot be empty.");
        }

        var cleaned = word.Trim().ToUpperInvariant();
        if (cleaned.Length != _gameOptions.WordLength)
        {
            throw new AnalyzerInputException(
                $"{fieldDescription} must be {_gameOptions.WordLength} letters long.");
        }

        for (var i = 0; i < cleaned.Length; i++)
        {
            if (cleaned[i] < 'A' || cleaned[i] > 'Z')
            {
                throw new AnalyzerInputException($"{fieldDescription} must contain only A-Z letters.");
            }
        }

        if (!_wordValidator.IsValid(cleaned))
        {
            throw new AnalyzerInputException(
                $"{fieldDescription} '{cleaned}' is not in the valid word list.");
        }

        return cleaned;
    }
}
