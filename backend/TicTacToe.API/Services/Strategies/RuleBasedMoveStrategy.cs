using TicTacToe.API.Configuration;

namespace TicTacToe.API.Services.Strategies;

public class RuleBasedMoveStrategy : IMoveStrategy
{
    public string StrategyName => "RuleBased";

    public (int Row, int Col) SelectMove(string?[][] board, GameSettings settings, IWinDetectionService winDetection)
    {
        var size = settings.BoardSize;
        var comp = settings.ComputerPlayer;
        var human = comp == settings.PlayerX ? settings.PlayerO : settings.PlayerX;

        // 1. Win if possible
        var winMove = winDetection.FindWinningMove(board, comp);
        if (winMove.HasValue) return winMove.Value;

        // 2. Block opponent
        var blockMove = winDetection.FindWinningMove(board, human);
        if (blockMove.HasValue) return blockMove.Value;

        // 3. Center (for odd-sized boards)
        if (size % 2 != 0)
        {
            var center = size / 2;
            if (board[center][center] == null) return (center, center);
        }

        // 4. Corner (zero-allocation direct checks)
        var max = size - 1;
        if (board[0][0] == null) return (0, 0);
        if (board[0][max] == null) return (0, max);
        if (board[max][0] == null) return (max, 0);
        if (board[max][max] == null) return (max, max);

        // 5. Any available cell
        for (var r = 0; r < size; r++)
        {
            for (var c = 0; c < size; c++)
            {
                if (board[r][c] == null) return (r, c);
            }
        }

        throw new InvalidOperationException("No available moves.");
    }
}
