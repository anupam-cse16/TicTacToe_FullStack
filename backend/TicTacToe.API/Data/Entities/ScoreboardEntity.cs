using System.ComponentModel.DataAnnotations;

namespace TicTacToe.API.Data.Entities;

public class ScoreboardEntity
{
    [Key]
    public int Id { get; set; } = 1;

    public int XWins { get; set; } = 0;

    public int OWins { get; set; } = 0;

    public int Draws { get; set; } = 0;
}
