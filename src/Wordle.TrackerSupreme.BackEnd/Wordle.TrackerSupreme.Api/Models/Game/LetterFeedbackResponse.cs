using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Api.Models.Game;

public record LetterFeedbackResponse(int Position, char Letter, LetterResult Result);
