using Microsoft.Extensions.Logging;

namespace G2Data.AspNetCore.SignalR.ScaleOut.Core.MongoDB;

internal static partial class MongoDBBackplaneLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Acquiring MongoDB database instance.")]
    public static partial void AquiringDB(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "MongoDB database instance {database} acquired.")]
    public static partial void DBAcquired(ILogger logger, string database);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Initializing collection {collectionName}.")]
    public static partial void InitializingCollection(ILogger logger, string collectionName);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Collection {collectionName} initialized.")]
    public static partial void CollectionInitialized(ILogger logger, string collectionName);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Creating change stream on collection {collectionName}.")]
    public static partial void CreatingChangeStream(ILogger logger, string collectionName);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Change stream on collection {collectionName} created.")]
    public static partial void ChangeStreamCreated(ILogger logger, string collectionName);

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Message received on watch stream, scope: {scope}, method: {method}, senderId: {senderId}, sendAt: {sentAt}.")]
    public static partial void ChangeStreamMessageReceived(ILogger logger, SignalRMessageScope scope, string method, string senderId, DateTime sentAt);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Publishing message, scope: {scope}, method: {method}, senderId: {senderId}, sendAt: {sentAt}.")]
    public static partial void PublishingMessage(ILogger logger, SignalRMessageScope scope, string method, string senderId, DateTime sentAt);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "An error occurred while subscribing to MongoDB change stream, retrying in {reconnectDelayInSeconds} seconds...")]
    public static partial void CreateChangeStreamFailed(ILogger logger, int reconnectDelayInSeconds, Exception ex);

    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Message published, scope: {scope}, method: {method}, senderId: {senderId}, sendAt: {sentAt}.")]
    public static partial void MessagePublished(ILogger logger, SignalRMessageScope scope, string method, string senderId, DateTime sentAt);
}
