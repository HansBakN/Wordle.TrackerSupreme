namespace Wordle.TrackerSupreme.Domain.Services.Game;

public interface IAnswerPoolProvider
{
    IReadOnlyList<string> Answers { get; }
}
