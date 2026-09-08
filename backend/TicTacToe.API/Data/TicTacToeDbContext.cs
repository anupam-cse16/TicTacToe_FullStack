using Microsoft.EntityFrameworkCore;
using TicTacToe.API.Data.Entities;

namespace TicTacToe.API.Data;

public class TicTacToeDbContext(DbContextOptions<TicTacToeDbContext> options) : DbContext(options)
{
    public DbSet<GameSessionEntity> GameSessions => Set<GameSessionEntity>();
    public DbSet<ScoreboardEntity> Scoreboards => Set<ScoreboardEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GameSessionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LastActiveUtc);
            entity.Property(e => e.BoardJson).IsRequired();
            entity.Property(e => e.CurrentPlayer).IsRequired();
            entity.Property(e => e.Mode).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.WinningCellsJson).IsRequired();
            entity.Property(e => e.MoveHistoryJson).IsRequired();
            entity.Property(e => e.LastActiveUtc).IsRequired();
        });

        modelBuilder.Entity<ScoreboardEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasData(new ScoreboardEntity
            {
                Id = 1,
                XWins = 0,
                OWins = 0,
                Draws = 0
            });
        });
    }
}
