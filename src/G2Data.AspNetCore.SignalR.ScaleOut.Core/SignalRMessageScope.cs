namespace G2Data.AspNetCore.SignalR.ScaleOut.Core;

public enum SignalRMessageScope
{
    All,
    AllExcept,
    Connections,
    Groups,
    GroupExcept,
    Users
}
