using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TicTacToe.API.Data;
using TicTacToe.API.Data.Entities;
using TicTacToe.API.Data.Repositories;
using TicTacToe.API.Data.UnitOfWork;
using TicTacToe.API.Models;
using FluentAssertions;

namespace TicTacToe.Tests;

public class RepositoryAndMappingTests : IDisposable
{
    private readonly TicTacToeDbContext _db;
    private readonly SqliteConnection _connection;
    private readonly IGameRepository _gameRepo;
    private readonly IScoreboardRepository _scoreRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public RepositoryAndMappingTests()
    {
        (_db, _connection) = TestDbContextFactory.CreateInMemoryContext();
        _gameRepo = new GameRepository(_db);
        _scoreRepo = new ScoreboardRepository(_db);
        _uow = new UnitOfWork(_db, _gameRepo, _scoreRepo);
        _mapper = TestDbContextFactory.CreateMapper();
    }

    public void Dispose()
    {
        _uow.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GameRepository_CrudOperations_WorkAsExpected()
    {
        var entity = new GameSessionEntity
        {
            Id = Guid.NewGuid(),
            CurrentPlayer = "X",
            Mode = "TwoPlayer",
            Status = "InProgress",
            LastActiveUtc = DateTime.UtcNow
        };

        // Add
        _gameRepo.Add(entity);
        await _uow.SaveChangesAsync();

        // GetById
        var fetched = _gameRepo.GetById(entity.Id);
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(entity.Id);

        // GetByIdAsync with AsNoTracking
        var untracked = await _gameRepo.GetByIdAsync(entity.Id, asNoTracking: true);
        untracked.Should().NotBeNull();
        _db.Entry(untracked!).State.Should().Be(EntityState.Detached);

        // Update
        entity.CurrentPlayer = "O";
        _gameRepo.Update(entity);
        _uow.SaveChanges();

        var updated = _gameRepo.GetById(entity.Id);
        updated!.CurrentPlayer.Should().Be("O");

        // Remove
        _gameRepo.Remove(entity);
        _uow.SaveChanges();

        _gameRepo.GetById(entity.Id).Should().BeNull();
    }

    [Fact]
    public void GameRepository_GetStaleSessions_And_RemoveRange_WorkWithIndex()
    {
        var stale1 = new GameSessionEntity { Id = Guid.NewGuid(), LastActiveUtc = DateTime.UtcNow.AddHours(-5) };
        var stale2 = new GameSessionEntity { Id = Guid.NewGuid(), LastActiveUtc = DateTime.UtcNow.AddHours(-4) };
        var active = new GameSessionEntity { Id = Guid.NewGuid(), LastActiveUtc = DateTime.UtcNow };

        _gameRepo.Add(stale1);
        _gameRepo.Add(stale2);
        _gameRepo.Add(active);
        _uow.SaveChanges();

        var cutoff = DateTime.UtcNow.AddHours(-2);
        var stale = _gameRepo.GetStaleSessions(cutoff);

        stale.Should().HaveCount(2);
        stale.Select(s => s.Id).Should().Contain([stale1.Id, stale2.Id]);

        _gameRepo.RemoveRange(stale);
        _uow.SaveChanges();

        _gameRepo.GetById(stale1.Id).Should().BeNull();
        _gameRepo.GetById(stale2.Id).Should().BeNull();
        _gameRepo.GetById(active.Id).Should().NotBeNull();
    }

    [Fact]
    public void ScoreboardRepository_GetOrCreate_AsNoTracking_And_Reset()
    {
        // Get with AsNoTracking
        var untracked = _scoreRepo.GetOrCreate(asNoTracking: true);
        untracked.Should().NotBeNull();
        _db.Entry(untracked).State.Should().Be(EntityState.Detached);

        // Mutate and Reset
        var tracked = _scoreRepo.GetOrCreate(asNoTracking: false);
        tracked.XWins = 5;
        tracked.OWins = 3;
        tracked.Draws = 2;
        _uow.SaveChanges();

        _scoreRepo.Reset();
        _uow.SaveChanges();

        var resetScore = _scoreRepo.GetOrCreate(asNoTracking: true);
        resetScore.XWins.Should().Be(0);
        resetScore.OWins.Should().Be(0);
        resetScore.Draws.Should().Be(0);
    }

    [Fact]
    public void UnitOfWork_ExposesRepositoriesAndSavesChanges()
    {
        _uow.Games.Should().NotBeNull();
        _uow.Scoreboard.Should().NotBeNull();

        var entity = new GameSessionEntity { Id = Guid.NewGuid(), LastActiveUtc = DateTime.UtcNow };
        _uow.Games.Add(entity);
        var affected = _uow.SaveChanges();

        affected.Should().BeGreaterThan(0);
    }

    [Fact]
    public void AutoMapper_MapsGameSessionBidirectionally()
    {
        var domain = new GameSession
        {
            Id = Guid.NewGuid(),
            Board = [["X", "O", null], [null, "X", null], [null, null, "O"]],
            CurrentPlayer = "X",
            Mode = GameMode.Computer,
            Status = GameStatus.InProgress,
            WinningCells = [new WinningCell { Row = 0, Col = 0 }, new WinningCell { Row = 1, Col = 1 }],
            MoveHistory = [new MoveRecord { MoveNumber = 1, Player = "X", Row = 0, Col = 0 }],
            LastActiveUtc = DateTime.UtcNow
        };

        // Domain -> Entity
        var entity = _mapper.Map<GameSessionEntity>(domain);
        entity.Id.Should().Be(domain.Id);
        entity.Mode.Should().Be("Computer");
        entity.Status.Should().Be("InProgress");
        entity.BoardJson.Should().Contain("\"X\"");

        // Entity -> Domain
        var mappedDomain = _mapper.Map<GameSession>(entity);
        mappedDomain.Id.Should().Be(domain.Id);
        mappedDomain.Mode.Should().Be(GameMode.Computer);
        mappedDomain.Status.Should().Be(GameStatus.InProgress);
        mappedDomain.Board[0][0].Should().Be("X");
        mappedDomain.Board[0][1].Should().Be("O");
        mappedDomain.WinningCells.Should().HaveCount(2);
        mappedDomain.MoveHistory.Should().HaveCount(1);
    }

    [Fact]
    public void AutoMapper_MapsScoreboardBidirectionally()
    {
        var domainScore = new Scoreboard { XWins = 4, OWins = 2, Draws = 1 };

        var entity = _mapper.Map<ScoreboardEntity>(domainScore);
        entity.XWins.Should().Be(4);
        entity.OWins.Should().Be(2);
        entity.Draws.Should().Be(1);

        var mappedBack = _mapper.Map<Scoreboard>(entity);
        mappedBack.XWins.Should().Be(4);
        mappedBack.OWins.Should().Be(2);
        mappedBack.Draws.Should().Be(1);
    }
}
