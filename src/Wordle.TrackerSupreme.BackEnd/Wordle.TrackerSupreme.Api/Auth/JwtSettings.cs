namespace Wordle.TrackerSupreme.Api.Auth;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string? Secret { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public int ExpiryMinutes { get; set; } = 60 * 24;
}
