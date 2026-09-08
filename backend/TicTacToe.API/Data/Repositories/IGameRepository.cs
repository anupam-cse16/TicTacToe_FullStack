using TicTacToe.API.Data.Entities;

namespace TicTacToe.API.Data.Repositories;

public interface IGameRepository
{
    GameSessionEntity? GetById(Guid id, bool asNoTracking = false);
    Task<GameSessionEntity?> GetByIdAsync(Guid id, bool asNoTracking = false, CancellationToken cancellationToken = default);
    void Add(GameSessionEntity entity);
    void Update(GameSessionEntity entity);
    void Remove(GameSessionEntity entity);
    void RemoveRange(IEnumerable<GameSessionEntity> entities);
    List<GameSessionEntity> GetStaleSessions(DateTime cutoff);
}
