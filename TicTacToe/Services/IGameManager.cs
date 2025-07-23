using System;
using System.Threading.Tasks;
using ResultMonad;
using TicTacToe.Domain;

namespace TicTacToe.Services;

public interface IGameManager
{
    public Task<GameDto> CreateNewGameAsync();

    public Task<Result<GameDto, Error>> MakeMoveAsync(MakeMove move);

    public Task<Result<GameDto, Error>> FindGameAsync(Guid id);
}