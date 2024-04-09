namespace Dottor.Umarell.ParentalControl.Client.Services;

using Dottor.Umarell.ParentalControl.Client.Models;
using System;
using System.Threading.Tasks;

public class GeofenceRemoteService : IGeofenceService
{
    public event Func<GeofenceEventArg, Task>? StateChanged;

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public Task StartMonitoringAsync()
    {
        return Task.CompletedTask;
    }
}
