using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using TicTacToe.API.Controllers;
using TicTacToe.API.Data;
using TicTacToe.API.Data.UnitOfWork;
using TicTacToe.API.Models.Requests;
using TicTacToe.API.Models.Responses;
using TicTacToe.API.Services;
using FluentAssertions;

namespace TicTacToe.Tests;

public class GamesControllerTests : IDisposable
{
    private readonly IUnitOfWork _uow;
    private readonly SqliteConnection _connection;
    private readonly WinDetectionService _winDetection = new();
    private readonly ComputerMoveService _computerMove;
    private readonly ScoreboardService _scoreboard;
    private readonly GameService _gameService;
    private readonly GamesController _controller;

    public GamesControllerTests()
    {
        (_uow, _connection, _) = TestDbContextFactory.CreateUnitOfWork();
        var mapper = TestDbContextFactory.CreateMapper();
        _computerMove = new ComputerMoveService(_winDetection);
        _scoreboard = new ScoreboardService(_uow, mapper);
        _gameService = new GameService(_uow, mapper, _winDetection, _computerMove, _scoreboard);
        _controller = new GamesController(_gameService);
    }

    public void Dispose()
    {
        _uow.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void CreateGame_ReturnsCreatedAtAction()
    {
        var result = _controller.CreateGame(new CreateGameRequest { Mode = "TwoPlayer" }) as CreatedAtActionResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(201);
        result.Value.Should().BeOfType<GameStateResponse>();
    }

    [Fact]
    public void GetGame_ExistingGame_ReturnsOk()
    {
        var session = _gameService.CreateGame("TwoPlayer");

        var result = _controller.GetGame(session.Id) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var response = result.Value as GameStateResponse;
        response!.Id.Should().Be(session.Id);
    }

    [Fact]
    public void GetGame_NonExistingGame_ThrowsKeyNotFoundException()
    {
        var act = () => _controller.GetGame(Guid.NewGuid());
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void MakeMove_ValidMove_ReturnsOk()
    {
        var session = _gameService.CreateGame("TwoPlayer");

        var result = _controller.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 }) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var response = result.Value as GameStateResponse;
        response!.Board[0][0].Should().Be("X");
    }

    [Fact]
    public void MakeMove_NonExistingGame_ThrowsKeyNotFoundException()
    {
        var act = () => _controller.MakeMove(Guid.NewGuid(), new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void MakeMove_InvalidMove_ThrowsInvalidOperationException()
    {
        var session = _gameService.CreateGame("TwoPlayer");
        // Out of turn
        var act = () => _controller.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 0, Col = 0 });
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Undo_ValidUndo_ReturnsOk()
    {
        var session = _gameService.CreateGame("TwoPlayer");
        _gameService.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });

        var result = _controller.Undo(session.Id) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var response = result.Value as GameStateResponse;
        response!.Board[0][0].Should().BeNull();
    }

    [Fact]
    public void Undo_NonExistingGame_ThrowsKeyNotFoundException()
    {
        var act = () => _controller.Undo(Guid.NewGuid());
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Undo_NoMovesToUndo_ThrowsInvalidOperationException()
    {
        var session = _gameService.CreateGame("TwoPlayer");

        var act = () => _controller.Undo(session.Id);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reset_ValidReset_ReturnsOk()
    {
        var session = _gameService.CreateGame("TwoPlayer");
        _gameService.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });

        var result = _controller.Reset(session.Id) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var response = result.Value as GameStateResponse;
        response!.Board[0][0].Should().BeNull();
    }

    [Fact]
    public void Reset_NonExistingGame_ThrowsKeyNotFoundException()
    {
        var act = () => _controller.Reset(Guid.NewGuid());
        act.Should().Throw<KeyNotFoundException>();
    }
}
