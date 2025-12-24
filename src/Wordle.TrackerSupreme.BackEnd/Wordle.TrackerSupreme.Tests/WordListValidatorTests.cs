using FluentAssertions;
using Wordle.TrackerSupreme.Application.Services.Game;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class WordListValidatorTests
{
    [Fact]
    public void IsValid_returns_true_for_known_word()
    {
        var validator = new WordListValidator();

        validator.IsValid("CRANE").Should().BeTrue();
    }

    [Fact]
    public void IsValid_returns_false_for_unknown_word()
    {
        var validator = new WordListValidator();

        validator.IsValid("ZZZZZ").Should().BeFalse();
    }
}
