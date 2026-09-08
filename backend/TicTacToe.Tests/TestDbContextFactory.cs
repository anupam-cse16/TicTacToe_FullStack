using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.API.Data;
using TicTacToe.API.Data.Repositories;
using TicTacToe.API.Data.UnitOfWork;
using TicTacToe.API.Mapping;

namespace TicTacToe.Tests;

public static class TestDbContextFactory
{
    private static readonly IMapper SharedMapper = new MapperConfiguration(
        cfg => cfg.AddProfile<GameMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    public static IMapper CreateMapper() => SharedMapper;

    public static (TicTacToeDbContext Db, SqliteConnection Connection) CreateInMemoryContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TicTacToeDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TicTacToeDbContext(options);
        context.Database.EnsureCreated();

        return (context, connection);
    }

    public static (IUnitOfWork Uow, SqliteConnection Connection, TicTacToeDbContext Db) CreateUnitOfWork()
    {
        var (db, connection) = CreateInMemoryContext();
        var gameRepo = new GameRepository(db);
        var scoreRepo = new ScoreboardRepository(db);
        var uow = new UnitOfWork(db, gameRepo, scoreRepo);
        return (uow, connection, db);
    }
}
