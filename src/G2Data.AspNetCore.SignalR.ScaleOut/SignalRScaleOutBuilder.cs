using G2Data.AspNetCore.SignalR.ScaleOut.Core;
using Microsoft.AspNetCore.SignalR;

namespace G2Data.AspNetCore.SignalR.ScaleOut
{
    internal class SignalRScaleOutBuilder : ISignalRScaleOutBuilder
    {
        public required ISignalRServerBuilder SignalRBuilder { get; init; }
    }
}
