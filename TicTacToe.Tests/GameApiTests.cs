using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mongo2Go;
using System.Net;
using System.Net.Http.Json;
using TicTacToe.Domain;
using TicTacToe.Services;
using Xunit;

namespace TicTacToe.Tests;

public class GameApiTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;

    public GameApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateGame_WithValidBoardSize_ReturnsCreated()
    {
        // Act
        var response = await _client.PostAsync("/games", new StringContent(""));
        var game = await response.Content.ReadFromJsonAsync<GameDto>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(game);
        Assert.NotEqual(Guid.Empty, game!.Id);
        Assert.Equal(3, game.Board.Length);
        foreach (var row in game.Board)
        {
            Assert.Equal(3, row.Length);
        }
        Assert.Equal('X', game.PlayerTurn);
        Assert.Null(game.Winner);
        Assert.Equal($"/games/{game.Id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GetGame_WithValidId_ReturnsGame()
    {
        // Arrange
        var createResponse = await _client.PostAsync("/games", new StringContent(""));
        var createdGame = await createResponse.Content.ReadFromJsonAsync<GameDto>();

        // Act
        var response = await _client.GetAsync($"/games/{createdGame!.Id}");
        var game = await response.Content.ReadFromJsonAsync<GameDto>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(game);
        Assert.Equal(createdGame!.Id, game!.Id);
        Assert.Equal(createdGame.Board, game.Board);
        Assert.Equal(createdGame.PlayerTurn, game.PlayerTurn);
    }

    [Fact]
    public async Task GetGame_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/games/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MakeMove_WithValidMove_ReturnsOk()
    {
        // Arrange
        var createResponse = await _client.PostAsync("/games", new StringContent(""));
        var game = await createResponse.Content.ReadFromJsonAsync<GameDto>();
        var move = new MakeMove
        {
            GameId = game!.Id,
            X = 0,
            Y = 0,
            Symbol = 'X'
        };

        // Act
        var response = await _client.PostAsJsonAsync("/moves", move);
        var updatedGame = await response.Content.ReadFromJsonAsync<GameDto>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updatedGame);
        Assert.Equal('X', updatedGame!.Board[0][0]);
        Assert.Equal('O', updatedGame.PlayerTurn);
        Assert.Contains(updatedGame.ModifiedAt.ToString(), response.Headers.GetValues("ETag"));
    }

    [Fact]
    public async Task MakeMove_WithInvalidSymbol_ReturnsBadRequest()
    {
        // Arrange
        var createResponse = await _client.PostAsync("/games", new StringContent(""));
        var game = await createResponse.Content.ReadFromJsonAsync<GameDto>();
        var move = new MakeMove
        {
            GameId = game!.Id,
            X = 0,
            Y = 0,
            Symbol = 'Z'
        };

        // Act
        var response = await _client.PostAsJsonAsync("/moves", move);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("Symbol is not recognized.", error!.Error);
    }

    [Fact]
    public async Task MakeMove_WithInvalidCoordinates_ReturnsBadRequest()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/games", 3);
        var game = await createResponse.Content.ReadFromJsonAsync<GameDto>();
        var move = new MakeMove
        {
            GameId = game!.Id,
            X = 3,
            Y = 0,
            Symbol = 'X'
        };

        // Act
        var response = await _client.PostAsJsonAsync("/moves", move);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("Coordinates are out of board range.", error!.Error);
    }

    [Fact]
    public async Task MakeMove_OnCompletedGame_ReturnsBadRequest()
    {
        // Arrange
        var createResponse = await _client.PostAsync("/games", new StringContent(""));
        var game = await createResponse.Content.ReadFromJsonAsync<GameDto>();
        // Simulate a winning condition (e.g., X wins in a row)
        var moves = new[]
        {
            new MakeMove { GameId = game!.Id, X = 0, Y = 0, Symbol = 'X' },
            new MakeMove { GameId = game!.Id, X = 1, Y = 0, Symbol = 'O' },
            new MakeMove { GameId = game!.Id, X = 0, Y = 1, Symbol = 'X' },
            new MakeMove { GameId = game!.Id, X = 1, Y = 1, Symbol = 'O' },
            new MakeMove { GameId = game!.Id, X = 0, Y = 2, Symbol = 'X' } 
            // X wins
        };

        foreach (var move in moves)
        {
            await _client.PostAsJsonAsync("/moves", move);
        }

        // Act
        var response = await _client.PostAsJsonAsync("/moves", new MakeMove
        {
            GameId = game!.Id,
            X = 2,
            Y = 0,
            Symbol = 'O'
        });
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.StartsWith("The Game has been completed at", error!.Error);
    }

    [Fact]
    public async Task MakeMove_WithWrongPlayer_ReturnsBadRequest()
    {
        // Arrange
        var createResponse = await _client.PostAsync("/games", new StringContent(""));
        var game = await createResponse.Content.ReadFromJsonAsync<GameDto>();
        var move = new MakeMove
        {
            GameId = game!.Id,
            X = 0,
            Y = 0,
            Symbol = 'O' 
            // Wrong player (should be X)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/moves", move);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("X has turn now.", error!.Error);
    }

    [Fact]
    public async Task MakeMove_ResultingInDraw_ReturnsOkWithNoWinner()
    {
        // Arrange
        var createResponse = await _client.PostAsync("/games", new StringContent(""));
        var game = await createResponse.Content.ReadFromJsonAsync<GameDto>();
        var moves = new[]
        {
            new MakeMove { GameId = game!.Id, X = 0, Y = 0, Symbol = 'X' },
            new MakeMove { GameId = game!.Id, X = 0, Y = 1, Symbol = 'O' },
            new MakeMove { GameId = game!.Id, X = 0, Y = 2, Symbol = 'X' },
            new MakeMove { GameId = game!.Id, X = 1, Y = 1, Symbol = 'O' },
            new MakeMove { GameId = game!.Id, X = 1, Y = 0, Symbol = 'X' },
            new MakeMove { GameId = game!.Id, X = 2, Y = 0, Symbol = 'O' },
            new MakeMove { GameId = game!.Id, X = 2, Y = 1, Symbol = 'X' },
            new MakeMove { GameId = game!.Id, X = 1, Y = 2, Symbol = 'O' },
            new MakeMove { GameId = game!.Id, X = 2, Y = 2, Symbol = 'X' } 
            // Draw
        };

        foreach (var move in moves)
        {
            await _client.PostAsJsonAsync("/moves", move);
        }

        // Act
        var response = await _client.GetAsync($"/games/{game!.Id}");
        var updatedGame = await response.Content.ReadFromJsonAsync<GameDto>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updatedGame);
        Assert.Equal("draw", updatedGame!.Winner);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public MongoDbRunner MongoDbRunner { get; }

    public CustomWebApplicationFactory()
    {
        MongoDbRunner = MongoDbRunner.Start();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the default MongoDB context
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MongoDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory MongoDB context
            services.AddDbContext<MongoDbContext>(options =>
                options.UseMongoDB(MongoDbRunner.ConnectionString, "tictactoe"));

            services.AddSingleton<IRandomProvider, ConstantRandomProvider>();

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:MongoDB", MongoDbRunner.ConnectionString }
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            MongoDbRunner.Dispose();
        }
        base.Dispose(disposing);
    }
}

public record ErrorResponse
{
    public string Error { get; init; } = string.Empty;
}