namespace Dottor.Umarell.ParentalControl.Services;

using Azure.Messaging.ServiceBus;
using Dottor.Umarell.ParentalControl.Client.Models;
using Dottor.Umarell.ParentalControl.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class GeofenceWorker : BackgroundService
{
    private ServiceBusClient                      _client;
    private ServiceBusProcessor                   _processor;
    private readonly IConfiguration               _configuration;
    private readonly IHubContext<NotificationHub> _hubContext;

    public GeofenceWorker(IConfiguration configuration, IHubContext<NotificationHub> hubContext)
    {
        _configuration = configuration;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string serviceBusConnectionString = _configuration.GetConnectionString("ServiceBusConnectionString");

        if (_processor is not null)
            await _processor.DisposeAsync();

        if (_client is not null)
            await _client.DisposeAsync();

        _client = new ServiceBusClient(serviceBusConnectionString);
        _processor = _client.CreateProcessor("42", "InteractiveWebAssembly", new ServiceBusProcessorOptions());
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

        await _hubContext.Clients.All.SendAsync("OutOfZone", telemetry);
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
