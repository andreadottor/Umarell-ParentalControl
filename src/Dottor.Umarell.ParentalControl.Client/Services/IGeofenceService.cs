namespace Dottor.Umarell.ParentalControl.Client.Services;

using Dottor.Umarell.ParentalControl.Client.Models;

public interface IGeofenceService : IAsyncDisposable
{
    event Func<GeofenceEventArg, Task>? StateChanged;

    Task StartMonitoringAsync();

}
