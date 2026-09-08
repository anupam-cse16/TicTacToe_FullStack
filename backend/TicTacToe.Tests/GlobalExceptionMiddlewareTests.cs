using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.API.Middleware;
using FluentAssertions;

namespace TicTacToe.Tests;

public class GlobalExceptionMiddlewareTests
{
    private static async Task<(int StatusCode, string ResponseBody)> InvokeMiddlewareAsync(RequestDelegate next)
    {
        var middleware = new GlobalExceptionMiddleware(next, NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task KeyNotFoundException_Returns404NotFound()
    {
        var (status, body) = await InvokeMiddlewareAsync(_ => throw new KeyNotFoundException("Game 123 not found"));

        status.Should().Be((int)HttpStatusCode.NotFound);
        body.Should().Contain("Game 123 not found");
    }

    [Fact]
    public async Task InvalidOperationException_Returns400BadRequest()
    {
        var (status, body) = await InvokeMiddlewareAsync(_ => throw new InvalidOperationException("Cell is already occupied"));

        status.Should().Be((int)HttpStatusCode.BadRequest);
        body.Should().Contain("Cell is already occupied");
    }

    [Fact]
    public async Task ArgumentOutOfRangeException_Returns400BadRequest()
    {
        var (status, body) = await InvokeMiddlewareAsync(_ => throw new ArgumentOutOfRangeException("row", "Out of bounds"));

        status.Should().Be((int)HttpStatusCode.BadRequest);
        body.Should().Contain("Out of bounds");
    }

    [Fact]
    public async Task GenericException_Returns500InternalServerError()
    {
        var (status, body) = await InvokeMiddlewareAsync(_ => throw new Exception("Fatal crash"));

        status.Should().Be((int)HttpStatusCode.InternalServerError);
        body.Should().Contain("An unexpected server error occurred.");
    }

    [Fact]
    public async Task SuccessfulRequest_PassesThrough()
    {
        var (status, _) = await InvokeMiddlewareAsync(ctx =>
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            return Task.CompletedTask;
        });

        status.Should().Be((int)HttpStatusCode.OK);
    }
}
