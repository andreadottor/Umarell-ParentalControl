namespace Dottor.Umarell.ParentalControl.Services;

using Azure.Messaging.ServiceBus;
using Dottor.Umarell.ParentalControl.Client.Models;
using Dottor.Umarell.ParentalControl.Client.Services;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text.Json;

public class GeofenceService : IGeofenceService, IAsyncDisposable
{

    public event Func<GeofenceEventArg, Task>? OutOfZone;

    private ServiceBusClient                  _client;
    private ManagementClient                  _managementClient;
    private ServiceBusProcessor               _processor;

    private readonly ILogger<GeofenceService> _logger;
    private readonly IConfiguration           _configuration;
    private readonly string                   _subscriptionName;
    private readonly string                   _topicName = "42";

    public GeofenceService(IConfiguration configuration, ILogger<GeofenceService> logger)
    {
        _configuration = configuration;
        _subscriptionName = Guid.NewGuid().ToString();
        _logger = logger;
    }

    public async Task StartMonitoringAsync()
    {
        string? serviceBusConnectionString = _configuration.GetConnectionString("ServiceBusConnectionString");
        ArgumentNullException.ThrowIfNullOrWhiteSpace(serviceBusConnectionString, nameof(serviceBusConnectionString));

        if (_processor is not null)
            await _processor.DisposeAsync();

        if (_client is not null)
            await _client.DisposeAsync();

        // create a new subscription for current client
        if (_managementClient is null)
        {
            _managementClient = new ManagementClient(serviceBusConnectionString);

            if (!await _managementClient.SubscriptionExistsAsync(_topicName, _subscriptionName))
            {
                var subscription  = new SubscriptionDescription(_topicName, _subscriptionName);
                subscription.DefaultMessageTimeToLive = TimeSpan.FromMinutes(5);
                await _managementClient.CreateSubscriptionAsync(subscription);
            }
        }

        _client = new ServiceBusClient(serviceBusConnectionString);
        _processor = _client.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions());
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

        if (OutOfZone is not null)
            await OutOfZone.Invoke(new() { Data = telemetry });
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        if(_logger.IsEnabled(LogLevel.Error))
            _logger.LogError(args.Exception, "Error on receive telemetry data from ServiceBus.");

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if(_managementClient is not null)
            await _managementClient.DeleteSubscriptionAsync(_topicName, _subscriptionName);

        await _processor.DisposeAsync();
        await _client.DisposeAsync();
    }
}
