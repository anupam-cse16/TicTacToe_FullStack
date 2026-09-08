using System.Text.Json;
using AutoMapper;
using TicTacToe.API.Data.Entities;
using TicTacToe.API.Models;

namespace TicTacToe.API.Mapping;

public class GameMappingProfile : Profile
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public GameMappingProfile()
    {
        CreateMap<GameSessionEntity, GameSession>()
            .ForMember(dest => dest.Board,
                opt => opt.MapFrom(src =>
                    JsonSerializer.Deserialize<string?[][]>(src.BoardJson, JsonOptions) ?? Array.Empty<string?[]>()))
            .ForMember(dest => dest.Mode,
                opt => opt.MapFrom(src => ParseMode(src.Mode)))
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => ParseStatus(src.Status)))
            .ForMember(dest => dest.WinningCells,
                opt => opt.MapFrom(src =>
                    JsonSerializer.Deserialize<List<WinningCell>>(src.WinningCellsJson, JsonOptions) ?? new List<WinningCell>()))
            .ForMember(dest => dest.MoveHistory,
                opt => opt.MapFrom(src =>
                    JsonSerializer.Deserialize<List<MoveRecord>>(src.MoveHistoryJson, JsonOptions) ?? new List<MoveRecord>()));

        CreateMap<GameSession, GameSessionEntity>()
            .ForMember(dest => dest.BoardJson,
                opt => opt.MapFrom(src => JsonSerializer.Serialize(src.Board, JsonOptions)))
            .ForMember(dest => dest.Mode,
                opt => opt.MapFrom(src => src.Mode.ToString()))
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.WinningCellsJson,
                opt => opt.MapFrom(src => JsonSerializer.Serialize(src.WinningCells, JsonOptions)))
            .ForMember(dest => dest.MoveHistoryJson,
                opt => opt.MapFrom(src => JsonSerializer.Serialize(src.MoveHistory, JsonOptions)))
            .ForMember(dest => dest.IsScoreboardProcessed,
                opt => opt.Ignore());

        CreateMap<ScoreboardEntity, Scoreboard>();
        CreateMap<Scoreboard, ScoreboardEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());
    }

    private static GameMode ParseMode(string mode) =>
        Enum.TryParse<GameMode>(mode, true, out var m) ? m : GameMode.TwoPlayer;

    private static GameStatus ParseStatus(string status) =>
        Enum.TryParse<GameStatus>(status, true, out var s) ? s : GameStatus.InProgress;
}
