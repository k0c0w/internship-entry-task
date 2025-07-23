using Moq;
using TicTacToe.Domain;
using Xunit;

namespace TicTacToe.Tests;

public class GameTests
{
    private readonly Mock<IRandomProvider> AlwaysNonFlipRandomMock;

    public GameTests()
    {
        AlwaysNonFlipRandomMock = new Mock<IRandomProvider>();
        var random = new Mock<Random>();
        random.Setup(x => x.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(1);

        AlwaysNonFlipRandomMock.Setup(x => x.Random).Returns(random.Object);
    }

    [Fact]
    public void MakeMove_ValidMove_UpdatesBoardAndPlayer()
    {
        var game = Game.CreateNew(3, 3);
        var pos = (0, 0);
        var symbol = TicTacToeSymbol.X;

        game.MakeMove(pos, symbol, AlwaysNonFlipRandomMock.Object);

        Assert.Equal(TicTacToeSymbol.X, game.Board[0]);
        Assert.Equal(TicTacToeSymbol.O, game.CurrentPlayer);
        Assert.Equal(1, game.MovesCompleted);
    }

    [Fact]
    public void MakeMove_InvalidSymbol_ThrowsArgumentException()
    {
        var game = Game.CreateNew(3, 3);
        var pos = (0, 0);
        var symbol = TicTacToeSymbol.O;

        var ex = Assert.Throws<ArgumentException>(() => game.MakeMove(pos, symbol, AlwaysNonFlipRandomMock.Object));
        Assert.Equal("X has turn now.", ex.Message);
    }

    [Fact]
    public void MakeMove_CompletedGame_ThrowsArgumentException()
    {
        var game = Game.CreateNew(3, 3);
        game.MakeMove((0,0), TicTacToeSymbol.X, AlwaysNonFlipRandomMock.Object);
        game.MakeMove((0,1), TicTacToeSymbol.O, AlwaysNonFlipRandomMock.Object);
        game.MakeMove((1,0), TicTacToeSymbol.X, AlwaysNonFlipRandomMock.Object);
        game.MakeMove((1,1), TicTacToeSymbol.O, AlwaysNonFlipRandomMock.Object);
        game.MakeMove((2,0), TicTacToeSymbol.X, AlwaysNonFlipRandomMock.Object);

        var ex = Assert.Throws<ArgumentException>(() => game.MakeMove((2, 1), TicTacToeSymbol.O, AlwaysNonFlipRandomMock.Object));
        Assert.StartsWith("The Game has been completed at", ex.Message);
    }

    [Fact]
    public void MakeMove_InvalidCoordinates_ThrowsArgumentException()
    {
        var game = Game.CreateNew(3,3);
        var pos = (3, 0);
        var symbol = TicTacToeSymbol.X;

        var ex = Assert.Throws<ArgumentException>(() => game.MakeMove(pos, symbol, AlwaysNonFlipRandomMock.Object));
        Assert.Equal("Coordinates are out of board range.", ex.Message);
    }

    [Fact]
    public void MakeMove_ThirdMoveWithFlip_ChangesSymbol()
    {
        Console.WriteLine("Testing third move with flip...");
        var game = Game.CreateNew(3,3);
        var randomMock = new Mock<Random>();
        randomMock.Setup(x => x.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(0);
        var mock = new Mock<IRandomProvider>();
        mock.Setup(x=>x.Random).Returns(randomMock.Object);

        // First two moves
        game.MakeMove((0,0),TicTacToeSymbol.X, AlwaysNonFlipRandomMock.Object);
        game.MakeMove((0, 1), TicTacToeSymbol.O, AlwaysNonFlipRandomMock.Object);

        // Third move (should flip X to O)
        var flipPos = (1, 0);
        var flipSymbol = TicTacToeSymbol.X;
        var oppositeSymbol = TicTacToeSymbol.O;
        game.MakeMove(flipPos, flipSymbol, mock.Object);

        Assert.Equal(oppositeSymbol, game.Board[1 * 3 + 0]);
    }
}