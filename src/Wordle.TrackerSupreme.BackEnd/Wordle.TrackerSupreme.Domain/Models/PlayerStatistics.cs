namespace Wordle.TrackerSupreme.Domain.Models;

public class PlayerStatistics
{
    public int TotalAttempts { get; set; }
    public int Wins { get; set; }
    public int Failures { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public double? AverageGuessCount { get; set; }
}
