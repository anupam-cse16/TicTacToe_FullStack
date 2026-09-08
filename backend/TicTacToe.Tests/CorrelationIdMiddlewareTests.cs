using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TicTacToe.API.Middleware;
using FluentAssertions;

namespace TicTacToe.Tests;

public class CorrelationIdMiddlewareTests
{
    private class TestScopeLogger : ILogger<CorrelationIdMiddleware>
    {
        public List<object?> CapturedScopes { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            CapturedScopes.Add(state);
            return new NoopDisposable();
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }

        private class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    [Fact]
    public async Task ExistingCorrelationId_PassesThroughToResponseAndTraceIdentifier()
    {
        // Arrange
        var testLogger = new TestScopeLogger();
        var nextExecuted = false;
        RequestDelegate next = ctx =>
        {
            nextExecuted = true;
            return Task.CompletedTask;
        };

        var middleware = new CorrelationIdMiddleware(next, testLogger);
        var context = new DefaultHttpContext();
        var expectedId = "custom-trace-id-12345";
        context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeader] = expectedId;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextExecuted.Should().BeTrue();
        context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeader].ToString().Should().Be(expectedId);
        context.TraceIdentifier.Should().Be(expectedId);
        testLogger.CapturedScopes.Should().ContainSingle();
        var scope = testLogger.CapturedScopes[0] as IEnumerable<KeyValuePair<string, object>>;
        scope.Should().NotBeNull();
        scope!.Should().Contain(kvp => kvp.Key == "CorrelationId" && kvp.Value.ToString() == expectedId);
    }

    [Fact]
    public async Task MissingCorrelationId_GeneratesNewGuid()
    {
        // Arrange
        var testLogger = new TestScopeLogger();
        var nextExecuted = false;
        RequestDelegate next = ctx =>
        {
            nextExecuted = true;
            return Task.CompletedTask;
        };

        var middleware = new CorrelationIdMiddleware(next, testLogger);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextExecuted.Should().BeTrue();
        var responseId = context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeader].ToString();
        responseId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(responseId, out _).Should().BeTrue("a new valid GUID should be generated");
        context.TraceIdentifier.Should().Be(responseId);
        testLogger.CapturedScopes.Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WhitespaceCorrelationId_GeneratesNewGuid(string whitespaceHeader)
    {
        // Arrange
        var testLogger = new TestScopeLogger();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, testLogger);
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeader] = whitespaceHeader;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseId = context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeader].ToString();
        responseId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(responseId, out _).Should().BeTrue();
    }
}
