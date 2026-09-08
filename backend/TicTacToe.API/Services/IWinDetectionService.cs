using TicTacToe.API.Models;

namespace TicTacToe.API.Services;

public interface IWinDetectionService
{
    int BoardSize { get; }
    List<WinningCell> CheckWinner(string?[][] board, out string? winner);
    bool IsBoardFull(string?[][] board);
    (int Row, int Col)? FindWinningMove(string?[][] board, string player);
}
