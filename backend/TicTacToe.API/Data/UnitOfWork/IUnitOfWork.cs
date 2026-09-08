using TicTacToe.API.Data.Repositories;

namespace TicTacToe.API.Data.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IGameRepository Games { get; }
    IScoreboardRepository Scoreboard { get; }
    int SaveChanges();
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
