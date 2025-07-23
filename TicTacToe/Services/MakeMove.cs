
using System;

namespace TicTacToe.Services;

public record MakeMove
{
    public Guid GameId { get; init; }

    public int X { get; init; }

    public int Y { get; init; }

    public char Symbol { get; init; }
}