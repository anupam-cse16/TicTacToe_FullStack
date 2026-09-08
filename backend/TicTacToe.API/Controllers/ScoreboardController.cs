using Microsoft.AspNetCore.Mvc;
using TicTacToe.API.Services;

namespace TicTacToe.API.Controllers;

[ApiController]
[Route("api/scoreboard")]
public class ScoreboardController(IScoreboardService scoreboardService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetScoreboard() =>
        Ok(scoreboardService.GetScoreboard());

    [HttpPost("reset")]
    public IActionResult ResetScoreboard()
    {
        scoreboardService.ResetScoreboard();
        return Ok(scoreboardService.GetScoreboard());
    }
}
