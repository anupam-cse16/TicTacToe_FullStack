using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TicTacToe.API.Configuration;
using TicTacToe.API.Data;
using TicTacToe.API.Data.Repositories;
using TicTacToe.API.Data.UnitOfWork;
using TicTacToe.API.Mapping;
using TicTacToe.API.Middleware;
using TicTacToe.API.Services;
using TicTacToe.API.Services.Strategies;
using Microsoft.AspNetCore.HttpLogging;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration options
builder.Services.Configure<GameSettings>(builder.Configuration.GetSection(GameSettings.SectionName));
builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection(CorsSettings.SectionName));

// SQLite DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=tictactoe.db";
builder.Services.AddDbContext<TicTacToeDbContext>(options =>
    options.UseSqlite(connectionString));

// Repositories & Unit of Work
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IScoreboardRepository, ScoreboardRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// AutoMapper
builder.Services.AddSingleton<IMapper>(sp =>
{
    var loggerFactory = sp.GetService<ILoggerFactory>() ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
    var config = new MapperConfiguration(cfg =>
    {
        cfg.AddProfile<GameMappingProfile>();
    }, loggerFactory);
    return config.CreateMapper();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// HTTP Request logging
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestMethod |
                            HttpLoggingFields.RequestPath |
                            HttpLoggingFields.ResponseStatusCode |
                            HttpLoggingFields.Duration;
});

// AI Move Strategies
builder.Services.AddSingleton<IMoveStrategy, RuleBasedMoveStrategy>();
builder.Services.AddSingleton<IMoveStrategy, MinimaxMoveStrategy>();

// Game services
builder.Services.AddSingleton<IWinDetectionService, WinDetectionService>();
builder.Services.AddSingleton<IComputerMoveService, ComputerMoveService>();
builder.Services.AddScoped<IScoreboardService, ScoreboardService>();
builder.Services.AddScoped<IGameService, GameService>();

// Periodic session cleanup hosted service
builder.Services.AddHostedService<SessionCleanupBackgroundService>();

// CORS configured from appsettings.json
var corsSettings = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsSettings.AllowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(corsSettings.AllowedOrigins);
        }
        else
        {
            policy.AllowAnyOrigin();
        }

        policy.AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Ensure SQLite database, tables, and WAL mode are initialized
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TicTacToeDbContext>();
    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL;");
}

// Correlation ID middleware (first in pipeline so downstream logs include correlation scope)
app.UseMiddleware<CorrelationIdMiddleware>();

// HTTP request logging
app.UseHttpLogging();

// Centralized exception handling middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "TicTacToe API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
