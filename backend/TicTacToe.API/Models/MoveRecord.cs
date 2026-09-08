namespace TicTacToe.API.Models;

public class MoveRecord
{
    public int MoveNumber { get; set; }
    public string Player { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Col { get; set; }
}
