using Microsoft.Extensions.Options;
using TicTacToe.API.Configuration;
using TicTacToe.API.Models;
using TicTacToe.API.Services;
using FluentAssertions;

namespace TicTacToe.Tests;

public class ConfigurationTests
{
    [Fact]
    public void WinDetectionService_WithCustomBoardSize_DetectsWinProperly()
    {
        var settings = Options.Create(new GameSettings { BoardSize = 4 });
        var service = new WinDetectionService(settings);

        service.BoardSize.Should().Be(4);

        // Create 4x4 board
        var board = Enumerable.Range(0, 4).Select(_ => new string?[4]).ToArray();
        board[0][0] = board[0][1] = board[0][2] = board[0][3] = "X";

        var cells = service.CheckWinner(board, out var winner);

        winner.Should().Be("X");
        cells.Should().HaveCount(4);
    }

    [Fact]
    public void ComputerMoveService_WithCustomSettings_SelectsConfiguredCornerAndSymbols()
    {
        var settings = Options.Create(new GameSettings
        {
            BoardSize = 4,
            ComputerPlayer = "O",
            PlayerX = "X"
        });
        var winDetection = new WinDetectionService(settings);
        var computerMove = new ComputerMoveService(winDetection, settings);

        var board = Enumerable.Range(0, 4).Select(_ => new string?[4]).ToArray();

        var (row, col) = computerMove.SelectMove(board);

        // In 4x4, size % 2 == 0 so no single center, priority falls back to first available corner (0,0)
        row.Should().Be(0);
        col.Should().Be(0);
    }
}
