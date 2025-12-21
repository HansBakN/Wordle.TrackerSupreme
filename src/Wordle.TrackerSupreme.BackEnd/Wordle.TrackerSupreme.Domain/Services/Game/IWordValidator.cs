namespace Wordle.TrackerSupreme.Domain.Services.Game;

public interface IWordValidator
{
    bool IsValid(string word);
}
