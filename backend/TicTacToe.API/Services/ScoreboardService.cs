using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TicTacToe.API.Configuration;
using TicTacToe.API.Data.UnitOfWork;
using TicTacToe.API.Models;

namespace TicTacToe.API.Services;

public class ScoreboardService(
    IUnitOfWork uow,
    IMapper mapper,
    ILogger<ScoreboardService>? logger = null,
    IOptions<GameSettings>? options = null) : IScoreboardService
{
    private readonly ILogger<ScoreboardService> _logger = logger ?? NullLogger<ScoreboardService>.Instance;
    private readonly GameSettings _settings = options?.Value ?? new GameSettings();
    private readonly Dictionary<Guid, DateTime> _processedGameIds = new();
    private readonly object _lock = new();

    public Scoreboard GetScoreboard()
    {
        lock (_lock)
        {
            // EF Core Optimization: AsNoTracking on read-only lookup
            var entity = uow.Scoreboard.GetOrCreate(asNoTracking: true);
            return mapper.Map<Scoreboard>(entity);
        }
    }

    public void UpdateScoreboard(GameSession session)
    {
        lock (_lock)
        {
            var entity = uow.Scoreboard.GetOrCreate(asNoTracking: false);
            var sessionEntity = uow.Games.GetById(session.Id, asNoTracking: false);

            if (sessionEntity != null && sessionEntity.IsScoreboardProcessed) return;
            if (_processedGameIds.ContainsKey(session.Id)) return;

            if (session.Status == GameStatus.Won)
            {
                if (session.Winner == _settings.PlayerX) entity.XWins++;
                else entity.OWins++;

                _processedGameIds[session.Id] = DateTime.UtcNow;
                if (sessionEntity != null) sessionEntity.IsScoreboardProcessed = true;
                uow.SaveChanges();

                _logger.LogInformation("Scoreboard updated for game {GameId}: Winner={Winner} (XWins={XWins}, OWins={OWins}, Draws={Draws})",
                    session.Id, session.Winner, entity.XWins, entity.OWins, entity.Draws);
            }
            else if (session.Status == GameStatus.Draw)
            {
                entity.Draws++;

                _processedGameIds[session.Id] = DateTime.UtcNow;
                if (sessionEntity != null) sessionEntity.IsScoreboardProcessed = true;
                uow.SaveChanges();

                _logger.LogInformation("Scoreboard updated for game {GameId}: Draw recorded (XWins={XWins}, OWins={OWins}, Draws={Draws})",
                    session.Id, entity.XWins, entity.OWins, entity.Draws);
            }
        }
    }

    public void ResetScoreboard()
    {
        lock (_lock)
        {
            uow.Scoreboard.Reset();
            _processedGameIds.Clear();
            uow.SaveChanges();
            _logger.LogInformation("Scoreboard reset to zero (XWins=0, OWins=0, Draws=0)");
        }
    }

    public int PruneStaleGameIds(TimeSpan maxAge)
    {
        lock (_lock)
        {
            var cutoff = DateTime.UtcNow - maxAge;
            var toRemove = new List<Guid>();
            foreach (var (id, timestamp) in _processedGameIds)
            {
                if (timestamp < cutoff)
                {
                    toRemove.Add(id);
                }
            }

            foreach (var id in toRemove)
            {
                _processedGameIds.Remove(id);
            }

            if (toRemove.Count > 0)
            {
                _logger.LogInformation("Pruned {Count} stale tracked game ID(s) older than {Cutoff}", toRemove.Count, cutoff);
            }

            return toRemove.Count;
        }
    }
}
