using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Application.Services.Analyzer;

/// <summary>
/// Deterministic Wordle tile feedback computation that mirrors how the gameplay
/// service evaluates real player guesses. Implementing this once and sharing it
/// guarantees the analyzer can never disagree with the live game on what a guess
/// reveals.
/// </summary>
public static class WordleFeedback
{
    /// <summary>
    /// Computes the per-position feedback for a guess against an answer. Both
    /// strings are expected to be the same length and already upper-cased.
    /// Duplicate letters are handled exactly like NYT Wordle: each answer letter
    /// can only be matched once, with positional (Correct) matches taking
    /// precedence over relocated (Present) matches.
    /// </summary>
    public static LetterResult[] Compute(string answer, string guess)
    {
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(guess);

        if (answer.Length != guess.Length)
        {
            throw new ArgumentException("Guess and answer must have the same length.", nameof(guess));
        }

        var length = answer.Length;
        var result = new LetterResult[length];
        Span<bool> consumed = length <= 16 ? stackalloc bool[length] : new bool[length];

        for (var i = 0; i < length; i++)
        {
            if (answer[i] == guess[i])
            {
                result[i] = LetterResult.Correct;
                consumed[i] = true;
            }
        }

        for (var i = 0; i < length; i++)
        {
            if (result[i] == LetterResult.Correct)
            {
                continue;
            }

            var letter = guess[i];
            var matched = false;
            for (var j = 0; j < length; j++)
            {
                if (consumed[j] || answer[j] != letter)
                {
                    continue;
                }

                consumed[j] = true;
                matched = true;
                break;
            }

            result[i] = matched ? LetterResult.Present : LetterResult.Absent;
        }

        return result;
    }

    /// <summary>
    /// Packs a feedback array into a deterministic integer key (base-3 over
    /// {Absent, Present, Correct}). Used by future analyzer slices to bucket
    /// guesses by feedback pattern; included here so the public surface is
    /// stable from PR-1.
    /// </summary>
    public static int Pack(IReadOnlyList<LetterResult> feedback)
    {
        ArgumentNullException.ThrowIfNull(feedback);

        var key = 0;
        for (var i = 0; i < feedback.Count; i++)
        {
            key = (key * 3) + (int)feedback[i];
        }

        return key;
    }

    public static bool Equal(IReadOnlyList<LetterResult> left, IReadOnlyList<LetterResult> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }
}
