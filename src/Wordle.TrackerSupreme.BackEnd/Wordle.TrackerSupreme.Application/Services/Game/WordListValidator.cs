using System.Reflection;
using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Application.Services.Game;

public class WordListValidator : IWordValidator, IWordListProvider
{
    private static readonly Lazy<WordListData> WordData = new(LoadWords);

    public IReadOnlyList<string> Words => WordData.Value.Words;

    public bool IsValid(string word) => WordData.Value.WordSet.Contains(word);

    private static WordListData LoadWords()
    {
        var assembly = Assembly.GetExecutingAssembly();
        // Word list sourced from the NYT Wordle allowed guesses set.
        const string resourceName = "Wordle.TrackerSupreme.Application.Services.Game.wordlist-nyt.txt";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Word list resource not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        var words = new List<string>();
        var wordSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cleaned = line.Trim().ToUpperInvariant();
            if (wordSet.Add(cleaned))
            {
                words.Add(cleaned);
            }
        }

        return new WordListData(words, wordSet);
    }

    private sealed record WordListData(IReadOnlyList<string> Words, HashSet<string> WordSet);
}
