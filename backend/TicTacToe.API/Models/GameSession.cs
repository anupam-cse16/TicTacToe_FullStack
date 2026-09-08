namespace TicTacToe.API.Models;

public enum GameMode { TwoPlayer, Computer }
public enum GameStatus { InProgress, Won, Draw }

public class GameSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string?[][] Board { get; set; } = [];
    public string CurrentPlayer { get; set; } = string.Empty;
    public GameMode Mode { get; set; }
    public GameStatus Status { get; set; } = GameStatus.InProgress;
    public string? Winner { get; set; }
    public List<WinningCell> WinningCells { get; set; } = [];
    public List<MoveRecord> MoveHistory { get; set; } = [];
    public DateTime LastActiveUtc { get; set; } = DateTime.UtcNow;

    public static string?[][] CreateEmptyBoard(int size)
    {
        var board = new string?[size][];
        for (var i = 0; i < size; i++)
        {
            board[i] = new string?[size];
        }
        return board;
    }
}
