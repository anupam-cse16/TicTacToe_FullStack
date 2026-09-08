using TicTacToe.API.Models;

namespace TicTacToe.API.Models.Responses;

public class GameStateResponse
{
    public Guid Id { get; set; }
    public string?[][] Board { get; set; } = Array.Empty<string?[]>();
    public string CurrentPlayer { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Winner { get; set; }
    public List<WinningCell> WinningCells { get; set; } = new();
    public List<MoveRecord> MoveHistory { get; set; } = new();

    public static GameStateResponse FromSession(GameSession session) => new()
    {
        Id = session.Id,
        Board = session.Board.Select(row => (string?[])row.Clone()).ToArray(),
        CurrentPlayer = session.CurrentPlayer,
        Mode = session.Mode.ToString(),
        Status = session.Status.ToString(),
        Winner = session.Winner,
        WinningCells = [.. session.WinningCells],
        MoveHistory = [.. session.MoveHistory]
    };
}
