using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TicTacToe.API.Configuration;
using TicTacToe.API.Data.Entities;
using TicTacToe.API.Data.UnitOfWork;
using TicTacToe.API.Models;
using TicTacToe.API.Models.Requests;

namespace TicTacToe.API.Services;

public class GameService(
    IUnitOfWork uow,
    IMapper mapper,
    IWinDetectionService winDetection,
    IComputerMoveService computerMove,
    IScoreboardService scoreboard,
    ILogger<GameService>? logger = null,
    IOptions<GameSettings>? options = null) : IGameService
{
    private readonly ILogger<GameService> _logger = logger ?? NullLogger<GameService>.Instance;
    private readonly GameSettings _settings = options?.Value ?? new GameSettings();

    public GameSession CreateGame(string mode)
    {
        var gameMode = mode.Equals("Computer", StringComparison.OrdinalIgnoreCase)
            ? GameMode.Computer
            : GameMode.TwoPlayer;

        var session = new GameSession
        {
            Mode = gameMode,
            Board = GameSession.CreateEmptyBoard(_settings.BoardSize),
            CurrentPlayer = _settings.StartingPlayer,
            LastActiveUtc = DateTime.UtcNow
        };

        var entity = mapper.Map<GameSessionEntity>(session);
        uow.Games.Add(entity);
        uow.SaveChanges();

        _logger.LogInformation("Created new game session {GameId} with mode {Mode}", session.Id, session.Mode);

        return session;
    }

    public GameSession GetGame(Guid id)
    {
        var entity = uow.Games.GetById(id, asNoTracking: false);
        if (entity == null)
        {
            _logger.LogWarning("Game session {GameId} was not found", id);
            throw new KeyNotFoundException($"Game {id} not found.");
        }

        entity.LastActiveUtc = DateTime.UtcNow;
        uow.SaveChanges();

        return mapper.Map<GameSession>(entity);
    }

    public GameSession MakeMove(Guid id, MakeMoveRequest request)
    {
        var entity = uow.Games.GetById(id, asNoTracking: false);
        if (entity == null)
        {
            _logger.LogWarning("Move rejected: Game session {GameId} not found", id);
            throw new KeyNotFoundException($"Game {id} not found.");
        }

        var session = mapper.Map<GameSession>(entity);

        ValidateMove(session, request.Player, request.Row, request.Col);

        ApplyMove(session, entity, request.Player, request.Row, request.Col);
        _logger.LogInformation("Move played in game {GameId}: Player {Player} at ({Row}, {Col})", id, request.Player, request.Row, request.Col);

        // In computer mode, after a valid human move, computer plays automatically
        if (session.Mode == GameMode.Computer && session.Status == GameStatus.InProgress)
        {
            var (compRow, compCol) = computerMove.SelectMove(session.Board);
            ApplyMove(session, entity, _settings.ComputerPlayer, compRow, compCol);
            _logger.LogInformation("Computer player {Player} made move in game {GameId} at ({Row}, {Col})", _settings.ComputerPlayer, id, compRow, compCol);
        }

        session.LastActiveUtc = DateTime.UtcNow;
        mapper.Map(session, entity);
        uow.SaveChanges();

        return session;
    }

    public GameSession Undo(Guid id)
    {
        var entity = uow.Games.GetById(id, asNoTracking: false);
        if (entity == null)
            throw new KeyNotFoundException($"Game {id} not found.");

        var session = mapper.Map<GameSession>(entity);

        if (session.MoveHistory.Count == 0)
            throw new InvalidOperationException("No moves to undo.");

        if (session.Status != GameStatus.InProgress)
            throw new InvalidOperationException("Cannot undo after game completion.");

        // Computer mode: undo 2 moves (computer + human); Two-player: undo 1
        int movesToUndo = session.Mode == GameMode.Computer ? 2 : 1;
        movesToUndo = Math.Min(movesToUndo, session.MoveHistory.Count);

        for (int i = 0; i < movesToUndo; i++)
        {
            var last = session.MoveHistory[^1];
            session.Board[last.Row][last.Col] = null;
            session.MoveHistory.RemoveAt(session.MoveHistory.Count - 1);
        }

        // Recalculate state
        session.Status = GameStatus.InProgress;
        session.Winner = null;
        session.WinningCells = [];
        session.CurrentPlayer = session.MoveHistory.Count == 0
            ? _settings.StartingPlayer
            : GetOpponent(session.MoveHistory[^1].Player);

        session.LastActiveUtc = DateTime.UtcNow;
        mapper.Map(session, entity);
        uow.SaveChanges();

        _logger.LogInformation("Undid {MovesToUndo} move(s) in game {GameId}. Current player is now {CurrentPlayer}",
            movesToUndo, id, session.CurrentPlayer);

        return session;
    }

    public GameSession Reset(Guid id)
    {
        var entity = uow.Games.GetById(id, asNoTracking: false);
        if (entity == null)
            throw new KeyNotFoundException($"Game {id} not found.");

        var session = mapper.Map<GameSession>(entity);

        session.Board = GameSession.CreateEmptyBoard(_settings.BoardSize);
        session.CurrentPlayer = _settings.StartingPlayer;
        session.Status = GameStatus.InProgress;
        session.Winner = null;
        session.WinningCells = [];
        session.MoveHistory = [];
        session.LastActiveUtc = DateTime.UtcNow;

        mapper.Map(session, entity);
        entity.IsScoreboardProcessed = false;
        uow.SaveChanges();

        _logger.LogInformation("Reset game session {GameId} to initial board state", id);

        return session;
    }

    public int CleanupStaleSessions(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var staleSessions = uow.Games.GetStaleSessions(cutoff);

        if (staleSessions.Count > 0)
        {
            uow.Games.RemoveRange(staleSessions);
            uow.SaveChanges();
            _logger.LogInformation("Evicted {Count} stale game session(s) inactive since {Cutoff}", staleSessions.Count, cutoff);
        }

        return staleSessions.Count;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void ValidateMove(GameSession session, string player, int row, int col)
    {
        if (session.Status != GameStatus.InProgress)
            throw new InvalidOperationException("Game is already completed.");

        if (player != session.CurrentPlayer)
            throw new InvalidOperationException($"It is {session.CurrentPlayer}'s turn.");

        if (row < 0 || row >= _settings.BoardSize || col < 0 || col >= _settings.BoardSize)
            throw new ArgumentOutOfRangeException(nameof(row), "Cell is out of bounds.");

        if (session.Board[row][col] != null)
            throw new InvalidOperationException("Cell is already occupied.");
    }

    private void ApplyMove(GameSession session, GameSessionEntity entity, string player, int row, int col)
    {
        session.Board[row][col] = player;
        session.MoveHistory.Add(new MoveRecord
        {
            MoveNumber = session.MoveHistory.Count + 1,
            Player = player,
            Row = row,
            Col = col
        });

        var winningCells = winDetection.CheckWinner(session.Board, out var winner);
        if (winner != null)
        {
            session.Status = GameStatus.Won;
            session.Winner = winner;
            session.WinningCells = winningCells;
            mapper.Map(session, entity);
            scoreboard.UpdateScoreboard(session);
            _logger.LogInformation("Game {GameId} won by {Winner}", session.Id, winner);
        }
        else if (winDetection.IsBoardFull(session.Board))
        {
            session.Status = GameStatus.Draw;
            mapper.Map(session, entity);
            scoreboard.UpdateScoreboard(session);
            _logger.LogInformation("Game {GameId} concluded in a Draw", session.Id);
        }
        else
        {
            session.CurrentPlayer = GetOpponent(player);
        }
    }

    private string GetOpponent(string player) =>
        player == _settings.PlayerX ? _settings.PlayerO : _settings.PlayerX;
}
