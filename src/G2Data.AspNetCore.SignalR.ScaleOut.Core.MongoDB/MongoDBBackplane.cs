using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace G2Data.AspNetCore.SignalR.ScaleOut.Core.MongoDB;

internal sealed class MongoDBBackplane(IServiceProvider serviceProvider, MongoDBOptions mongoDBOptions) : ISignalRBackplane
{
    private const string _defaultCollectionName = "SignalR.ScaleOut.Messages";

    private IMongoDatabase? _db;
    private IMongoCollection<SignalRMessageDO>? _collection;
    private readonly ILogger<MongoDBBackplane> _logger = serviceProvider.GetRequiredService<ILogger<MongoDBBackplane>>();
    private bool _isConnected;

    public async Task PublishAsync(SignalRMessage message, CancellationToken cancellationToken)
    {
        if (_isConnected && _collection != null)
        {
            MongoDBBackplaneLog.PublishingMessage(_logger
                , message.Scope
                , message.Method
                , message.SenderId
                , message.SentAt);
            var msg = SignalRMessageDO.FromSignalRMessage(message);
            await _collection.InsertOneAsync(msg, null, cancellationToken).ConfigureAwait(false);
            MongoDBBackplaneLog.MessagePublished(_logger
                , message.Scope
                , message.Method
                , message.SenderId
                , message.SentAt);
        }
    }

    public async Task SubscribeAsync(Func<SignalRMessage, Task> onMessageReceived, CancellationToken cancellationToken)
    {
        var reconnect = true;
        while (reconnect && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var collectionName = mongoDBOptions.CollectionName ?? _defaultCollectionName;

                MongoDBBackplaneLog.AquiringDB(_logger);
                _db = mongoDBOptions.GetDbDelegate!(serviceProvider);
                MongoDBBackplaneLog.DBAcquired(_logger, _db.DatabaseNamespace.DatabaseName);

                MongoDBBackplaneLog.InitializingCollection(_logger, collectionName);
                MongoDBSetup.Init(_db, collectionName);
                MongoDBBackplaneLog.CollectionInitialized(_logger, collectionName);

                _collection = _db.GetCollection<SignalRMessageDO>(collectionName);

                MongoDBBackplaneLog.CreatingChangeStream(_logger, collectionName);
                var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<SignalRMessageDO>>()
                    .Match(x => x.OperationType == ChangeStreamOperationType.Insert);
                using var stream = await _collection.WatchAsync(pipeline, cancellationToken: cancellationToken).ConfigureAwait(false);
                _isConnected = true;
                MongoDBBackplaneLog.ChangeStreamCreated(_logger, collectionName);
                while (await stream.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    foreach (var change in stream.Current)
                    {
                        MongoDBBackplaneLog.ChangeStreamMessageReceived(_logger
                            , change.FullDocument.Scope
                            , change.FullDocument.Method
                            , change.FullDocument.SenderId
                            , change.FullDocument.SentAt);
                        var message = change.FullDocument.ToSignalRMessage();
                        await onMessageReceived(message).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _isConnected = false;
                reconnect = false;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                MongoDBBackplaneLog.CreateChangeStreamFailed(_logger, mongoDBOptions.ReconnectDelayInSeconds, ex);
                reconnect = true;
                await Task.Delay(TimeSpan.FromSeconds(mongoDBOptions.ReconnectDelayInSeconds), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
