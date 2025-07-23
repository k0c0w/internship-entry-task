using System;
using System.Threading.Tasks;

namespace TicTacToe.Domain;

public interface IGameRepository
{
    Task AddAsync(Game game);

    Task<Game?> FindAsync(Guid id);

    Task UpdateAsync(Game game);
}