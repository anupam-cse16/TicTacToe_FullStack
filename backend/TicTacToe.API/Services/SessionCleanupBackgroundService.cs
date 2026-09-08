using Microsoft.Extensions.Options;
using TicTacToe.API.Configuration;

namespace TicTacToe.API.Services;

public class SessionCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<GameSettings> options,
    ILogger<SessionCleanupBackgroundService> logger) : BackgroundService
{
    private readonly GameSettings _settings = options?.Value ?? new GameSettings();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var checkInterval = TimeSpan.FromMinutes(_settings.CleanupIntervalMinutes > 0 ? _settings.CleanupIntervalMinutes : 30);
        var maxSessionAge = TimeSpan.FromHours(_settings.SessionTtlHours > 0 ? _settings.SessionTtlHours : 2);

        using var timer = new PeriodicTimer(checkInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
                var scoreboardService = scope.ServiceProvider.GetRequiredService<IScoreboardService>();

                var removed = gameService.CleanupStaleSessions(maxSessionAge);
                var pruned = scoreboardService.PruneStaleGameIds(maxSessionAge);
                if (removed > 0 || pruned > 0)
                {
                    logger.LogInformation("Evicted {SessionCount} stale game sessions and {PrunedCount} tracked game IDs.", removed, pruned);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during stale session cleanup.");
            }
        }
    }
}
