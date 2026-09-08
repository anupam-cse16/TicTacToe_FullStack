using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace TicTacToe.API.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            KeyNotFoundException knf => (HttpStatusCode.NotFound, knf.Message),
            InvalidOperationException ioe => (HttpStatusCode.BadRequest, ioe.Message),
            ArgumentOutOfRangeException aor => (HttpStatusCode.BadRequest, aor.Message),
            ArgumentException ae => (HttpStatusCode.BadRequest, ae.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected server error occurred.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "Unhandled server error: {Message}", exception.Message);
        }
        else
        {
            logger.LogWarning("Request rejected ({StatusCode}): {Message}", statusCode, message);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = statusCode.ToString(),
            Detail = message,
            Instance = context.Request.Path
        };
        problemDetails.Extensions["error"] = message;

        return context.Response.WriteAsJsonAsync(problemDetails);
    }
}
