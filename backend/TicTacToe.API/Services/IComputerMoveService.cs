namespace TicTacToe.API.Services;

public interface IComputerMoveService
{
    (int Row, int Col) SelectMove(string?[][] board);
}
