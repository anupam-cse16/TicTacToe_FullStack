using TicTacToe.API.Models;
using TicTacToe.API.Services;
using FluentAssertions;

namespace TicTacToe.Tests;

public class WinDetectionTests
{
    private readonly WinDetectionService _sut = new();

    private static string?[][] EmptyBoard() =>
        [new string?[3], new string?[3], new string?[3]];

    [Fact]
    public void CheckWinner_RowWin_ReturnsWinner()
    {
        var board = EmptyBoard();
        board[0][0] = board[0][1] = board[0][2] = "X";

        var cells = _sut.CheckWinner(board, out var winner);

        winner.Should().Be("X");
        cells.Should().HaveCount(3);
        cells.Should().ContainEquivalentOf(new WinningCell { Row = 0, Col = 0 });
    }

    [Fact]
    public void CheckWinner_ColumnWin_ReturnsWinner()
    {
        var board = EmptyBoard();
        board[0][1] = board[1][1] = board[2][1] = "O";

        _sut.CheckWinner(board, out var winner);

        winner.Should().Be("O");
    }

    [Fact]
    public void CheckWinner_DiagonalWin_ReturnsWinner()
    {
        var board = EmptyBoard();
        board[0][0] = board[1][1] = board[2][2] = "X";

        var cells = _sut.CheckWinner(board, out var winner);

        winner.Should().Be("X");
        cells.Should().HaveCount(3);
    }

    [Fact]
    public void CheckWinner_AntiDiagonalWin_ReturnsWinner()
    {
        var board = EmptyBoard();
        board[0][2] = board[1][1] = board[2][0] = "O";

        _sut.CheckWinner(board, out var winner);

        winner.Should().Be("O");
    }

    [Fact]
    public void CheckWinner_NoWinner_ReturnsNull()
    {
        var board = EmptyBoard();
        board[0][0] = "X"; board[0][1] = "O";

        var cells = _sut.CheckWinner(board, out var winner);

        winner.Should().BeNull();
        cells.Should().BeEmpty();
    }

    [Fact]
    public void IsBoardFull_FullBoard_ReturnsTrue()
    {
        var board = new string?[][]
        {
            ["X", "O", "X"],
            ["X", "X", "O"],
            ["O", "X", "O"]
        };

        _sut.IsBoardFull(board).Should().BeTrue();
    }

    [Fact]
    public void IsBoardFull_EmptyBoard_ReturnsFalse()
    {
        _sut.IsBoardFull(EmptyBoard()).Should().BeFalse();
    }

    [Fact]
    public void FindWinningMove_XCanWin_ReturnsCell()
    {
        var board = EmptyBoard();
        board[0][0] = board[0][1] = "X";

        var move = _sut.FindWinningMove(board, "X");

        move.Should().Be((0, 2));
    }

    [Fact]
    public void FindWinningMove_NoWinningMove_ReturnsNull()
    {
        var board = EmptyBoard();
        board[0][0] = "X";

        var move = _sut.FindWinningMove(board, "X");

        move.Should().BeNull();
    }
}
