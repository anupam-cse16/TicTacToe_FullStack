using TicTacToe.API.Configuration;

namespace TicTacToe.API.Services.Strategies;

public interface IMoveStrategy
{
    string StrategyName { get; }
    (int Row, int Col) SelectMove(string?[][] board, GameSettings settings, IWinDetectionService winDetection);
}
