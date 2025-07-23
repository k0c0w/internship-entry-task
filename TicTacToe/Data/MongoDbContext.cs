using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.EntityFrameworkCore.Extensions;
using TicTacToe.Domain;

public class MongoDbContext : DbContext
{
    public DbSet<Game> Games { get; set; }

    public MongoDbContext(DbContextOptions<MongoDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Game>()
            .ToCollection("games")
            .HasKey(g => g.Id);

        BsonSerializer.RegisterSerializer(new EnumSerializer<TicTacToeSymbol>(BsonType.Int32));
    }
}