namespace Wordle.TrackerSupreme.Api.Models.Admin;

public record AdminUpdateAttemptRequest(IReadOnlyList<string> Guesses, bool PlayedInHardMode);
