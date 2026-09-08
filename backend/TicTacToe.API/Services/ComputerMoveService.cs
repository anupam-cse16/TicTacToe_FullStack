using Microsoft.Extensions.Options;
using TicTacToe.API.Configuration;
using TicTacToe.API.Services.Strategies;

namespace TicTacToe.API.Services;

public class ComputerMoveService : IComputerMoveService
{
    private readonly IWinDetectionService _winDetection;
    private readonly GameSettings _settings;
    private readonly IMoveStrategy _strategy;

    public ComputerMoveService(
        IWinDetectionService winDetection,
        IOptions<GameSettings>? options = null,
        IEnumerable<IMoveStrategy>? strategies = null)
    {
        _winDetection = winDetection;
        _settings = options?.Value ?? new GameSettings();

        var strategyList = (strategies ?? [new RuleBasedMoveStrategy(), new MinimaxMoveStrategy()]).ToList();
        _strategy = strategyList.FirstOrDefault(s =>
            s.StrategyName.Equals(_settings.AiStrategy, StringComparison.OrdinalIgnoreCase))
            ?? strategyList.First();
    }

    public (int Row, int Col) SelectMove(string?[][] board) =>
        _strategy.SelectMove(board, _settings, _winDetection);
}
