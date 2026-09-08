using Microsoft.Extensions.DependencyInjection;
using TicTacToe.API.Configuration;

namespace TicTacToe.API.Services.Strategies;

public class MinimaxMoveStrategy(RuleBasedMoveStrategy? fallbackStrategy = null) : IMoveStrategy
{
    private readonly RuleBasedMoveStrategy _fallbackStrategy = fallbackStrategy ?? new RuleBasedMoveStrategy();

    public string StrategyName => "Minimax";

    public (int Row, int Col) SelectMove(string?[][] board, GameSettings settings, IWinDetectionService winDetection)
    {
        var size = settings.BoardSize;
        var comp = settings.ComputerPlayer;
        var human = comp == settings.PlayerX ? settings.PlayerO : settings.PlayerX;

        // If board is larger than 3x3, minimax tree is huge, fallback to rule-based for safety
        if (size > 3)
        {
            return _fallbackStrategy.SelectMove(board, settings, winDetection);
        }

        var bestScore = int.MinValue;
        var bestMove = (-1, -1);

        for (var r = 0; r < size; r++)
        {
            for (var c = 0; c < size; c++)
            {
                if (board[r][c] == null)
                {
                    board[r][c] = comp;
                    var score = Minimax(board, 0, false, comp, human, winDetection, size);
                    board[r][c] = null;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = (r, c);
                    }
                }
            }
        }

        if (bestMove.Item1 == -1)
            throw new InvalidOperationException("No available moves.");

        return bestMove;
    }

    private static int Minimax(
        string?[][] board,
        int depth,
        bool isMaximizing,
        string comp,
        string human,
        IWinDetectionService winDetection,
        int size)
    {
        winDetection.CheckWinner(board, out var winner);
        if (winner == comp) return 10 - depth;
        if (winner == human) return depth - 10;
        if (winDetection.IsBoardFull(board)) return 0;

        if (isMaximizing)
        {
            var maxEval = int.MinValue;
            for (var r = 0; r < size; r++)
            {
                for (var c = 0; c < size; c++)
                {
                    if (board[r][c] == null)
                    {
                        board[r][c] = comp;
                        var eval = Minimax(board, depth + 1, false, comp, human, winDetection, size);
                        board[r][c] = null;
                        maxEval = Math.Max(maxEval, eval);
                    }
                }
            }
            return maxEval;
        }
        else
        {
            var minEval = int.MaxValue;
            for (var r = 0; r < size; r++)
            {
                for (var c = 0; c < size; c++)
                {
                    if (board[r][c] == null)
                    {
                        board[r][c] = human;
                        var eval = Minimax(board, depth + 1, true, comp, human, winDetection, size);
                        board[r][c] = null;
                        minEval = Math.Min(minEval, eval);
                    }
                }
            }
            return minEval;
        }
    }
}
