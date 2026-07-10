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
using Wordle.TrackerSupreme.Api.Models.Import;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Services.Import;
using Wordle.TrackerSupreme.Infrastructure.Database;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class NytImportApiTests
{
    [Fact]
    public async Task Import_allows_malformed_record_to_be_reported_in_response_instead_of_rejecting_whole_payload()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        using var body = JsonContent.Create(new
        {
            schemaVersion = 1,
            importCode = "ABCD-EFGH",
            states = new object[]
            {
                new
                {
                    nytPuzzleId = 1205,
                    printDate = "2024-10-06",
                    status = "WIN",
                    hardMode = true,
                    isPlayingArchive = false,
                    boardState = new[] { "CIGAR" }
                },
                new
                {
                    nytPuzzleId = 1205,
                    printDate = "2024-10-06",
                    status = "WIN",
                    hardMode = true,
                    isPlayingArchive = false,
                    boardState = (string[]?)null
                }
            }
        });

        var response = await client.PostAsync("/api/import/nyt", body);
        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);
        factory.ImportService.LastRequest.Should().NotBeNull();
        factory.ImportService.LastRequest!.States.Should().HaveCount(2);
        factory.ImportService.LastRequest.States[1]!.BoardState.Should().BeNull();
        var payload = await response.Content.ReadFromJsonAsync<NytImportResponse>();
        payload!.Imported.Should().Be(1);
        payload.Rejected.Should().Be(1);
    }

    private sealed class Factory : WebApplicationFactory<NytImportController>
    {
        public FakeImportService ImportService { get; } = new();

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
                services.RemoveAll<INytImportService>();
                services.AddDbContext<WordleTrackerSupremeDbContext>(options => options.UseInMemoryDatabase($"nyt-api-{Guid.NewGuid()}"));
                services.AddSingleton<INytImportService>(ImportService);
            });
        }
    }

    private sealed class FakeImportService : INytImportService
    {
        public NytImportRequest? LastRequest { get; private set; }

        public Task<NytImportSessionCreated> CreateSession(Guid playerId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<NytImportSessionStatus?> GetSession(Guid playerId, Guid sessionId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<NytPuzzleMetadata>> GetPublishedCatalogue(string importCode, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<NytImportResult> Import(NytImportRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new NytImportResult(2, 1, 0, 0, 1, 0,
            [
                new NytImportRecordResult(1205, new DateOnly(2024, 10, 6), "imported"),
                new NytImportRecordResult(1205, new DateOnly(2024, 10, 6), "rejected", "A completed game must include a board state.")
            ]));
        }
    }
}
