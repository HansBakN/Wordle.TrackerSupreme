using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Application.Services.Game;

public class WordSelector : IWordSelector, IAnswerPoolProvider
{
    private static readonly string[] Pool =
    [
        "SLATE", "CRANE", "BRAVE", "TRAIN", "SHINE", "GLASS", "FROND", "QUIET", "PLANT", "ROAST",
        "TRAIL", "SNAKE", "CLOUD", "BRINK", "DRIVE", "STEAM", "WATER", "GRAPE", "PANEL", "CROWN",
        "STARE", "GHOST", "PLUSH", "MONEY", "LIGHT", "RANGE", "BRICK", "FLAME", "WOUND", "SCORE",
        "CHIME", "PRIDE", "STONE", "HOUSE", "PIVOT", "CHALK", "FROST", "BLINK", "SHARD", "TOWEL",
        "NORTH", "SOUTH", "EAGER", "QUEST", "FRAME", "GRIND", "WRIST", "TRICK", "VOICE", "YEARN"
    ];

    private static readonly IReadOnlyList<string> SortedAnswers =
        Pool.OrderBy(word => word, StringComparer.Ordinal).ToArray();

    // Keep the rotation deterministic so every node agrees on the solution without extra storage.
    private static readonly DateOnly Anchor = new(2025, 1, 1);

    public IReadOnlyList<string> Answers => SortedAnswers;

    public string GetSolutionFor(DateOnly puzzleDate)
    {
        var offset = puzzleDate.DayNumber - Anchor.DayNumber;
        var index = ((offset % Pool.Length) + Pool.Length) % Pool.Length;
        return Pool[index];
    }

    public string SelectRandomWord()
    {
        var index = Random.Shared.Next(Pool.Length);
        return Pool[index];
    }
}
