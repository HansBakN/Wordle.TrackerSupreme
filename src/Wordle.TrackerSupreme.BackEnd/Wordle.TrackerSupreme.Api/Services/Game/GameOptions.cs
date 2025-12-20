namespace Wordle.TrackerSupreme.Api.Services.Game;

public class GameOptions
{
    public const string SectionName = "Game";

    /// <summary>
    /// Hour of the day (server local time) when solutions unlock and late plays stop counting toward stats.
    /// </summary>
    public int RevealHourLocal { get; set; } = 12;

    public int MaxGuesses { get; set; } = 6;

    public int WordLength { get; set; } = 5;
}
