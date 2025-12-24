using System;

namespace Wordle.TrackerSupreme.Application.Services.Game;

public class DailyPuzzleUnavailableException : Exception
{
    public DailyPuzzleUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
