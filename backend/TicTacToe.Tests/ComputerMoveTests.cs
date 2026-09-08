using TicTacToe.API.Services;
using FluentAssertions;

namespace TicTacToe.Tests;

public class ComputerMoveTests
{
    private readonly WinDetectionService _winDetection = new();
    private ComputerMoveService CreateSut() => new(_winDetection);

    private static string?[][] EmptyBoard() =>
        [new string?[3], new string?[3], new string?[3]];

    [Fact]
    public void SelectMove_OCanWin_PlaysWinningMove()
    {
        var board = EmptyBoard();
        board[0][0] = board[0][1] = "O";

        var (row, col) = CreateSut().SelectMove(board);

        row.Should().Be(0);
        col.Should().Be(2);
    }

    [Fact]
    public void SelectMove_XWouldWin_BlocksX()
    {
        var board = EmptyBoard();
        board[1][0] = board[1][1] = "X";

        var (row, col) = CreateSut().SelectMove(board);

        row.Should().Be(1);
        col.Should().Be(2);
    }

    [Fact]
    public void SelectMove_CenterFree_TakesCenter()
    {
        var board = EmptyBoard();
        board[0][0] = "X";

        var (row, col) = CreateSut().SelectMove(board);

        row.Should().Be(1);
        col.Should().Be(1);
    }

    [Fact]
    public void SelectMove_CenterTaken_TakesCorner()
    {
        var board = EmptyBoard();
        board[1][1] = "X";

        var (row, col) = CreateSut().SelectMove(board);

        new[] { (0, 0), (0, 2), (2, 0), (2, 2) }.Should().Contain((row, col));
    }

    [Fact]
    public void SelectMove_AllCornersTaken_TakesAvailableCell()
    {
        var board = new string?[][]
        {
            ["X", null, "X"],
            [null, "X", null],
            ["X", null, "X"]
        };

        var (row, col) = CreateSut().SelectMove(board);

        board[row][col].Should().BeNull();
    }

    [Fact]
    public void SelectMove_WinOverBlock_PrefersWin()
    {
        // O can win at (2,2); X would win at (0,2)
        var board = new string?[][]
        {
            [null, null, "X"],
            [null, "X", null],
            ["O", "O", null]
        };

        var (row, col) = CreateSut().SelectMove(board);

        // Should pick O's win
        row.Should().Be(2);
        col.Should().Be(2);
    }

    [Fact]
    public void SelectMove_BoardFull_ThrowsInvalidOperationException()
    {
        var board = new string?[][]
        {
            ["X", "O", "X"],
            ["X", "O", "O"],
            ["O", "X", "X"]
        };

        var act = () => CreateSut().SelectMove(board);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("No available moves.");
    }
}
