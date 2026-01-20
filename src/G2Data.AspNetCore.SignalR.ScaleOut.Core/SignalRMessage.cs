namespace G2Data.AspNetCore.SignalR.ScaleOut.Core;

public sealed class SignalRMessage
{
    public SignalRMessageScope Scope { get; set; } = default!;

    public string Method { get; set; } = default!;

    public object[] Arguments { get; set; } = default!;

    public string[]? Targets { get; set; }

    public string[]? ExcludedConnectionIds { get; set; }

    public string SenderId { get; set; } = default!;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
