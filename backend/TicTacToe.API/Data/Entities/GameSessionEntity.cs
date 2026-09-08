using System.ComponentModel.DataAnnotations;

namespace TicTacToe.API.Data.Entities;

public class GameSessionEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string BoardJson { get; set; } = "[[null,null,null],[null,null,null],[null,null,null]]";

    [Required]
    public string CurrentPlayer { get; set; } = "X";

    [Required]
    public string Mode { get; set; } = "TwoPlayer";

    [Required]
    public string Status { get; set; } = "InProgress";

    public string? Winner { get; set; }

    [Required]
    public string WinningCellsJson { get; set; } = "[]";

    [Required]
    public string MoveHistoryJson { get; set; } = "[]";

    public bool IsScoreboardProcessed { get; set; } = false;

    public DateTime LastActiveUtc { get; set; } = DateTime.UtcNow;
}
