using FluentAssertions;
using Wordle.TrackerSupreme.Application.Services.Analyzer;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Exceptions;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Models.Analyzer;
using Wordle.TrackerSupreme.Domain.Services.Analyzer;
using Wordle.TrackerSupreme.Domain.Services.Game;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class AnalyzerServiceTests
{
    private const string Version = "v0.1";

    private static readonly string[] Lexicon =
    [
        "CRANE", "PLANT", "AROSE", "BLEAT", "BLINK", "BLAST", "BLAME", "BRAVE",
        "BRAIN", "DRAIN", "GRAIN", "TRAIN", "SLATE", "STARE", "STORE", "STORY",
        "MOMMY", "ROBOT", "OOZED", "AABBA",
    ];

    private static IAnalyzerService BuildService(AnalyzerOptions? options = null, GameOptions? gameOptions = null)
    {
        var validator = new InMemoryWordList(Lexicon);
        return new AnalyzerService(
            options ?? new AnalyzerOptions { CurrentVersion = Version, MaxRemainingAnswersInResult = 50 },
            gameOptions ?? new GameOptions(),
            validator,
            validator);
    }

    private static AnalyzerInput Input(IEnumerable<string> guesses, string answer, AnalyzerMode mode = AnalyzerMode.Normal, string version = Version) => new()
    {
        Guesses = guesses.ToList(),
        Answer = answer,
        Mode = mode,
        AnalyzerVersion = version,
    };

    [Fact]
    public void Analyze_SolvedInOneStep_ReturnsCorrectFeedbackAndSinglePossibleAnswer()
    {
        var service = BuildService();

        var result = service.Analyze(Input(["CRANE"], "CRANE"));

        result.PlayerSteps.Should().Be(1);
        result.Solved.Should().BeTrue();
        result.AnalyzerVersion.Should().Be(Version);
        result.Answer.Should().Be("CRANE");
        result.Turns.Should().HaveCount(1);
        result.Turns[0].Feedback.Should().AllSatisfy(r => r.Should().Be(LetterResult.Correct));
        result.Turns[0].PossibleAnswersRemainingCount.Should().Be(1);
        result.Turns[0].PossibleAnswersRemaining.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new RemainingAnswer("CRANE", 1d));
    }

    [Fact]
    public void Analyze_NarrowsCandidateSetMonotonically()
    {
        var service = BuildService();

        var result = service.Analyze(Input(["CRANE", "BRAIN", "TRAIN"], "TRAIN"));

        var counts = result.Turns.Select(t => t.PossibleAnswersRemainingCount).ToList();
        counts.Should().BeInDescendingOrder();
        counts[^1].Should().Be(1);
        result.Turns[^1].PossibleAnswersRemaining.Single().Word.Should().Be("TRAIN");
    }

    [Fact]
    public void Analyze_PosteriorProbabilitiesAreUniformOverRemainingCandidates()
    {
        var service = BuildService();

        var result = service.Analyze(Input(["CRANE", "PLANT", "AROSE", "BLEAT", "BLINK", "BLAST"], "BRAIN"));

        foreach (var turn in result.Turns)
        {
            if (turn.PossibleAnswersRemainingCount == 0)
            {
                turn.PossibleAnswersRemaining.Should().BeEmpty();
                continue;
            }

            var expectedProbability = 1d / turn.PossibleAnswersRemainingCount;
            turn.PossibleAnswersRemaining.Should()
                .AllSatisfy(remaining => remaining.Probability.Should().BeApproximately(expectedProbability, 1e-9));

            if (turn.PossibleAnswersRemainingCount <= turn.PossibleAnswersRemaining.Count)
            {
                turn.PossibleAnswersRemaining.Sum(r => r.Probability).Should().BeApproximately(1d, 1e-9);
            }
        }
    }

    [Fact]
    public void Analyze_TruncatesRemainingAnswerListAtConfiguredLimit()
    {
        var service = BuildService(new AnalyzerOptions { CurrentVersion = Version, MaxRemainingAnswersInResult = 3 });

        var result = service.Analyze(Input(["CRANE", "PLANT", "AROSE", "BLEAT", "BLINK", "BLAST"], "BLAST"));

        foreach (var turn in result.Turns)
        {
            turn.PossibleAnswersRemaining.Count.Should().BeLessThanOrEqualTo(3);
        }
    }

    [Fact]
    public void Analyze_FailedGameWithSixGuesses_IsAccepted()
    {
        var service = BuildService();

        var result = service.Analyze(Input(["CRANE", "PLANT", "AROSE", "BLEAT", "BLINK", "BLAST"], "TRAIN"));

        result.Solved.Should().BeFalse();
        result.PlayerSteps.Should().Be(6);
        result.Turns.Should().HaveCount(6);
    }

    [Fact]
    public void Analyze_IsDeterministic()
    {
        var service = BuildService();
        var input = Input(["CRANE", "BRAIN", "TRAIN"], "TRAIN");

        var first = service.Analyze(input);
        var second = service.Analyze(input);

        first.Should().BeEquivalentTo(second);
    }

    [Theory]
    [InlineData("v0.2")]
    [InlineData("")]
    public void Analyze_RejectsUnsupportedVersion(string requested)
    {
        var service = BuildService();

        var act = () => service.Analyze(Input(["CRANE"], "CRANE", version: requested));

        act.Should().Throw<AnalyzerInputException>()
            .WithMessage("*analyzer version*");
    }

    [Fact]
    public void Analyze_RejectsZeroGuesses()
    {
        var service = BuildService();

        var act = () => service.Analyze(Input([], "CRANE"));

        act.Should().Throw<AnalyzerInputException>()
            .WithMessage("*at least one guess*");
    }

    [Fact]
    public void Analyze_RejectsMoreThanSixGuesses()
    {
        var service = BuildService();

        var act = () => service.Analyze(Input(
            ["CRANE", "PLANT", "AROSE", "BLEAT", "BLINK", "BLAST", "BRAVE"],
            "BRAVE"));

        act.Should().Throw<AnalyzerInputException>()
            .WithMessage("*at most 6 guesses*");
    }

    [Fact]
    public void Analyze_RejectsInvalidWord()
    {
        var service = BuildService();

        var act = () => service.Analyze(Input(["ZZZZZ"], "CRANE"));

        act.Should().Throw<AnalyzerInputException>()
            .WithMessage("*not in the valid word list*");
    }

    [Fact]
    public void Analyze_RejectsWrongLength()
    {
        var service = BuildService();

        var act = () => service.Analyze(Input(["CRANES"], "CRANE"));

        act.Should().Throw<AnalyzerInputException>()
            .WithMessage("*5 letters long*");
    }

    [Fact]
    public void Analyze_RejectsNonLetterCharacters()
    {
        var service = BuildService();

        var act = () => service.Analyze(Input(["CR4NE"], "CRANE"));

        act.Should().Throw<AnalyzerInputException>()
            .WithMessage("*only A-Z*");
    }

    [Fact]
    public void Analyze_RejectsAnswerNotInWordList()
    {
        var service = BuildService();

        var act = () => service.Analyze(Input(["CRANE"], "ZZZZZ"));

        act.Should().Throw<AnalyzerInputException>()
            .WithMessage("*answer*not in the valid word list*");
    }

    [Fact]
    public void Analyze_RejectsIncompleteUnsolvedGame()
    {
        var service = BuildService();

        var act = () => service.Analyze(Input(["CRANE", "PLANT"], "TRAIN"));

        act.Should().Throw<AnalyzerInputException>()
            .WithMessage("*incomplete*");
    }

    [Fact]
    public void Analyze_RejectsExtraGuessesAfterSolve()
    {
        var service = BuildService();

        var act = () => service.Analyze(Input(["CRANE", "CRANE"], "CRANE"));

        act.Should().Throw<AnalyzerInputException>()
            .WithMessage("*before the final guess*");
    }

    [Fact]
    public void Analyze_NormalizesLowercaseInput()
    {
        var service = BuildService();

        var result = service.Analyze(Input(["crane"], "crane"));

        result.Answer.Should().Be("CRANE");
        result.Turns[0].Guess.Should().Be("CRANE");
    }

    private sealed class InMemoryWordList : IWordValidator, IWordListProvider
    {
        private readonly HashSet<string> _set;

        public InMemoryWordList(IEnumerable<string> words)
        {
            _set = words.Select(w => w.ToUpperInvariant()).ToHashSet(StringComparer.Ordinal);
            Words = _set.OrderBy(w => w, StringComparer.Ordinal).ToList();
        }

        public IReadOnlyList<string> Words { get; }

        public bool IsValid(string word) => _set.Contains(word.ToUpperInvariant());
    }
}
