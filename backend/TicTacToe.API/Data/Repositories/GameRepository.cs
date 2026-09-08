using Microsoft.EntityFrameworkCore;
using TicTacToe.API.Data.Entities;

namespace TicTacToe.API.Data.Repositories;

public class GameRepository(TicTacToeDbContext db) : IGameRepository
{
    public GameSessionEntity? GetById(Guid id, bool asNoTracking = false)
    {
        return asNoTracking
            ? db.GameSessions.AsNoTracking().FirstOrDefault(g => g.Id == id)
            : db.GameSessions.Find(id);
    }

    public async Task<GameSessionEntity?> GetByIdAsync(Guid id, bool asNoTracking = false, CancellationToken cancellationToken = default)
    {
        return asNoTracking
            ? await db.GameSessions.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            : await db.GameSessions.FindAsync([id], cancellationToken);
    }

    public void Add(GameSessionEntity entity) => db.GameSessions.Add(entity);

    public void Update(GameSessionEntity entity)
    {
        if (db.Entry(entity).State == EntityState.Detached)
        {
            db.GameSessions.Update(entity);
        }
    }

    public void Remove(GameSessionEntity entity) => db.GameSessions.Remove(entity);

    public void RemoveRange(IEnumerable<GameSessionEntity> entities) => db.GameSessions.RemoveRange(entities);

    public List<GameSessionEntity> GetStaleSessions(DateTime cutoff)
    {
        // Leverage B-Tree index on LastActiveUtc
        return db.GameSessions.Where(s => s.LastActiveUtc < cutoff).ToList();
    }
}
