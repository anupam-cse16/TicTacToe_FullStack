using TicTacToe.API.Data.Repositories;

namespace TicTacToe.API.Data.UnitOfWork;

public class UnitOfWork(
    TicTacToeDbContext db,
    IGameRepository gameRepository,
    IScoreboardRepository scoreboardRepository) : IUnitOfWork
{
    public IGameRepository Games => gameRepository;
    public IScoreboardRepository Scoreboard => scoreboardRepository;

    public int SaveChanges() => db.SaveChanges();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public void Dispose() => db.Dispose();
}
