using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TicTacToe.API.Models;
using TicTacToe.API.Models.Requests;
using TicTacToe.API.Models.Responses;
using FluentAssertions;

namespace TicTacToe.Tests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateGame_ViaHttp_Returns201AndValidSession()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new CreateGameRequest { Mode = "TwoPlayer" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var game = await response.Content.ReadFromJsonAsync<GameStateResponse>();
        game.Should().NotBeNull();
        game!.Mode.Should().Be("TwoPlayer");
        game.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task FullGameFlow_ViaHttp_WorksEndToEnd()
    {
        // 1. Create game
        var createResponse = await _client.PostAsJsonAsync("/api/games", new CreateGameRequest { Mode = "TwoPlayer" });
        var game = await createResponse.Content.ReadFromJsonAsync<GameStateResponse>();

        // 2. Make moves to win for X: (0,0), (1,0), (0,1), (1,1), (0,2)
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/moves", new MakeMoveRequest { Player = "X", Row = 0, Col = 0 });
        await _client.PostAsJsonAsync($"/api/games/{game.Id}/moves", new MakeMoveRequest { Player = "O", Row = 1, Col = 0 });
        await _client.PostAsJsonAsync($"/api/games/{game.Id}/moves", new MakeMoveRequest { Player = "X", Row = 0, Col = 1 });
        await _client.PostAsJsonAsync($"/api/games/{game.Id}/moves", new MakeMoveRequest { Player = "O", Row = 1, Col = 1 });
        var winResponse = await _client.PostAsJsonAsync($"/api/games/{game.Id}/moves", new MakeMoveRequest { Player = "X", Row = 0, Col = 2 });

        var wonGame = await winResponse.Content.ReadFromJsonAsync<GameStateResponse>();
        wonGame!.Status.Should().Be("Won");
        wonGame.Winner.Should().Be("X");

        // 3. Scoreboard check
        var scoreResponse = await _client.GetFromJsonAsync<Scoreboard>("/api/scoreboard");
        scoreResponse.Should().NotBeNull();
        scoreResponse!.XWins.Should().BeGreaterThan(0);

        // 4. Reset game
        var resetResponse = await _client.PostAsync($"/api/games/{game.Id}/reset", null);
        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Reset scoreboard
        var resetScoreResponse = await _client.PostAsync("/api/scoreboard/reset", null);
        resetScoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
