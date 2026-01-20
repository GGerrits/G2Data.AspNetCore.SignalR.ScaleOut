using G2Data.AspNetCore.SignalR.ScaleOut.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace G2Data.AspNetCore.SignalR.ScaleOut;

public sealed class SignalRHubLifeTimeManager<THub> : HubLifetimeManager<THub> where THub : Hub
{
    public SignalRHubLifeTimeManager(ISignalRBackplane backplane, IHostApplicationLifetime lifetime)
    {
        _backplane = backplane;
        _ = Task.Run(ListenAsync);
        lifetime.ApplicationStopping.Register(_cts.Cancel);
    }

    private readonly ISignalRBackplane _backplane;

    private readonly string _serverId = Guid.NewGuid().ToString();

    private readonly CancellationTokenSource _cts = new();

    private readonly ConcurrentDictionary<string, HubConnectionContext> _connections = new();

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _groups = new();

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _users = new();

    public override Task OnConnectedAsync(HubConnectionContext connection)
    {
        _connections[connection.ConnectionId] = connection;
        if (connection.UserIdentifier is not null)
        {
            var connections = _users.GetOrAdd(connection.UserIdentifier, _ => new ConcurrentDictionary<string, byte>());
            connections[connection.ConnectionId] = 0;
        }
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(HubConnectionContext connection)
    {
        _connections.TryRemove(connection.ConnectionId, out _);
        foreach (var group in _groups.Values)
        {
            group.TryRemove(connection.ConnectionId, out _);
        }

        if (connection.UserIdentifier is not null &&
            _users.TryGetValue(connection.UserIdentifier, out var connections))
        {
            connections.TryRemove(connection.ConnectionId, out _);
            if (connections.IsEmpty)
            {
                _users.TryRemove(connection.UserIdentifier, out _);
            }
        }
        return Task.CompletedTask;
    }

    public override Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken)
    {
        var group = _groups.GetOrAdd(groupName, _ => new ConcurrentDictionary<string, byte>());
        group[connectionId] = 0;
        return Task.CompletedTask;
    }

    public override Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken)
    {
        if (_groups.TryGetValue(groupName, out var group))
        {
            group.TryRemove(connectionId, out _);
            if (group.IsEmpty)
            {
                _groups.TryRemove(groupName, out _);
            }
        }
        return Task.CompletedTask;
    }

    private async Task SendToConnections(IEnumerable<string> connectionIds, string method, object[] args, IReadOnlySet<string>? exclude = null)
    {
        var tasks = new List<Task>();
        foreach (var id in connectionIds)
        {
            if (exclude?.Contains(id) == true)
            {
                continue;
            }
            if (!_connections.TryGetValue(id, out var conn))
            {
                continue;
            }
            var invocationMessage = new InvocationMessage(method, args);
            var serialized = new SerializedHubMessage(invocationMessage);
            var task = conn.WriteAsync(serialized).AsTask();
            tasks.Add(task);
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public override Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken)
        => Publish(SignalRMessageScope.All, methodName, args, null, null, cancellationToken);

    public override Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken)
        => Publish(SignalRMessageScope.AllExcept, methodName, args, null, excludedConnectionIds, cancellationToken);

    public override Task SendConnectionAsync(string connectionId, string methodName, object[] args, CancellationToken cancellationToken)
        => Publish(SignalRMessageScope.Connections, methodName, args, [connectionId], null, cancellationToken);

    public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args, CancellationToken cancellationToken)
        => Publish(SignalRMessageScope.Connections, methodName, args, connectionIds, null, cancellationToken);

    public override Task SendGroupAsync(string groupName, string methodName, object[] args, CancellationToken cancellationToken)
        => Publish(SignalRMessageScope.Groups, methodName, args, [groupName], null, cancellationToken);

    public override Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken)
        => Publish(SignalRMessageScope.GroupExcept, methodName, args, [groupName], excludedConnectionIds, cancellationToken);

    public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args, CancellationToken cancellationToken)
        => Publish(SignalRMessageScope.Groups, methodName, args, groupNames, null, cancellationToken);

    public override Task SendUserAsync(string userId, string methodName, object[] args, CancellationToken cancellationToken)
        => Publish(SignalRMessageScope.Users, methodName, args, [userId], null, cancellationToken);

    public override Task SendUsersAsync( IReadOnlyList<string> userIds, string methodName, object[] args, CancellationToken cancellationToken)
        => Publish(SignalRMessageScope.Users, methodName, args, userIds, null, cancellationToken);

    private Task Publish(SignalRMessageScope scope, string method, object[] args, IEnumerable<string>? targets, IReadOnlyList<string>? excluded, CancellationToken ct)
    {
        return _backplane.PublishAsync(new SignalRMessage
        {
            Scope = scope,
            Method = method,
            Arguments = args,
            Targets = targets?.ToArray(),
            ExcludedConnectionIds = excluded?.ToArray(),
            SenderId = _serverId
        }, ct);
    }

    private async Task ListenAsync()
    {
        await _backplane.SubscribeAsync(SendMessage, _cts.Token).ConfigureAwait(false);
    }

    private async Task SendMessage(SignalRMessage message)
    {
        switch (message.Scope)
        {
            case SignalRMessageScope.All:
                await SendToConnections(_connections.Keys, message.Method, message.Arguments).ConfigureAwait(false);
                break;
            case SignalRMessageScope.AllExcept:
                await SendToConnections(_connections.Keys, message.Method, message.Arguments, message.ExcludedConnectionIds!.ToHashSet()).ConfigureAwait(false);
                break;
            case SignalRMessageScope.Connections:
                await SendToConnections(message.Targets!, message.Method, message.Arguments).ConfigureAwait(false);
                break;
            case SignalRMessageScope.Groups:
                {
                    foreach (var group in message.Targets!)
                    {
                        if (_groups.TryGetValue(group, out var connections))
                        {
                            await SendToConnections(connections.Keys, message.Method, message.Arguments).ConfigureAwait(false);
                        }
                    }
                }
                break;
            case SignalRMessageScope.GroupExcept:
                {
                    var g = message.Targets![0];
                    if (_groups.TryGetValue(g, out var connections))
                    {
                        await SendToConnections(connections.Keys, message.Method, message.Arguments, message.ExcludedConnectionIds!.ToHashSet()).ConfigureAwait(false);
                    }
                }
                break;
            case SignalRMessageScope.Users:
                foreach (var user in message.Targets!)
                {
                    if (_users.TryGetValue(user, out var conns))
                    {
                        await SendToConnections(conns.Keys, message.Method, message.Arguments).ConfigureAwait(false);
                    }
                }
                break;
        }
    }
}
