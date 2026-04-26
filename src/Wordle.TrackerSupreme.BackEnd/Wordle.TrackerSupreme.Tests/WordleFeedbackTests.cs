using FluentAssertions;
using Wordle.TrackerSupreme.Application.Services.Analyzer;
using Wordle.TrackerSupreme.Domain.Models;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class WordleFeedbackTests
{
    [Theory]
    [InlineData("CRANE", "CRANE", "CCCCC")]
    [InlineData("CRANE", "PLANT", "AACCA")]
    [InlineData("CRANE", "AROSE", "PCAAC")]
    [InlineData("CRANE", "MOMMY", "AAAAA")]
    public void Compute_ReturnsExpectedFeedbackForBasicCases(string answer, string guess, string expected)
    {
        var feedback = WordleFeedback.Compute(answer, guess);

        Encode(feedback).Should().Be(expected);
    }

    [Theory]
    [InlineData("ALLOY", "LLAMA", "PCPAA")]
    [InlineData("ABBEY", "BABES", "PPCCA")]
    [InlineData("EERIE", "GEESE", "ACPAC")]
    [InlineData("ROBOT", "OOOOO", "ACACA")]
    [InlineData("BOOST", "GOOEY", "ACCAA")]
    public void Compute_HandlesDuplicateLettersExactly(string answer, string guess, string expected)
    {
        var feedback = WordleFeedback.Compute(answer, guess);

        Encode(feedback).Should().Be(expected);
    }

    [Fact]
    public void Compute_IsCaseSensitive_ExpectsUpperCase()
    {
        var feedback = WordleFeedback.Compute("CRANE", "crane");

        feedback.Should().AllSatisfy(result => result.Should().Be(LetterResult.Absent));
    }

    [Fact]
    public void Compute_ThrowsWhenLengthsDiffer()
    {
        var act = () => WordleFeedback.Compute("CRANE", "CR");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compute_ThrowsWhenAnswerNull()
    {
        var act = () => WordleFeedback.Compute(null!, "CRANE");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Pack_IsDeterministicAndUniquePerPattern()
    {
        var packA = WordleFeedback.Pack(WordleFeedback.Compute("CRANE", "PLANT"));
        var packB = WordleFeedback.Pack(WordleFeedback.Compute("CRANE", "PLANT"));
        var packC = WordleFeedback.Pack(WordleFeedback.Compute("CRANE", "AROSE"));

        packA.Should().Be(packB);
        packA.Should().NotBe(packC);
    }

    [Fact]
    public void Equal_IsTrueOnlyForIdenticalSequences()
    {
        var first = WordleFeedback.Compute("CRANE", "CRANE");
        var second = WordleFeedback.Compute("CRANE", "CRANE");
        var different = WordleFeedback.Compute("CRANE", "PLANT");

        WordleFeedback.Equal(first, second).Should().BeTrue();
        WordleFeedback.Equal(first, different).Should().BeFalse();
    }

    private static string Encode(IReadOnlyList<LetterResult> feedback) =>
        new(feedback.Select(r => r switch
        {
            LetterResult.Correct => 'C',
            LetterResult.Present => 'P',
            LetterResult.Absent => 'A',
            _ => '?',
        }).ToArray());
}
