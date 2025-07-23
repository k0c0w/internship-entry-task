using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ResultMonad;
using TicTacToe.Domain;

namespace TicTacToe.Services;

public class GameService : IGameManager
{
    private readonly IGameRepository games;
    private readonly IRandomProvider randomProvider;
    private readonly IOptionsMonitor<GameSettings> gameSettingsMonitor;

    public GameService(IGameRepository db, IRandomProvider randomProvider, IOptionsMonitor<GameSettings> gameSettings)
    {
        games = db;
        this.randomProvider = randomProvider;
        gameSettingsMonitor = gameSettings;
    }

    public async Task<GameDto> CreateNewGameAsync()
    {
        var settings = gameSettingsMonitor.CurrentValue;
        var game = Game.CreateNew(settings.BoardSize, settings.WinCondition);

        await games.AddAsync(game);

        return game.ToDto();
    }

    public async Task<Result<GameDto, Error>> FindGameAsync(Guid id)
    {
        var game = await games.FindAsync(id);
        if (game is not null)
        {
            return Result.Ok<GameDto, Error>(game.ToDto());
        }

        return Result.Fail<GameDto, Error>(new Error
            {
                Type = Error.ErrorType.NotFoundError,
                Message = "Game not found."
            });
    }

    public async Task<Result<GameDto, Error>> MakeMoveAsync(MakeMove movement)
    {
        var symbolParsingResult = ParseSymbol(movement.Symbol);
        if (symbolParsingResult.IsFailure)
        {
            return Result.Fail<GameDto, Error>(symbolParsingResult.Error);
        }
        var symbol = symbolParsingResult.Value;
        var pos = (movement.X, movement.Y);

        var game = await games.FindAsync(movement.GameId);
        if (game is null)
        {
            return Result.Fail<GameDto, Error>(new Error
            {
                Type = Error.ErrorType.NotFoundError,
                Message = "Game not found."
            });
        }

        try
        {
            game.MakeMove(pos, symbol, randomProvider);

            await games.UpdateAsync(game);

            return Result.Ok<GameDto, Error>(game.ToDto());
        }
        catch (ArgumentException ex)
        {
            return Result.Fail<GameDto, Error>(new Error
            {
                Message = ex.Message,
                Type = Error.ErrorType.ValidationError
            });
        }
    }

    private Result<TicTacToeSymbol, Error> ParseSymbol(char symbol)
    {
        return char.ToLower(symbol) switch
        {
            'x' => Result.Ok<TicTacToeSymbol, Error>(TicTacToeSymbol.X),
            'o' => Result.Ok<TicTacToeSymbol, Error>(TicTacToeSymbol.O),
            _ => Result.Fail<TicTacToeSymbol, Error>(new Error { Type = Error.ErrorType.ValidationError, Message = "Symbol is not recognized."})
        };
    }
}