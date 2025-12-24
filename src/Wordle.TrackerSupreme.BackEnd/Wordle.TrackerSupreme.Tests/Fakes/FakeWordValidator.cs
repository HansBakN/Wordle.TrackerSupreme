using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Tests.Fakes;

public class FakeWordValidator : IWordValidator
{
    private readonly HashSet<string> _words;

    public FakeWordValidator(IEnumerable<string>? words = null)
    {
        _words = words is null
            ? []
            : new HashSet<string>(words.Select(w => w.ToUpperInvariant()));
    }

    public bool IsValid(string word)
    {
        if (_words.Count == 0)
        {
            return true;
        }

        return _words.Contains(word.ToUpperInvariant());
    }

    public IReadOnlyList<string> Words => _words.ToList();
}
