using System.Text.Json;

namespace G2Data.AspNetCore.SignalR.ScaleOut.Core.MongoDB;

internal sealed class SignalRMessageDO
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public required Guid Id { get; set; }

    public SignalRMessageScope Scope { get; set; } = default!;

    public string Method { get; set; } = default!;

    public string Payload { get; set; } = default!;

    public string[]? Targets { get; set; }

    public string[]? ExcludedConnectionIds { get; set; }

    public string SenderId { get; set; } = default!;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public SignalRMessage ToSignalRMessage()
    {
        var arguments = JsonSerializer.Deserialize<object[]>(Payload, _serializerOptions);
        return new SignalRMessage
        {
            Id = Guid.NewGuid(),
            Scope = Scope,
            Method = Method,
            Arguments = arguments!,
            SenderId = SenderId,
            SentAt = SentAt
        };
    }

    public static SignalRMessageDO FromSignalRMessage(SignalRMessage message)
    {
        var payload = JsonSerializer.Serialize(message.Arguments, _serializerOptions);
        return new SignalRMessageDO
        {
            Id = Guid.NewGuid(),
            Scope = message.Scope,
            Method = message.Method,
            Payload = payload,
            SenderId = message.SenderId,
            SentAt = message.SentAt
        };
    }
}
