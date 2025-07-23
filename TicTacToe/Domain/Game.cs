using System;

namespace TicTacToe.Domain;

public class Game
{
    public int BoardSize { get; protected set; }

    public int WinCondition { get; protected set; }

    public Guid Id { get; protected set; }

    public TicTacToeSymbol[] Board { get; protected set; }

    public TicTacToeSymbol CurrentPlayer { get; protected set; }

    public bool Completed { get; protected set; }

    public TicTacToeSymbol Winner { get; protected set; }

    public DateTime ModifiedAt { get; protected set; }

    public int MovesCompleted { get; protected set; }

    public static Game CreateNew(int boardSize, int winCondition)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(boardSize, 3, nameof(boardSize));
        ArgumentOutOfRangeException.ThrowIfLessThan(winCondition, 3, nameof(winCondition));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(winCondition, boardSize, nameof(winCondition));
        return new Game
        {
            Id = Guid.CreateVersion7(),
            BoardSize = boardSize,
            Board = new TicTacToeSymbol[boardSize * boardSize],
            CurrentPlayer = TicTacToeSymbol.X,
            ModifiedAt = DateTime.UtcNow,
            Winner = TicTacToeSymbol.None,
            MovesCompleted = 0,
            WinCondition = winCondition,
        };
    }

    protected Game() { }

    public void MakeMove((int X, int Y) pos, TicTacToeSymbol symbol, IRandomProvider randomProvider)
    {
        var (x, y) = pos;
        if (Completed)
        {
            throw new ArgumentException($"The Game has been completed at {ModifiedAt}.");
        }
        if (symbol != CurrentPlayer)
        {
            throw new ArgumentException($"{CurrentPlayer} has turn now.");
        }
        if (!ValidateCoordinate(x) || !ValidateCoordinate(y))
        {
            throw new ArgumentException("Coordinates are out of board range.");
        }

        var index = GetIndex(x, y);
        if (Board[index] != TicTacToeSymbol.None)
        {
            throw new ArgumentException($"Position {pos} is already placed.");
        }

        Board[index] = ShouldFlipPlayer(randomProvider) ? symbol.OppositeSymbol() : symbol;
        MovesCompleted++;
        ModifiedAt = DateTime.UtcNow;

        var (isCompleted, winner) = IsGameOver(Board);
        Completed = isCompleted;
        Winner = winner;
        CurrentPlayer = CurrentPlayer.OppositeSymbol();
    }

    private (bool IsCompleted, TicTacToeSymbol Winner) IsGameOver(TicTacToeSymbol[] board)
    {
        // Check rows
        for (var i = 0; i < BoardSize; i++)
        {
            for (var j = 0; j <= BoardSize - WinCondition; j++)
            {
                var win = true;
                var symbol = board[i * BoardSize + j];
                if (symbol == TicTacToeSymbol.None) continue;
                for (var k = 0; k < WinCondition; k++)
                {
                    if (board[i * BoardSize + j + k] != symbol)
                    {
                        win = false;
                        break;
                    }
                }
                if (win) return (true, symbol);
            }
        }

        // Check columns
        for (var j = 0; j < BoardSize; j++)
        {
            for (var i = 0; i <= BoardSize - WinCondition; i++)
            {
                var win = true;
                var symbol = board[i * BoardSize + j];
                if (symbol == TicTacToeSymbol.None) continue;
                for (var k = 0; k < WinCondition; k++)
                {
                    if (board[(i + k) * BoardSize + j] != symbol)
                    {
                        win = false;
                        break;
                    }
                }
                if (win) return (true, symbol);
            }
        }

        // Check main diagonal (top-left to bottom-right)
        for (var i = 0; i <= BoardSize - WinCondition; i++)
        {
            for (var j = 0; j <= BoardSize - WinCondition; j++)
            {
                var win = true;
                var symbol = board[i * BoardSize + j];
                if (symbol == TicTacToeSymbol.None) continue;
                for (var k = 0; k < WinCondition; k++)
                {
                    if (board[(i + k) * BoardSize + j + k] != symbol)
                    {
                        win = false;
                        break;
                    }
                }
                if (win) return (true, symbol);
            }
        }

        // Check secondary diagonal (top-right to bottom-left)
        for (var i = 0; i <= BoardSize - WinCondition; i++)
        {
            for (var j = WinCondition - 1; j < BoardSize; j++)
            {
                var win = true;
                var symbol = board[i * BoardSize + j];
                if (symbol == TicTacToeSymbol.None) continue;
                for (var k = 0; k < WinCondition; k++)
                {
                    if (board[(i + k) * BoardSize + j - k] != symbol)
                    {
                        win = false;
                        break;
                    }
                }
                if (win) return (true, symbol);
            }
        }

        // Check for draw (board is full)
        var isDraw = true;
        for (var i = 0; i < board.Length; i++)
        {
            if (board[i] == TicTacToeSymbol.None)
            {
                isDraw = false;
                break;
            }
        }
        if (isDraw)
        {
            return (true, TicTacToeSymbol.None);
        }

        return (false, TicTacToeSymbol.None);
    }

    private bool ShouldFlipPlayer(IRandomProvider randomProvider)
    {
        if (MovesCompleted > 0 && (MovesCompleted + 1) % 3 == 0)
        {
            return randomProvider.Random.Next(0, 10) == 0;
        }
        return false;
    }

    private bool ValidateCoordinate(int coordinate)
    {
        return coordinate >= 0 && coordinate < BoardSize;
    }

    private int GetIndex(int x, int y)
    {
        return x * BoardSize + y;
    }
}