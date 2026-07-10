using System.Reflection;
using System.Text.Json;
using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Application.Services.Import;

public class NytPuzzleCatalogue
{
    private readonly IReadOnlyList<NytPuzzleMetadata> _entries;
    private readonly IReadOnlyDictionary<int, NytPuzzleMetadata> _byId;

    public NytPuzzleCatalogue() : this(LoadEmbeddedCatalogue()) { }

    public NytPuzzleCatalogue(IEnumerable<NytPuzzleMetadata> entries)
    {
        _entries = entries.OrderBy(entry => entry.PrintDate).ToList();
        _byId = _entries.ToDictionary(entry => entry.NytPuzzleId);
    }

    public IReadOnlyList<NytPuzzleMetadata> Published => _entries;
    public bool TryGet(int nytPuzzleId, out NytPuzzleMetadata metadata) => _byId.TryGetValue(nytPuzzleId, out metadata!);

    private static IReadOnlyList<NytPuzzleMetadata> LoadEmbeddedCatalogue()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames().Single(name => name.EndsWith("nyt-puzzle-catalogue.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The embedded NYT puzzle catalogue is unavailable.");
        return JsonSerializer.Deserialize<List<NytPuzzleMetadata>>(stream)
            ?? throw new InvalidOperationException("The embedded NYT puzzle catalogue is invalid.");
    }
}
