using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TicTacToe.API.Data;
using TicTacToe.API.Data.Repositories;
using TicTacToe.API.Data.UnitOfWork;
using TicTacToe.API.Models;
using TicTacToe.API.Models.Requests;
using TicTacToe.API.Services;
using FluentAssertions;

namespace TicTacToe.Tests;

public class SqlitePersistenceTests : IDisposable
{
    private readonly SqliteConnection _sharedConnection;
    private readonly DbContextOptions<TicTacToeDbContext> _options;

    public SqlitePersistenceTests()
    {
        // A shared in-memory connection simulates persistent storage accessible by different DbContext instances
        _sharedConnection = new SqliteConnection("Data Source=shared_test;Mode=Memory;Cache=Shared");
        _sharedConnection.Open();

        _options = new DbContextOptionsBuilder<TicTacToeDbContext>()
            .UseSqlite(_sharedConnection)
            .Options;

        using var db = new TicTacToeDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _sharedConnection.Dispose();
    }

    private class TestScope : IDisposable
    {
        public GameService GameSvc { get; }
        public ScoreboardService ScoreSvc { get; }
        public TicTacToeDbContext Db { get; }
        public IUnitOfWork Uow { get; }

        public TestScope(DbContextOptions<TicTacToeDbContext> options)
        {
            Db = new TicTacToeDbContext(options);
            var gameRepo = new GameRepository(Db);
            var scoreRepo = new ScoreboardRepository(Db);
            Uow = new UnitOfWork(Db, gameRepo, scoreRepo);
            var mapper = TestDbContextFactory.CreateMapper();

            var winDetection = new WinDetectionService();
            var computerMove = new ComputerMoveService(winDetection);
            ScoreSvc = new ScoreboardService(Uow, mapper);
            GameSvc = new GameService(Uow, mapper, winDetection, computerMove, ScoreSvc);
        }

        public void Dispose()
        {
            Uow.Dispose();
        }
    }

    private TestScope CreateScope() => new(_options);

    [Fact]
    public void GameSession_PersistsAcrossDifferentServiceInstances()
    {
        Guid gameId;

        // Scope 1: Create game and make move
        using (var scope1 = CreateScope())
        {
            var session = scope1.GameSvc.CreateGame("TwoPlayer");
            gameId = session.Id;
            scope1.GameSvc.MakeMove(gameId, new MakeMoveRequest { Player = "X", Row = 1, Col = 1 });
        }

        // Scope 2: Simulate another request / server restart reading from DB
        using (var scope2 = CreateScope())
        {
            var retrieved = scope2.GameSvc.GetGame(gameId);
            retrieved.Should().NotBeNull();
            retrieved.Board[1][1].Should().Be("X");
            retrieved.CurrentPlayer.Should().Be("O");
            retrieved.MoveHistory.Should().HaveCount(1);
            retrieved.MoveHistory[0].Row.Should().Be(1);
            retrieved.MoveHistory[0].Col.Should().Be(1);
        }
    }

    [Fact]
    public void Scoreboard_PersistsAcrossDifferentServiceInstances()
    {
        Guid gameId;

        // Scope 1: Play game to win
        using (var scope1 = CreateScope())
        {
            var session = scope1.GameSvc.CreateGame("TwoPlayer");
            gameId = session.Id;
            scope1.GameSvc.MakeMove(gameId, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
            scope1.GameSvc.MakeMove(gameId, new MakeMoveRequest { Player = "O", Row = 1, Col = 0 });
            scope1.GameSvc.MakeMove(gameId, new MakeMoveRequest { Player = "X", Row = 0, Col = 1 });
            scope1.GameSvc.MakeMove(gameId, new MakeMoveRequest { Player = "O", Row = 1, Col = 1 });
            scope1.GameSvc.MakeMove(gameId, new MakeMoveRequest { Player = "X", Row = 0, Col = 2 }); // X wins

            scope1.ScoreSvc.GetScoreboard().XWins.Should().Be(1);
        }

        // Scope 2: Fresh instance (simulating app restart) verifies scoreboard persisted in SQLite
        using (var scope2 = CreateScope())
        {
            var score = scope2.ScoreSvc.GetScoreboard();
            score.XWins.Should().Be(1);
            score.OWins.Should().Be(0);
            score.Draws.Should().Be(0);

            // Verify double count protection holds across server restarts via DB flag
            var game = scope2.GameSvc.GetGame(gameId);
            scope2.ScoreSvc.UpdateScoreboard(game);
            scope2.ScoreSvc.GetScoreboard().XWins.Should().Be(1);
        }
    }

    [Fact]
    public void Scoreboard_Reset_PersistsInDatabase()
    {
        // Scope 1: Play game to win
        using (var scope1 = CreateScope())
        {
            var session = scope1.GameSvc.CreateGame("TwoPlayer");
            scope1.GameSvc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
            scope1.GameSvc.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 0 });
            scope1.GameSvc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 1 });
            scope1.GameSvc.MakeMove(session.Id, new MakeMoveRequest { Player = "O", Row = 1, Col = 1 });
            scope1.GameSvc.MakeMove(session.Id, new MakeMoveRequest { Player = "X", Row = 0, Col = 2 }); // X wins

            scope1.ScoreSvc.ResetScoreboard();
            scope1.ScoreSvc.GetScoreboard().XWins.Should().Be(0);
        }

        // Scope 2: Verify reset state persists
        using (var scope2 = CreateScope())
        {
            var score = scope2.ScoreSvc.GetScoreboard();
            score.XWins.Should().Be(0);
            score.OWins.Should().Be(0);
            score.Draws.Should().Be(0);
        }
    }

    [Fact]
    public void Scoreboard_Draw_PersistsAcrossDifferentServiceInstances()
    {
        // Scope 1: Play game to draw
        using (var scope1 = CreateScope())
        {
            var session = scope1.GameSvc.CreateGame("TwoPlayer");
            var moves = new[]
            {
                ("X", 0, 0), ("O", 0, 1), ("X", 0, 2),
                ("O", 2, 0), ("X", 1, 0), ("O", 2, 2),
                ("X", 1, 1), ("O", 1, 2), ("X", 2, 1)
            };
            foreach (var (player, row, col) in moves)
                scope1.GameSvc.MakeMove(session.Id, new MakeMoveRequest { Player = player, Row = row, Col = col });

            scope1.ScoreSvc.GetScoreboard().Draws.Should().Be(1);
        }

        // Scope 2: Verify persisted draw
        using (var scope2 = CreateScope())
        {
            var score = scope2.ScoreSvc.GetScoreboard();
            score.Draws.Should().Be(1);
        }
    }

    [Fact]
    public void AutoMapper_HandlesUnrecognizedEnumsGracefully()
    {
        var entity = new TicTacToe.API.Data.Entities.GameSessionEntity
        {
            Mode = "UnknownMode",
            Status = "UnknownStatus"
        };

        var mapper = TestDbContextFactory.CreateMapper();
        var domain = mapper.Map<GameSession>(entity);
        domain.Mode.Should().Be(GameMode.TwoPlayer);
        domain.Status.Should().Be(GameStatus.InProgress);
    }
}
