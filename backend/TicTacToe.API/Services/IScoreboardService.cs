using TicTacToe.API.Models;

namespace TicTacToe.API.Services;

public interface IScoreboardService
{
    Scoreboard GetScoreboard();
    void UpdateScoreboard(GameSession session);
    void ResetScoreboard();
    int PruneStaleGameIds(TimeSpan maxAge);
}
