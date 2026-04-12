using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wordle.TrackerSupreme.Api.Auth;
using Wordle.TrackerSupreme.Api.Controllers;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Infrastructure.Database;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class AuthRateLimitingTests
{
    private const string AdminEmail = "admin@wordle.supreme";
    private const string AdminCredential = "dev-password";
    private const string InvalidCredential = "wrong-password";
    private const string ValidCredential = "Supreme!234";

    [Fact]
    public async Task SignIn_returns_too_many_requests_after_limit_is_exceeded()
    {
        await using var factory = new AuthRateLimitingFactory();
        using var client = factory.CreateClient();
        var payload = new { email = AdminEmail, password = InvalidCredential };

        for (var attempt = 0; attempt < AuthRateLimiting.PermitLimit; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/signin", payload);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var limited = await client.PostAsJsonAsync("/api/auth/signin", payload);

        limited.StatusCode.Should().Be((HttpStatusCode)429);
        var body = await limited.Content.ReadFromJsonAsync<MessageResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be(AuthRateLimiting.TooManyAttemptsMessage);
    }

    [Fact]
    public async Task SignUp_returns_too_many_requests_after_limit_is_exceeded()
    {
        await using var factory = new AuthRateLimitingFactory();
        using var client = factory.CreateClient();

        for (var attempt = 0; attempt < AuthRateLimiting.PermitLimit; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/signup", new
            {
                displayName = $"RateLimitUser{attempt}",
                email = $"ratelimit{attempt}@example.com",
                password = ValidCredential
            });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var limited = await client.PostAsJsonAsync("/api/auth/signup", new
        {
            displayName = "RateLimitUserFinal",
            email = "ratelimit-final@example.com",
            password = ValidCredential
        });

        limited.StatusCode.Should().Be((HttpStatusCode)429);
        var body = await limited.Content.ReadFromJsonAsync<MessageResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be(AuthRateLimiting.TooManyAttemptsMessage);
    }

    private sealed class AuthRateLimitingFactory : WebApplicationFactory<AuthController>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
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
                    options.UseInMemoryDatabase($"auth-rate-limit-{Guid.NewGuid()}"));

                using var scope = services.BuildServiceProvider().CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<WordleTrackerSupremeDbContext>();
                dbContext.Database.EnsureCreated();

                var admin = new Player
                {
                    Id = Guid.NewGuid(),
                    DisplayName = "AdminSupreme",
                    Email = AdminEmail,
                    PasswordHash = string.Empty,
                    IsAdmin = true
                };

                var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<Player>>();
                admin.PasswordHash = passwordHasher.HashPassword(admin, AdminCredential);
                dbContext.Players.Add(admin);
                dbContext.SaveChanges();
            });
        }
    }

    private sealed record MessageResponse(string Message);
}
