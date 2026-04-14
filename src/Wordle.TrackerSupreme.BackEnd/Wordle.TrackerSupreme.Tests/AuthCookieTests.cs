using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wordle.TrackerSupreme.Api.Auth;
using Wordle.TrackerSupreme.Api.Controllers;
using Wordle.TrackerSupreme.Api.Models.Auth;
using Wordle.TrackerSupreme.Infrastructure.Database;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class AuthCookieTests
{
    private const string ValidCredential = "Supreme!234";

    [Fact]
    public async Task SignUp_sets_http_only_auth_cookie_and_does_not_return_a_browser_token()
    {
        await using var factory = new AuthCookieFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var nonce = Guid.NewGuid().ToString("N");

        var response = await client.PostAsJsonAsync("/api/auth/signup", new
        {
            displayName = $"CookieUser{nonce[..8]}",
            email = $"cookie.{nonce}@example.com",
            password = ValidCredential
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var setCookieValues).Should().BeTrue();
        setCookieValues.Should().NotBeNull();
        setCookieValues!.Should().Contain(value =>
            value.Contains($"{AuthCookie.CookieName}=")
            && value.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().BeNull();
    }

    [Fact]
    public async Task SignOut_returns_a_clearing_cookie_for_the_auth_session()
    {
        await using var factory = new AuthCookieFactory();
        using var client = factory.CreateClient();
        var nonce = Guid.NewGuid().ToString("N");

        var signUp = await client.PostAsJsonAsync("/api/auth/signup", new
        {
            displayName = $"CookieUser{nonce[..8]}",
            email = $"cookie.{nonce}@example.com",
            password = ValidCredential
        });
        signUp.EnsureSuccessStatusCode();
        var authCookie = signUp.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith($"{AuthCookie.CookieName}=", StringComparison.Ordinal));
        var cookieHeader = authCookie.Split(';', 2)[0];

        using var signOutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/signout");
        signOutRequest.Headers.Add("Cookie", cookieHeader);
        var signOut = await client.SendAsync(signOutRequest);
        signOut.StatusCode.Should().Be(HttpStatusCode.NoContent);
        signOut.Headers.TryGetValues("Set-Cookie", out var signOutCookies).Should().BeTrue();
        signOutCookies.Should().NotBeNull();
        signOutCookies!.Should().Contain(value =>
            value.Contains($"{AuthCookie.CookieName}=")
            && value.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class AuthCookieFactory : WebApplicationFactory<AuthController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{JwtSettings.SectionName}:Secret"] = "12345678901234567890123456789012",
                    [$"{JwtSettings.SectionName}:Issuer"] = "test-issuer",
                    [$"{JwtSettings.SectionName}:Audience"] = "test-audience"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<WordleTrackerSupremeDbContext>>();
                services.RemoveAll<DbContextOptions<WordleTrackerSupremeDbContext>>();
                services.RemoveAll<WordleTrackerSupremeDbContext>();

                services.AddDbContext<WordleTrackerSupremeDbContext>(options =>
                    options.UseInMemoryDatabase($"auth-cookie-{Guid.NewGuid()}"));

                using var scope = services.BuildServiceProvider().CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<WordleTrackerSupremeDbContext>();
                dbContext.Database.EnsureCreated();
            });
        }
    }
}
