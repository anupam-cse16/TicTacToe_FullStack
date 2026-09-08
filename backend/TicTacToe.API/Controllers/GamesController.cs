using Microsoft.AspNetCore.Mvc;
using TicTacToe.API.Models.Requests;
using TicTacToe.API.Models.Responses;
using TicTacToe.API.Services;

namespace TicTacToe.API.Controllers;

[ApiController]
[Route("api/games")]
public class GamesController(IGameService gameService) : ControllerBase
{
    [HttpPost]
    public IActionResult CreateGame([FromBody] CreateGameRequest request)
    {
        var session = gameService.CreateGame(request.Mode);
        return CreatedAtAction(nameof(GetGame), new { id = session.Id },
            GameStateResponse.FromSession(session));
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetGame(Guid id)
    {
        var session = gameService.GetGame(id);
        return Ok(GameStateResponse.FromSession(session));
    }

    [HttpPost("{id:guid}/moves")]
    public IActionResult MakeMove(Guid id, [FromBody] MakeMoveRequest request)
    {
        var session = gameService.MakeMove(id, request);
        return Ok(GameStateResponse.FromSession(session));
    }

    [HttpPost("{id:guid}/undo")]
    public IActionResult Undo(Guid id)
    {
        var session = gameService.Undo(id);
        return Ok(GameStateResponse.FromSession(session));
    }

    [HttpPost("{id:guid}/reset")]
    public IActionResult Reset(Guid id)
    {
        var session = gameService.Reset(id);
        return Ok(GameStateResponse.FromSession(session));
    }
}
