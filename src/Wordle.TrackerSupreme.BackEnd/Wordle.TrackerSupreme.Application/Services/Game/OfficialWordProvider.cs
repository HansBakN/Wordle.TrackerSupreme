using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Application.Services.Game;

public class OfficialWordProvider : IOfficialWordProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public OfficialWordProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetSolutionForDateAsync(DateOnly puzzleDate, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"svc/wordle/v2/{puzzleDate:yyyy-MM-dd}.json", cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<WordleResponse>(contentStream, JsonOptions, cancellationToken);
        if (payload?.Solution is null)
        {
            throw new InvalidOperationException("Received an unexpected Wordle response without a solution.");
        }

        return payload.Solution.ToUpperInvariant();
    }

    private sealed record WordleResponse(string? Solution);
}
