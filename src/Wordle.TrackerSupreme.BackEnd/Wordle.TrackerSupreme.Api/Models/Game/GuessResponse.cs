namespace Wordle.TrackerSupreme.Api.Models.Game;

public record GuessResponse(
    Guid GuessId,
    int GuessNumber,
    string GuessWord,
    IReadOnlyCollection<LetterFeedbackResponse> Feedback);
