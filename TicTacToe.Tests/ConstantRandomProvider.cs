using TicTacToe.Domain;

namespace TicTacToe.Tests;

internal class ConstantRandomProvider : IRandomProvider
{
    public Random Random => new Random(0);
    
}
