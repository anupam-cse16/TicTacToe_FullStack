using TicTacToe.API.Models;
using TicTacToe.API.Models.Requests;

namespace TicTacToe.API.Services;

public interface IGameService
{
    GameSession CreateGame(string mode);
    GameSession GetGame(Guid id);
    GameSession MakeMove(Guid id, MakeMoveRequest request);
    GameSession Undo(Guid id);
    GameSession Reset(Guid id);
    int CleanupStaleSessions(TimeSpan maxAge);
}
