using Microsoft.AspNetCore.Http;

namespace Wordle.TrackerSupreme.Api.Auth;

public static class AuthCookie
{
    public const string CookieName = "wts_auth";

    public static CookieOptions BuildOptions(bool secure)
        => new()
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = secure,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(1)
        };
}
