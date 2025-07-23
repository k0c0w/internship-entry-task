using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicTacToe.Domain;
using TicTacToe.Services;
namespace TicTacToe;

public class Program
{
    public static void Main(params string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<MongoDbContext>(options =>
            options.UseMongoDB(builder.Configuration.GetConnectionString("MongoDB"), "tictactoe"));
        builder.Services.Configure<GameSettings>(gs =>
        {
            var boardSize = builder.Configuration.GetRequiredSection(nameof(GameSettings)).GetValue(nameof(GameSettings.BoardSize), 3);
            ArgumentOutOfRangeException.ThrowIfLessThan(boardSize, 3);
            var winCondition = builder.Configuration.GetRequiredSection(nameof(GameSettings)).GetValue(nameof(GameSettings.WinCondition), 3);
            ArgumentOutOfRangeException.ThrowIfLessThan(winCondition, 1);

            gs.BoardSize = boardSize;
            gs.WinCondition = winCondition;
        });

        builder.Services.AddSwaggerGen();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddScoped<IGameManager, GameService>();

        builder.Services.AddScoped<IGameRepository, GameRepository>();
        builder.Services.AddSingleton<IRandomProvider, SharedRandomProvider>();

        builder.Services.AddProblemDetails();

        var app = builder.Build();


        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid request",
                    Detail = "An error occurred while processing your request."
                };
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(problemDetails);
            });
        });
        app.MapSwagger();


        app.MapGet("/health", () => Results.Ok()).WithName("Health check");

        app.MapPost("/games", async ([FromServices] IGameManager service) =>
        {
            var game = await service.CreateNewGameAsync();

            return Results.Created($"/games/{game.Id}", game);
        });

        app.MapGet("/games/{id:guid}", async ([FromRoute] Guid id, IGameManager service) =>
        {
            var gameFindResult = await service.FindGameAsync(id);
            if (gameFindResult.IsSuccess)
            {
                return Results.Ok(gameFindResult.Value);
            }
            else if (gameFindResult.Error.Type == Error.ErrorType.NotFoundError)
            {
                return Results.NotFound();
            }
            else if (gameFindResult.Error.Type == Error.ErrorType.ValidationError)
            {
                return Results.BadRequest(new { error = gameFindResult.Error.Message });
            }

            return Results.InternalServerError();
        }).Produces(200).Produces(404).Produces(400).Produces(500);


        app.MapPost("/moves", async ([FromBody] MakeMove move, [FromServices] IGameManager service, HttpContext ctx) =>
        {
            var makeMoveResult = await service.MakeMoveAsync(move);

            if (makeMoveResult.IsSuccess)
            {
                var game = makeMoveResult.Value;
                ctx.Response.Headers.ETag = game.ModifiedAt.ToString();
                return Results.Ok(game);
            }
            else if (makeMoveResult.Error.Type == Error.ErrorType.NotFoundError)
            {
                return Results.NotFound();
            }
            else if (makeMoveResult.Error.Type == Error.ErrorType.ValidationError)
            {
                return Results.BadRequest(new { error = makeMoveResult.Error.Message });
            }

            return Results.InternalServerError();
                }).Produces(200).Produces(404).Produces(400).Produces(500);

        app.Run();
    }
}
