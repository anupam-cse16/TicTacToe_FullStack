using Microsoft.EntityFrameworkCore;
using TicTacToe.API.Data.Entities;

namespace TicTacToe.API.Data.Repositories;

public class ScoreboardRepository(TicTacToeDbContext db) : IScoreboardRepository
{
    public ScoreboardEntity GetOrCreate(bool asNoTracking = false)
    {
        if (asNoTracking)
        {
            var tracked = db.Scoreboards.AsNoTracking().FirstOrDefault(s => s.Id == 1);
            if (tracked != null) return tracked;
        }

        var entity = db.Scoreboards.Find(1);
        if (entity == null)
        {
            entity = new ScoreboardEntity { Id = 1, XWins = 0, OWins = 0, Draws = 0 };
            db.Scoreboards.Add(entity);
            db.SaveChanges();
        }
        return entity;
    }

    public void Reset()
    {
        var entity = GetOrCreate(asNoTracking: false);
        entity.XWins = 0;
        entity.OWins = 0;
        entity.Draws = 0;
    }
}
