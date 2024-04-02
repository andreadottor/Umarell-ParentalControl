namespace Dottor.Umarell.ParentalControl.Client.Services;

using Microsoft.FluentUI.AspNetCore.Components;

public class NotificationUIService
{
    private readonly IGeofenceService _geofenceService;
    private readonly IToastService _toastService;

    public NotificationUIService(IGeofenceService geofenceService, IToastService toastService)
    {
        _geofenceService = geofenceService;
        _toastService = toastService;
        
        _geofenceService.StateChanged += (e) =>
        {
            _toastService.ShowWarning("L'umarell è uscito dall'area specificata.");
            return Task.CompletedTask;
        };

        _ = _geofenceService.StartMonitoringAsync();
    }

    
}