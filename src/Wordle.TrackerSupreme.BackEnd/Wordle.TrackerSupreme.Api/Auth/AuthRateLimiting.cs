namespace Wordle.TrackerSupreme.Api.Auth;

public static class AuthRateLimiting
{
    public const string PolicyName = "auth";
    public const string TooManyAttemptsMessage = "Too many authentication attempts. Please try again in a minute.";
    public const int PermitLimit = 25;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
}
