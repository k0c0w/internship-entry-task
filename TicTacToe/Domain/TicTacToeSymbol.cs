using System.Text.Json.Serialization;

namespace TicTacToe.Domain;


public enum TicTacToeSymbol
{
    None = 0,
    [JsonStringEnumMemberName("X")]
    X = 1,
    [JsonStringEnumMemberName("O")]
    O = 2
}

internal static class TicTacToeSymbolExtensions
{
    public static TicTacToeSymbol OppositeSymbol(this TicTacToeSymbol symbol)
    {
        if (symbol == TicTacToeSymbol.X)
        {
            return TicTacToeSymbol.O;
        }
        else if (symbol == TicTacToeSymbol.O)
        {
            return TicTacToeSymbol.X;
        }

        return TicTacToeSymbol.None;
    }
}