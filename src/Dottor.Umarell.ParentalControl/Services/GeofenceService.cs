namespace Dottor.Umarell.ParentalControl.Services;

using Azure.Messaging.ServiceBus;
using Dottor.Umarell.ParentalControl.Client.Models;
using Dottor.Umarell.ParentalControl.Client.Services;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text.Json;

public class GeofenceService : IGeofenceService, IAsyncDisposable
{

    public event Func<GeofenceEventArg, Task>? StateChanged;

    private ServiceBusClient    _client;
    private ServiceBusProcessor _processor;
    private readonly IConfiguration      _configuration;

    public GeofenceService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task StartMonitoringAsync()
    {
        string serviceBusConnectionString = _configuration.GetConnectionString("ServiceBusConnectionString");

        if (_processor is not null)
            await _processor.DisposeAsync();

        if (_client is not null)
            await _client.DisposeAsync();

        _client = new ServiceBusClient(serviceBusConnectionString);
        _processor = _client.CreateProcessor("42", new ServiceBusProcessorOptions());
        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;

        // start processing realtime data
        if (!_processor.IsProcessing)
            await _processor.StartProcessingAsync();
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        var body      = args.Message.Body.ToString();
        var telemetry = JsonSerializer.Deserialize<UmarellTelemetryData>(body);

        // complete the message. messages is deleted from the subscription. 
        await args.CompleteMessageAsync(args.Message);

        if(StateChanged is not null)
        await StateChanged.Invoke(new() { Data = telemetry });
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Debug.WriteLine(args.Exception.ToString());
        return Task.CompletedTask;
    }


    public async ValueTask DisposeAsync()
    {
        await _processor.DisposeAsync();
        await _client.DisposeAsync();
    }
}
