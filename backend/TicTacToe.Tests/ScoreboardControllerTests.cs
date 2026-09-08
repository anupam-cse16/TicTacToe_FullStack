using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using TicTacToe.API.Controllers;
using TicTacToe.API.Data;
using TicTacToe.API.Data.UnitOfWork;
using TicTacToe.API.Models;
using TicTacToe.API.Services;
using FluentAssertions;

namespace TicTacToe.Tests;

public class ScoreboardControllerTests : IDisposable
{
    private readonly IUnitOfWork _uow;
    private readonly SqliteConnection _connection;
    private readonly ScoreboardService _scoreboardService;
    private readonly ScoreboardController _controller;

    public ScoreboardControllerTests()
    {
        (_uow, _connection, _) = TestDbContextFactory.CreateUnitOfWork();
        var mapper = TestDbContextFactory.CreateMapper();
        _scoreboardService = new ScoreboardService(_uow, mapper);
        _controller = new ScoreboardController(_scoreboardService);
    }

    public void Dispose()
    {
        _uow.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void GetScoreboard_ReturnsOkWithScoreboard()
    {
        var result = _controller.GetScoreboard() as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var scoreboard = result.Value as Scoreboard;
        scoreboard.Should().NotBeNull();
        scoreboard!.XWins.Should().Be(0);
        scoreboard.OWins.Should().Be(0);
        scoreboard.Draws.Should().Be(0);
    }

    [Fact]
    public void ResetScoreboard_ReturnsOkWithResetScoreboard()
    {
        // Simulate a win
        var session = new GameSession { Status = GameStatus.Won, Winner = "X" };
        _scoreboardService.UpdateScoreboard(session);

        var result = _controller.ResetScoreboard() as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var scoreboard = result.Value as Scoreboard;
        scoreboard!.XWins.Should().Be(0);
    }
}
