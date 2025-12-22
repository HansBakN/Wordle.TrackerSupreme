using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Wordle.TrackerSupreme.Application.Services.Game;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class OfficialWordProviderTests
{
    [Fact]
    public async Task Returns_uppercase_solution()
    {
        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.RequestUri?.AbsolutePath.Should().Contain("/svc/wordle/v2/2025-01-01.json");
            var payload = "{\"solution\":\"wander\"}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
        });

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://www.nytimes.com/")
        };

        var provider = new OfficialWordProvider(client);

        var solution = await provider.GetSolutionForDateAsync(new DateOnly(2025, 1, 1), CancellationToken.None);

        solution.Should().Be("WANDER");
    }

    [Fact]
    public async Task Throws_when_response_is_not_successful()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://www.nytimes.com/")
        };

        var provider = new OfficialWordProvider(client);

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetSolutionForDateAsync(new DateOnly(2025, 1, 2), CancellationToken.None));
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

        public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            _responder = responder ?? throw new ArgumentNullException(nameof(responder));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _responder(request, cancellationToken);
    }
}
