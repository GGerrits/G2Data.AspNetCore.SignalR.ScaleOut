using G2Data.AspNetCore.SignalR.ScaleOut.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace G2Data.AspNetCore.SignalR.ScaleOut;

public static class SignalRServerBuilderExtensions
{
    public static ISignalRScaleOutBuilder AddScaleOut(this ISignalRServerBuilder builder)
    {
        builder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(ScaleOutHubLifeTimeManager<>));
        return new SignalRScaleOutBuilder { SignalRBuilder = builder };
    }
}
