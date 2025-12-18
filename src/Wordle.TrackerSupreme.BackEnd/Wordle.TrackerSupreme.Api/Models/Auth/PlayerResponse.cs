namespace Wordle.TrackerSupreme.Api.Models.Auth;

public record PlayerResponse(Guid Id, string DisplayName, string Email, DateTime CreatedOn);
