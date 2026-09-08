using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.API.Data;
using TicTacToe.API.Data.Repositories;
using TicTacToe.API.Data.UnitOfWork;
using TicTacToe.API.Services;
using FluentAssertions;

namespace TicTacToe.Tests;

public class SessionCleanupTests
{
    [Fact]
    public void CleanupStaleSessions_EvictsSessionsOlderThanMaxAge()
    {
        var (uow, connection, db) = TestDbContextFactory.CreateUnitOfWork();
        using var conn = connection;
        using var context = db;
        using var unitOfWork = uow;

        var mapper = TestDbContextFactory.CreateMapper();
        var winDetection = new WinDetectionService();
        var computerMove = new ComputerMoveService(winDetection);
        var scoreboard = new ScoreboardService(uow, mapper);
        var gameService = new GameService(uow, mapper, winDetection, computerMove, scoreboard);

        var staleSession = gameService.CreateGame("TwoPlayer");
        // Manually simulate last active 3 hours ago in database
        var staleEntity = db.GameSessions.Find(staleSession.Id)!;
        staleEntity.LastActiveUtc = DateTime.UtcNow.AddHours(-3);
        db.SaveChanges();

        var activeSession = gameService.CreateGame("TwoPlayer");

        var evicted = gameService.CleanupStaleSessions(TimeSpan.FromHours(2));

        evicted.Should().Be(1);

        var actStale = () => gameService.GetGame(staleSession.Id);
        actStale.Should().Throw<KeyNotFoundException>();

        var active = gameService.GetGame(activeSession.Id);
        active.Should().NotBeNull();
    }

    [Fact]
    public async Task SessionCleanupBackgroundService_RunsAndHandlesCancellationCleanly()
    {
        var (db, connection) = TestDbContextFactory.CreateInMemoryContext();
        using var conn = connection;
        using var context = db;

        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton<IWinDetectionService, WinDetectionService>();
        services.AddSingleton<IComputerMoveService, ComputerMoveService>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IScoreboardRepository, ScoreboardRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton(TestDbContextFactory.CreateMapper());
        services.AddScoped<IScoreboardService, ScoreboardService>();
        services.AddScoped<IGameService, GameService>();

        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SessionCleanupBackgroundService>.Instance;
        var options = Microsoft.Extensions.Options.Options.Create(new TicTacToe.API.Configuration.GameSettings
        {
            CleanupIntervalMinutes = 1,
            SessionTtlHours = 1
        });

        var service = new SessionCleanupBackgroundService(scopeFactory, options, logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Immediately cancel so it exits without hanging

        var act = async () => await service.StartAsync(cts.Token);
        await act.Should().NotThrowAsync();
    }
}
