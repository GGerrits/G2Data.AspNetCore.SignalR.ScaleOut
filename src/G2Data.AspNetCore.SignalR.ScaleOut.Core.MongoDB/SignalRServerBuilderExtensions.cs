using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace G2Data.AspNetCore.SignalR.ScaleOut.Core.MongoDB;

public static class SignalRServerBuilderExtensions
{
    public static ISignalRServerBuilder AddMongoDB(this ISignalRServerBuilder builder, Action<MongoDBOptions> configure)
    {
        var options = new MongoDBOptions();
        configure(options);
        ValidateMongoDBOptions(options);
        builder.Services.AddSingleton<ISignalRBackplane>(sp => InitBackplane(sp, options));
        builder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(SignalRHubLifeTimeManager<>));
        return builder;
    }

    private static MongoDBBackplane InitBackplane(IServiceProvider serviceProvider, MongoDBOptions options)
    {
        return new MongoDBBackplane(serviceProvider, options);
    }

    private static void ValidateMongoDBOptions(MongoDBOptions options)
    {
        var getDbDelegateName = nameof(options.GetDbDelegate);
        if (options.GetDbDelegate is null)
        {
            throw new ArgumentNullException(getDbDelegateName, "GetDbDelegate must be provided in MongoDBOptions.");
        }
        var reconnectDelayInSecondsName = nameof(options.ReconnectDelayInSeconds);
        if (options.ReconnectDelayInSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(reconnectDelayInSecondsName, "ReconnectDelayInSeconds must be greater than zero.");
        }
    }
}
