using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Wordle.TrackerSupreme.Api.Auth;
using Wordle.TrackerSupreme.Api.Controllers;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Infrastructure.Database;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class JwtAuthenticationTests
{
    private const string AdminEmail = "admin@wordle.supreme";
    private const string AdminCredential = "dev-password";
    private const string JwtSecret = "12345678901234567890123456789012";
    private const string JwtIssuer = "test-issuer";
    private const string JwtAudience = "test-audience";

    [Fact]
    public async Task GetCurrentPlayer_rejects_expired_tokens()
    {
        await using var factory = new AuthFactory(expiryMinutes: -10);
        using var client = factory.CreateClient();
        var token = CreateExpiredToken(factory.SeededAdmin);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed class AuthFactory(int expiryMinutes) : WebApplicationFactory<AuthController>
    {
        public Player SeededAdmin { get; private set; } = null!;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{JwtSettings.SectionName}:Secret"] = JwtSecret,
                    [$"{JwtSettings.SectionName}:Issuer"] = JwtIssuer,
                    [$"{JwtSettings.SectionName}:Audience"] = JwtAudience,
                    [$"{JwtSettings.SectionName}:ExpiryMinutes"] = expiryMinutes.ToString()
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<WordleTrackerSupremeDbContext>>();
                services.RemoveAll<DbContextOptions<WordleTrackerSupremeDbContext>>();
                services.RemoveAll<WordleTrackerSupremeDbContext>();

                services.AddDbContext<WordleTrackerSupremeDbContext>(options =>
                    options.UseInMemoryDatabase($"jwt-auth-{Guid.NewGuid()}"));

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
                SeededAdmin = admin;
                dbContext.Players.Add(admin);
                dbContext.SaveChanges();
            });
        }
    }

    private static string CreateExpiredToken(Player player)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, player.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, player.DisplayName),
                new Claim("playerId", player.Id.ToString()),
                new Claim("isAdmin", player.IsAdmin ? "true" : "false")
            },
            notBefore: now.AddHours(-2),
            expires: now.AddHours(-1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
