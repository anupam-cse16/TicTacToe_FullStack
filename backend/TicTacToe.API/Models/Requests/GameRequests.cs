using System.ComponentModel.DataAnnotations;

namespace TicTacToe.API.Models.Requests;

public class CreateGameRequest
{
    public string Mode { get; set; } = "TwoPlayer";
}

public class MakeMoveRequest
{
    [Required]
    public string Player { get; set; } = string.Empty;

    [Range(0, 100)]
    public int Row { get; set; }

    [Range(0, 100)]
    public int Col { get; set; }
}
