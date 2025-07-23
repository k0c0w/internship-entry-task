using System;
using TicTacToe.Domain;

namespace TicTacToe.Services;

public record GameDto
{
    public Guid Id { get; init; }

    public char? PlayerTurn { get; init; }

    public char[][] Board { get; init; }

    public DateTime ModifiedAt { get; init; }

    public string? Winner { get; init; }
}

public static class GameExtensions
{
    public static GameDto ToDto(this Game game)
    {
        var board = new char[game.BoardSize][];
        for(var offset = 0; offset < game.BoardSize; offset++)
        {
            board[offset] = new char[game.BoardSize];
            for(var i = 0;  i < game.BoardSize; i++)
            {
                board[offset][i] = FromSymbol(game.Board[offset * game.BoardSize + i]);
            }
        }

        return new GameDto
        {
            Id = game.Id,
            ModifiedAt = game.ModifiedAt,
            PlayerTurn = game.Completed ? null :FromSymbol(game.CurrentPlayer),
            Winner = game.Completed ? (game.Winner == TicTacToeSymbol.None ? "draw": FromSymbol(game.Winner).ToString()) : null,
            Board = board,
        };
    }

    private static char FromSymbol(TicTacToeSymbol symbol)
    {
        return symbol switch
        {
            TicTacToeSymbol.X => 'X',
            TicTacToeSymbol.O => 'O',
            TicTacToeSymbol.None => ' ',
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
