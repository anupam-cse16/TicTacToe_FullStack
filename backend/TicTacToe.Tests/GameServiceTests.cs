using AutoMapper;
using Microsoft.Data.Sqlite;
using TicTacToe.API.Data;
using TicTacToe.API.Data.UnitOfWork;
using TicTacToe.API.Models;
using TicTacToe.API.Models.Requests;
using TicTacToe.API.Services;
using FluentAssertions;

namespace TicTacToe.Tests;

public class GameServiceTests : IDisposable
{
    private readonly IUnitOfWork _uow;
    private readonly SqliteConnection _connection;
    private readonly TicTacToeDbContext _db;
    private readonly IMapper _mapper;
    private readonly GameService _sut;

    public GameServiceTests()
    {
        (_uow, _connection, _db) = TestDbContextFactory.CreateUnitOfWork();
        _mapper = TestDbContextFactory.CreateMapper();
        var winDetection = new WinDetectionService();
        var computerMove = new ComputerMoveService(winDetection);
        var scoreboard = new ScoreboardService(_uow, _mapper);
        _sut = new GameService(_uow, _mapper, winDetection, computerMove, scoreboard);
    }

    private GameService CreateSut() => _sut;

    public void Dispose()
    {
        _uow.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void CreateGame_TwoPlayer_ReturnsNewSession()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");

        session.Should().NotBeNull();
        session.Mode.Should().Be(GameMode.TwoPlayer);
        session.Status.Should().Be(GameStatus.InProgress);
        session.CurrentPlayer.Should().Be("X");
        session.MoveHistory.Should().BeEmpty();
    }

    [Fact]
    public void MakeMove_ValidMove_UpdatesBoard()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");

        var result = sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });

        result.Board[0][0].Should().Be("X");
        result.CurrentPlayer.Should().Be("O");
        result.MoveHistory.Should().HaveCount(1);
    }

    [Fact]
    public void MakeMove_OccupiedCell_Throws()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });

        var act = () => sut.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 0, Col = 0 });

        act.Should().Throw<InvalidOperationException>().WithMessage("*occupied*");
    }

    [Fact]
    public void MakeMove_OutOfBounds_Throws()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");

        var act = () => sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 5, Col = 0 });

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MakeMove_WrongPlayer_Throws()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");

        var act = () => sut.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 0, Col = 0 });

        act.Should().Throw<InvalidOperationException>().WithMessage("*X's turn*");
    }

    [Fact]
    public void MakeMove_TurnsAlternate()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");

        var s1 = sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        s1.CurrentPlayer.Should().Be("O");

        var s2 = sut.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 1 });
        s2.CurrentPlayer.Should().Be("X");
    }

    [Fact]
    public void MakeMove_RowWin_DetectsWinner()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");

        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 0 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 1 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 1 });
        var result = sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 2 });

        result.Status.Should().Be(GameStatus.Won);
        result.Winner.Should().Be("X");
        result.WinningCells.Should().HaveCount(3);
    }

    [Fact]
    public void MakeMove_ColumnWin_DetectsWinner()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");

        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 0, Col = 1 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 1, Col = 0 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 1 });
        var result = sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 2, Col = 0 });

        result.Status.Should().Be(GameStatus.Won);
        result.Winner.Should().Be("X");
    }

    [Fact]
    public void MakeMove_DiagonalWin_DetectsWinner()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");

        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 0, Col = 1 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 1, Col = 1 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 0, Col = 2 });
        var result = sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 2, Col = 2 });

        result.Status.Should().Be(GameStatus.Won);
        result.Winner.Should().Be("X");
    }

    [Fact]
    public void MakeMove_Draw_DetectsDraw()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");

        // Board result:
        // X O X
        // X X O
        // O X O  <- verified draw (no winner)
        // Moves strictly alternate: X O X O X O X O X
        var moves = new[]
        {
            ("X", 0, 0), ("O", 0, 1), ("X", 0, 2),
            ("O", 2, 0), ("X", 1, 0), ("O", 2, 2),
            ("X", 1, 1), ("O", 1, 2), ("X", 2, 1)
        };

        GameSession? result = null;
        foreach (var (player, row, col) in moves)
            result = sut.MakeMove(session.Id, new MakeMoveRequest { Player = player, Row = row, Col = col });

        result!.Status.Should().Be(GameStatus.Draw);
        result.Winner.Should().BeNull();
    }

    [Fact]
    public void MakeMove_AfterCompletion_Throws()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");

        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 0 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 1 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 1 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 2 }); // X wins

        var act = () => sut.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 2, Col = 0 });

        act.Should().Throw<InvalidOperationException>().WithMessage("*completed*");
    }

    [Fact]
    public void Reset_ClearsBoard_KeepsMode()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });

        var result = sut.Reset(session.Id);

        result.Board.SelectMany(r => r).Should().AllSatisfy(cell => cell.Should().BeNull());
        result.CurrentPlayer.Should().Be("X");
        result.Status.Should().Be(GameStatus.InProgress);
        result.MoveHistory.Should().BeEmpty();
        result.Mode.Should().Be(GameMode.TwoPlayer);
    }

    [Fact]
    public void Undo_TwoPlayerMode_RemovesOneMove()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");

        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 1 });

        var result = sut.Undo(session.Id);

        result.MoveHistory.Should().HaveCount(1);
        result.CurrentPlayer.Should().Be("O");
        result.Board[1][1].Should().BeNull();
        result.Board[0][0].Should().Be("X");
    }

    [Fact]
    public void Undo_NoMoves_Throws()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");

        var act = () => sut.Undo(session.Id);

        act.Should().Throw<InvalidOperationException>().WithMessage("*No moves*");
    }

    [Fact]
    public void Undo_AfterCompletion_Throws()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");

        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 0 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 1 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 1 });
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 2 });

        var act = () => sut.Undo(session.Id);

        act.Should().Throw<InvalidOperationException>().WithMessage("*completion*");
    }

    [Fact]
    public void Undo_ComputerMode_RemovesTwoMoves()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("Computer");

        // X plays -> computer (O) plays automatically
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        var historyAfterFirstRound = sut.GetGame(session.Id).MoveHistory.Count;

        var result = sut.Undo(session.Id);

        result.MoveHistory.Should().HaveCount(historyAfterFirstRound - 2);
        result.CurrentPlayer.Should().Be("X");
    }

    [Fact]
    public void GetGame_UnknownId_ThrowsKeyNotFoundException()
    {
        var sut = CreateSut();
        var act = () => sut.GetGame(Guid.NewGuid());
        act.Should().Throw<KeyNotFoundException>();
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(3, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 3)]
    public void MakeMove_VariousOutOfBounds_ThrowsArgumentOutOfRangeException(int row, int col)
    {
        var sut = CreateSut();
        var session = sut.CreateGame("TwoPlayer");

        var act = () => sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = row, Col = col });
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Undo_ComputerMode_WhenOnlyOneMoveExists_UndoesSingleMove()
    {
        var sut = CreateSut();
        var session = sut.CreateGame("Computer");

        // Simulate a session in DB where only 1 move is recorded
        var entity = _db.GameSessions.Find(session.Id)!;
        entity.BoardJson = "[[\"X\",null,null],[null,null,null],[null,null,null]]";
        entity.MoveHistoryJson = "[{\"MoveNumber\":1,\"Player\":\"X\",\"Row\":0,\"Col\":0}]";
        _db.SaveChanges();

        var result = sut.Undo(session.Id);
        result.MoveHistory.Should().BeEmpty();
        result.CurrentPlayer.Should().Be("X");
        result.Board[0][0].Should().BeNull();
    }

    [Fact]
    public void GameService_WithLoggerInjected_ExecutesAllOperationsAndLogs()
    {
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<GameService>.Instance;
        var winDetection = new WinDetectionService();
        var computerMove = new ComputerMoveService(winDetection);
        var scoreboard = new ScoreboardService(_uow, _mapper);
        var sut = new GameService(_uow, _mapper, winDetection, computerMove, scoreboard, logger);

        var session = sut.CreateGame("Computer");
        sut.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        sut.Undo(session.Id);
        sut.Reset(session.Id);
        sut.GetGame(session.Id).Should().NotBeNull();
    }
}
