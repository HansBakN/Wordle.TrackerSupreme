using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Wordle.TrackerSupreme.Api.Middleware;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_sets_correlation_id_response_header_when_absent_from_request()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var logger = NullLogger<CorrelationIdMiddleware>.Instance;

        await middleware.InvokeAsync(context, logger);

        context.Response.Headers.ContainsKey("X-Correlation-ID").Should().BeTrue();
        var header = context.Response.Headers["X-Correlation-ID"].ToString();
        Guid.TryParse(header, out _).Should().BeTrue("header value should be a valid GUID");
    }

    [Fact]
    public async Task InvokeAsync_echoes_correlation_id_from_request_header()
    {
        var correlationId = "my-fixed-correlation-id";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = correlationId;
        context.Response.Body = new MemoryStream();

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var logger = NullLogger<CorrelationIdMiddleware>.Instance;

        await middleware.InvokeAsync(context, logger);

        context.Response.Headers["X-Correlation-ID"].ToString().Should().Be(correlationId);
    }
}
