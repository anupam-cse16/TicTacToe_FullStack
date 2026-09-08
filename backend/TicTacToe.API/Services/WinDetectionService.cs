using Microsoft.Extensions.Options;
using TicTacToe.API.Configuration;
using TicTacToe.API.Models;

namespace TicTacToe.API.Services;

public class WinDetectionService : IWinDetectionService
{
    private readonly int _size;
    private readonly int[][][] _winPatterns;

    public int BoardSize => _size;

    public WinDetectionService(IOptions<GameSettings>? options = null)
    {
        _size = options?.Value.BoardSize is > 0 ? options.Value.BoardSize : 3;
        _winPatterns = GenerateWinPatterns(_size);
    }

    private static int[][][] GenerateWinPatterns(int size)
    {
        var patterns = new List<int[][]>();

        for (var i = 0; i < size; i++)
        {
            // Row i
            patterns.Add(Enumerable.Range(0, size).Select(j => new[] { i, j }).ToArray());
            // Column i
            patterns.Add(Enumerable.Range(0, size).Select(j => new[] { j, i }).ToArray());
        }

        // Main diagonal (top-left -> bottom-right)
        patterns.Add(Enumerable.Range(0, size).Select(i => new[] { i, i }).ToArray());

        // Anti-diagonal (top-right -> bottom-left)
        patterns.Add(Enumerable.Range(0, size).Select(i => new[] { i, size - 1 - i }).ToArray());

        return [.. patterns];
    }

    /// <summary>
    /// Checks for a winner without allocating temporary arrays on the hot-path.
    /// Returns winning cells if found, else empty list.
    /// </summary>
    public List<WinningCell> CheckWinner(string?[][] board, out string? winner)
    {
        foreach (var pattern in _winPatterns)
        {
            var first = board[pattern[0][0]][pattern[0][1]];
            if (first == null) continue;

            var allMatch = true;
            for (var i = 1; i < _size; i++)
            {
                if (board[pattern[i][0]][pattern[i][1]] != first)
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
            {
                winner = first;
                var winning = new List<WinningCell>(_size);
                for (var i = 0; i < _size; i++)
                {
                    winning.Add(new WinningCell { Row = pattern[i][0], Col = pattern[i][1] });
                }
                return winning;
            }
        }

        winner = null;
        return [];
    }

    /// <summary>
    /// Returns true if all cells are filled using fast direct index scan without LINQ overhead.
    /// </summary>
    public bool IsBoardFull(string?[][] board)
    {
        for (var r = 0; r < _size; r++)
        {
            for (var c = 0; c < _size; c++)
            {
                if (board[r][c] == null) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Checks if a specific player can win with one move without allocating tuples or arrays.
    /// Returns the winning cell or null.
    /// </summary>
    public (int Row, int Col)? FindWinningMove(string?[][] board, string player)
    {
        foreach (var pattern in _winPatterns)
        {
            var playerCount = 0;
            var emptyCount = 0;
            var emptyRow = -1;
            var emptyCol = -1;

            for (var i = 0; i < _size; i++)
            {
                var r = pattern[i][0];
                var c = pattern[i][1];
                var val = board[r][c];

                if (val == player)
                {
                    playerCount++;
                }
                else if (val == null)
                {
                    emptyCount++;
                    emptyRow = r;
                    emptyCol = c;
                }
            }

            if (playerCount == _size - 1 && emptyCount == 1)
            {
                return (emptyRow, emptyCol);
            }
        }

        return null;
    }
}
