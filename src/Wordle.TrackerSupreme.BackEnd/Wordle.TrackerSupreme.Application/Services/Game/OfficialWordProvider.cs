using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Wordle.TrackerSupreme.Domain.Services.Game;
using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Application.Services.Game;

public class OfficialWordProvider : IOfficialWordProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _httpClient;

    public OfficialWordProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetSolutionForDateAsync(DateOnly puzzleDate, CancellationToken cancellationToken)
        => (await GetMetadataForDateAsync(puzzleDate, cancellationToken)).Solution
            ?? throw new InvalidOperationException("Received an unexpected Wordle response without a solution.");

    public async Task<NytPuzzleMetadata> GetMetadataForDateAsync(DateOnly puzzleDate, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"svc/wordle/v2/{puzzleDate:yyyy-MM-dd}.json", cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<WordleResponse>(contentStream, JsonOptions, cancellationToken);
        if (payload?.Solution is null || payload.PrintDate is null)
        {
            throw new InvalidOperationException("Received an unexpected Wordle response without a solution.");
        }

        return new NytPuzzleMetadata(payload.Id, DateOnly.ParseExact(payload.PrintDate, "yyyy-MM-dd"),
            payload.DaysSinceLaunch, payload.Solution.ToUpperInvariant());
    }

    private sealed record WordleResponse(int Id, string? Solution, string? PrintDate, int? DaysSinceLaunch);
}
