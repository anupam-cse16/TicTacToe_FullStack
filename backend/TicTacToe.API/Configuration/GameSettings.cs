namespace TicTacToe.API.Configuration;

public class GameSettings
{
    public const string SectionName = "GameSettings";

    public int BoardSize { get; set; } = 3;
    public string PlayerX { get; set; } = "X";
    public string PlayerO { get; set; } = "O";
    public string StartingPlayer { get; set; } = "X";
    public string ComputerPlayer { get; set; } = "O";
    public string AiStrategy { get; set; } = "RuleBased";
    public int SessionTtlHours { get; set; } = 2;
    public int CleanupIntervalMinutes { get; set; } = 30;
}
