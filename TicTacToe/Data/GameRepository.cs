using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace TicTacToe.Domain;

public class GameRepository : IGameRepository
{
    private MongoDbContext dbContext;

    public GameRepository(MongoDbContext db)
    {
        dbContext = db;
    }

    public async Task AddAsync(Game game)
    {
        await dbContext.Games.AddAsync(game);

        await dbContext.SaveChangesAsync();
    }

    public Task<Game> FindAsync(Guid id)
    {
        return dbContext.Games.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task UpdateAsync(Game game)
    {
        dbContext.Games.Update(game);
        return dbContext.SaveChangesAsync();
    }
}