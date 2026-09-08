using Microsoft.Extensions.Options;
using TicTacToe.API.Configuration;
using TicTacToe.API.Services;
using TicTacToe.API.Services.Strategies;
using FluentAssertions;

namespace TicTacToe.Tests;

public class AiStrategyTests
{
    private readonly WinDetectionService _winDetection = new();
    private readonly GameSettings _settings = new();

    [Fact]
    public void Minimax_FindsWinningMove()
    {
        var strategy = new MinimaxMoveStrategy();
        var board = new string?[][]
        {
            ["O", "O", null],
            ["X", "X", null],
            [null, null, null]
        };

        var (row, col) = strategy.SelectMove(board, _settings, _winDetection);

        // O must complete row 0 to win immediately
        row.Should().Be(0);
        col.Should().Be(2);
    }

    [Fact]
    public void Minimax_BlocksOpponentWinningMove()
    {
        var strategy = new MinimaxMoveStrategy();
        var board = new string?[][]
        {
            ["X", "X", null],
            ["O", null, null],
            [null, null, null]
        };

        var (row, col) = strategy.SelectMove(board, _settings, _winDetection);

        // O must block X at (0, 2)
        row.Should().Be(0);
        col.Should().Be(2);
    }

    [Fact]
    public void Minimax_BoardLargerThan3_FallsBackToRuleBased()
    {
        var strategy = new MinimaxMoveStrategy();
        var customSettings = new GameSettings { BoardSize = 4 };
        var customWinDetection = new WinDetectionService(Options.Create(customSettings));

        var board = Enumerable.Range(0, 4).Select(_ => new string?[4]).ToArray();

        var (row, col) = strategy.SelectMove(board, customSettings, customWinDetection);

        row.Should().Be(0);
        col.Should().Be(0);
    }

    [Fact]
    public void ComputerMoveService_SelectsConfiguredStrategy()
    {
        var settings = Options.Create(new GameSettings { AiStrategy = "Minimax" });
        var service = new ComputerMoveService(_winDetection, settings);

        var board = new string?[][]
        {
            ["O", "O", null],
            ["X", "X", null],
            [null, null, null]
        };

        var (row, col) = service.SelectMove(board);
        row.Should().Be(0);
        col.Should().Be(2);
    }
}
