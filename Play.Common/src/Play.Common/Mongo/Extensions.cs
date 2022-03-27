using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Play.Common.Entities.Abstractions;
using Play.Common.Mongo.Repositories;
using Play.Common.Mongo.Settings;
using Play.Common.Repositories.Abstractions;

namespace Play.Common.Mongo;

public static class Extensions
{
    public static IServiceCollection AddMongo(this IServiceCollection services, IConfiguration configuration,
        string databaseName)
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
        BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));
        
        services.AddSingleton(sp =>
        {
            var mongoDbSettings = configuration.GetSection(nameof(MongoSettings)).Get<MongoSettings>();
            var mongoClient = new MongoClient(mongoDbSettings.ConnectionString);
            var database = mongoClient.GetDatabase(databaseName);

            return database;
        });


        return services;
    }

    public static IServiceCollection AddMongoRepository<TEntity>(this IServiceCollection services, string collectionName)
        where TEntity : class, IEntity
    {
        services.AddSingleton<IRepository<TEntity>>(sp =>
        {
            var database = sp.GetRequiredService<IMongoDatabase>();
            return new MongoRepository<TEntity>(database, collectionName);
        });
        
        return services;
    }
}