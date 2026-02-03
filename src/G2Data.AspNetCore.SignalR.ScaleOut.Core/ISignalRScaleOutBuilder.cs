using Microsoft.AspNetCore.SignalR;

namespace G2Data.AspNetCore.SignalR.ScaleOut.Core
{
    public interface ISignalRScaleOutBuilder
    {
        ISignalRServerBuilder SignalRBuilder { get; init; }
    }
}
