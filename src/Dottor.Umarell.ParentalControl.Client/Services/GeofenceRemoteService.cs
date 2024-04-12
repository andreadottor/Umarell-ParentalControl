namespace Dottor.Umarell.ParentalControl.Client.Services;

using Dottor.Umarell.ParentalControl.Client.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

public class GeofenceRemoteService : IGeofenceService
{
    public event Func<GeofenceEventArg, Task>? OutOfZone;

    private readonly NavigationManager _navigationManager;
    private readonly HubConnection     _hubConnection;

    public GeofenceRemoteService(NavigationManager navigation)
    {
        _navigationManager = navigation;
        _hubConnection = new HubConnectionBuilder()
                            .WithUrl(_navigationManager.ToAbsoluteUri("/notification-hub"))
                            .Build();
    }

    public async Task StartMonitoringAsync()
    {
        _hubConnection.On<UmarellTelemetryData>("OutOfZone", async (telemetry) =>
        {
            if (OutOfZone is not null)
                await OutOfZone.Invoke(new() { Data = telemetry });
        });

        await _hubConnection.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if(_hubConnection is not null)
            await _hubConnection.DisposeAsync();
    }
}
