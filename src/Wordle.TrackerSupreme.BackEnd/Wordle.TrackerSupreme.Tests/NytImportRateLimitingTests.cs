using System.Net;
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
using Wordle.TrackerSupreme.Infrastructure.Database;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class NytImportRateLimitingTests
{
    [Fact]
    public async Task Submission_endpoints_are_rate_limited_and_session_creation_requires_authentication()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        (await client.PostAsync("/api/import/nyt/session", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        for (var attempt = 0; attempt < 20; attempt++)
        {
            (await client.GetAsync("/api/import/nyt/catalogue?importCode=INVALID")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        (await client.GetAsync("/api/import/nyt/catalogue?importCode=INVALID")).StatusCode.Should().Be((HttpStatusCode)429);
    }

    private sealed class Factory : WebApplicationFactory<NytImportController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{JwtSettings.SectionName}:Secret"] = "12345678901234567890123456789012",
                [$"{JwtSettings.SectionName}:Issuer"] = "test-issuer",
                [$"{JwtSettings.SectionName}:Audience"] = "test-audience"
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<WordleTrackerSupremeDbContext>>();
                services.RemoveAll<DbContextOptions<WordleTrackerSupremeDbContext>>();
                services.RemoveAll<WordleTrackerSupremeDbContext>();
                services.AddDbContext<WordleTrackerSupremeDbContext>(options => options.UseInMemoryDatabase($"nyt-rate-{Guid.NewGuid()}"));
            });
        }
    }
}
