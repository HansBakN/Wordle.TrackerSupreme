namespace Wordle.TrackerSupreme.Api.Services.Game;

public class WordSelector
{
    private static readonly string[] Words =
    [
        "SLATE", "CRANE", "BRAVE", "TRAIN", "SHINE", "GLASS", "FROND", "QUIET", "PLANT", "ROAST",
        "TRAIL", "SNAKE", "CLOUD", "BRINK", "DRIVE", "STEAM", "WATER", "GRAPE", "PANEL", "CROWN",
        "STARE", "GHOST", "PLUSH", "MONEY", "LIGHT", "RANGE", "BRICK", "FLAME", "WOUND", "SCORE",
        "CHIME", "PRIDE", "STONE", "HOUSE", "PIVOT", "CHALK", "FROST", "BLINK", "SHARD", "TOWEL",
        "NORTH", "SOUTH", "EAGER", "QUEST", "FRAME", "GRIND", "WRIST", "TRICK", "VOICE", "YEARN"
    ];

    // Keep the rotation deterministic so every node agrees on the solution without extra storage.
    private static readonly DateOnly Anchor = new(2025, 1, 1);

    public string GetSolutionFor(DateOnly puzzleDate)
    {
        var offset = puzzleDate.DayNumber - Anchor.DayNumber;
        var index = ((offset % Words.Length) + Words.Length) % Words.Length;
        return Words[index];
    }
}
