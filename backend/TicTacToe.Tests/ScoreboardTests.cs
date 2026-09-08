using AutoMapper;
using Microsoft.Data.Sqlite;
using TicTacToe.API.Data;
using TicTacToe.API.Data.UnitOfWork;
using TicTacToe.API.Models;
using TicTacToe.API.Models.Requests;
using TicTacToe.API.Services;
using FluentAssertions;

namespace TicTacToe.Tests;

public class ScoreboardTests : IDisposable
{
    private readonly IUnitOfWork _uow;
    private readonly SqliteConnection _connection;
    private readonly TicTacToeDbContext _db;
    private readonly IMapper _mapper;
    private readonly GameService _gameSvc;
    private readonly ScoreboardService _scoreSvc;

    public ScoreboardTests()
    {
        (_uow, _connection, _db) = TestDbContextFactory.CreateUnitOfWork();
        _mapper = TestDbContextFactory.CreateMapper();
        var winDetection = new WinDetectionService();
        var computerMove = new ComputerMoveService(winDetection);
        _scoreSvc = new ScoreboardService(_uow, _mapper);
        _gameSvc = new GameService(_uow, _mapper, winDetection, computerMove, _scoreSvc);
    }

    private (GameService GameSvc, ScoreboardService ScoreSvc) CreateSut() => (_gameSvc, _scoreSvc);

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void Scoreboard_UpdatesOnXWin()
    {
        var (svc, score) = CreateSut();
        var session = svc.CreateGame("TwoPlayer");

        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 0 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 1 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 1 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 2 });

        score.GetScoreboard().XWins.Should().Be(1);
        score.GetScoreboard().OWins.Should().Be(0);
    }

    [Fact]
    public void Scoreboard_UpdatesOnDraw()
    {
        var (svc, score) = CreateSut();
        var session = svc.CreateGame("TwoPlayer");

        // X O X / X X O / O X O — verified draw, strictly alternating
        var moves = new[]
        {
            ("X", 0, 0), ("O", 0, 1), ("X", 0, 2),
            ("O", 2, 0), ("X", 1, 0), ("O", 2, 2),
            ("X", 1, 1), ("O", 1, 2), ("X", 2, 1)
        };
        foreach (var (player, row, col) in moves)
            svc.MakeMove(session.Id, new MakeMoveRequest { Player = player, Row = row, Col = col });

        score.GetScoreboard().Draws.Should().Be(1);
    }

    [Fact]
    public void Scoreboard_DoesNotDoubleCount()
    {
        var (svc, score) = CreateSut();
        var session = svc.CreateGame("TwoPlayer");

        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 0 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 1 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 1 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 2 });

        // Manually call UpdateScoreboard again
        score.UpdateScoreboard(svc.GetGame(session.Id));
        score.UpdateScoreboard(svc.GetGame(session.Id));

        score.GetScoreboard().XWins.Should().Be(1); // Not 3
    }

    [Fact]
    public void Scoreboard_Reset_ClearsAll()
    {
        var (svc, score) = CreateSut();
        var session = svc.CreateGame("TwoPlayer");

        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 0 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 1 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 1 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 2 });

        score.ResetScoreboard();

        score.GetScoreboard().XWins.Should().Be(0);
        score.GetScoreboard().OWins.Should().Be(0);
        score.GetScoreboard().Draws.Should().Be(0);
    }

    [Fact]
    public void Scoreboard_UnchangedAfterGameReset()
    {
        var (svc, score) = CreateSut();
        var session = svc.CreateGame("TwoPlayer");

        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 0 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 1 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 1 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 2 }); // X wins

        svc.Reset(session.Id); // Game reset

        score.GetScoreboard().XWins.Should().Be(1); // Scoreboard unchanged
    }

    [Fact]
    public void Scoreboard_PruneStaleGameIds_PurgesOldEntries()
    {
        var (svc, score) = CreateSut();
        var session = svc.CreateGame("TwoPlayer");

        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 0 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 1 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 1 });
        svc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 2 }); // X wins

        // Pruning with 0 age should remove the recorded game ID
        var pruned = score.PruneStaleGameIds(TimeSpan.FromSeconds(-1));
        pruned.Should().Be(1);

        // Pruning again should find 0 to prune
        score.PruneStaleGameIds(TimeSpan.FromHours(1)).Should().Be(0);
    }

    [Fact]
    public void Scoreboard_RecreatesEntity_WhenRowIsMissing()
    {
        _db.Scoreboards.RemoveRange(_db.Scoreboards);
        _db.SaveChanges();

        var score = _scoreSvc.GetScoreboard();
        score.Should().NotBeNull();
        score.XWins.Should().Be(0);
    }

    [Fact]
    public void ScoreboardService_WithLoggerInjected_ExecutesAndLogs()
    {
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ScoreboardService>.Instance;
        var scoreSvc = new ScoreboardService(_uow, _mapper, logger);
        var session = new GameSession { Id = Guid.NewGuid(), Status = GameStatus.Won, Winner = "X" };
        scoreSvc.UpdateScoreboard(session);
        scoreSvc.ResetScoreboard();
        scoreSvc.PruneStaleGameIds(TimeSpan.FromSeconds(-1));
        scoreSvc.GetScoreboard().Should().NotBeNull();
    }
}
