using TicTacToe.API.Data.Entities;

namespace TicTacToe.API.Data.Repositories;

public interface IScoreboardRepository
{
    ScoreboardEntity GetOrCreate(bool asNoTracking = false);
    void Reset();
}
