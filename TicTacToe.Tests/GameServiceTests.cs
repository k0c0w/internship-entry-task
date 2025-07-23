using Microsoft.Extensions.Options;
using Moq;
using TicTacToe.Domain;
using TicTacToe.Services;
using Xunit;

namespace TicTacToe.Tests;

public class GameServiceTests
{
    private readonly Mock<IGameRepository> _repositoryMock;
    private readonly Mock<IRandomProvider> _randomProviderMock;
    private readonly GameService _service;
    private readonly IOptionsMonitor<GameSettings> _gameSettings;

    public GameServiceTests()
    {
        _repositoryMock = new Mock<IGameRepository>();
        _randomProviderMock = new Mock<IRandomProvider>();
        _randomProviderMock.Setup(r => r.Random).Returns(new Random(0));
        _service = new GameService(_repositoryMock.Object, _randomProviderMock.Object, new GameSettingsFixture(new GameSettings
        {
            BoardSize = 3,
            WinCondition = 3
        }));
    }

    [Fact]
    public async Task CreateNewGameAsync_CreatesGameWithCorrectSize()
    {
        var boardSize = 3;

        var game = await _service.CreateNewGameAsync();

        Assert.Equal(boardSize, game.Board.GetLength(0));
        _repositoryMock.Verify(r => r.AddAsync(It.Is<Game>(g => g.Id == game.Id)), Times.Once());
    }

    [Fact]
    public async Task FindGameAsync_GameExists_ReturnsGame()
    {
        var game = Game.CreateNew(3, 3);
        _repositoryMock.Setup(r => r.FindAsync(game.Id)).ReturnsAsync(game);

        var result = await _service.FindGameAsync(game.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(game.Id, result.Value.Id);
    }

    [Fact]
    public async Task FindGameAsync_GameNotFound_ReturnsError()
    {
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.FindAsync(id)).ReturnsAsync((Game?)null);

        var result = await _service.FindGameAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal(Error.ErrorType.NotFoundError, result.Error.Type);
        Assert.Equal("Game not found.", result.Error.Message);
    }

    [Fact]
    public async Task MakeMoveAsync_ValidMove_UpdatesGame()
    {
        var game = Game.CreateNew(3, 3);
        var pos = (0, 0);
        _repositoryMock.Setup(r => r.FindAsync(game.Id)).ReturnsAsync(game);
        var makeMove = new MakeMove
        {
            X = pos.Item1,
            Y = pos.Item2,
            GameId = game.Id,
            Symbol = 'X'
        };

        var result = await _service.MakeMoveAsync(makeMove);

        Assert.True(result.IsSuccess);
        Assert.Equal('X', result.Value.Board[0][0]);
        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Game>(g => g.Id == game.Id)), Times.Once());
    }

    [Fact]
    public async Task MakeMoveAsync_DuplicateMove_ThrowsValidationError()
    {
        var game = Game.CreateNew(3, 3);
        _repositoryMock.Setup(r => r.FindAsync(game.Id)).ReturnsAsync(game);
        var movement = new MakeMove
        {
            X = 0,
            Y = 0,
            Symbol = 'X',
            GameId = game.Id,
        };

        // First move
        await _service.MakeMoveAsync(movement);

        // Second identical move
        var result = await _service.MakeMoveAsync(movement with { Symbol ='O' });

        Assert.False(result.IsSuccess);
        Assert.Equal(Error.ErrorType.ValidationError, result.Error.Type);
        Assert.Equal("Position (0, 0) is already placed.", result.Error.Message);
    }
}