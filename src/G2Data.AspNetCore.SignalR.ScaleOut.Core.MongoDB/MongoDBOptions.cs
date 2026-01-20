using MongoDB.Driver;

namespace G2Data.AspNetCore.SignalR.ScaleOut.Core.MongoDB;

public sealed class MongoDBOptions
{
    public Func<IServiceProvider, IMongoDatabase>? GetDbDelegate { get; set; }

    public string? CollectionName { get; set; }

    public int ReconnectDelayInSeconds { get; set; } = 10;
}
