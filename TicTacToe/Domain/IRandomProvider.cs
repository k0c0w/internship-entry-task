using System;

namespace TicTacToe.Domain;

public interface IRandomProvider
{
    public Random Random { get; }
}

internal class SharedRandomProvider : IRandomProvider
{
    public Random Random => Random.Shared;
}
