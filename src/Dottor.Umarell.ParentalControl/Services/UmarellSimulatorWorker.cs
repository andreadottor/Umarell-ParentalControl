namespace Dottor.Umarell.ParentalControl.Services;

using System.Globalization;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Azure.Messaging.WebPubSub;
using Azure.Core;
using Dottor.Umarell.ParentalControl.Client.Models;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;

public class UmarellSimulatorWorker : BackgroundService
{
    private readonly WebPubSubServiceClient           _pubSubClient;
    private readonly ServiceBusClient                 _serviceBusClient;
    private readonly ServiceBusSender                 _serviceBusSender;
    private readonly ManagementClient                 _managementClient;
    private readonly ILogger<UmarellSimulatorWorker>  _logger;
    private readonly string                           _topicName = "42";

    private bool _outOfZoneSent = false;

    public UmarellSimulatorWorker(IConfiguration configuration, ILogger<UmarellSimulatorWorker> logger)
    {
        _logger = logger;
        string? webPubSubConnectionString  = configuration.GetConnectionString("WebPubSubConnectionString");
        string? serviceBusConnectionString = configuration.GetConnectionString("ServiceBusConnectionString");
        ArgumentNullException.ThrowIfNullOrWhiteSpace(webPubSubConnectionString, nameof(webPubSubConnectionString));
        ArgumentNullException.ThrowIfNullOrWhiteSpace(serviceBusConnectionString, nameof(serviceBusConnectionString));

        _pubSubClient     = new WebPubSubServiceClient(webPubSubConnectionString, "hub");
        _managementClient = new ManagementClient(serviceBusConnectionString);

        _serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        _serviceBusSender = _serviceBusClient.CreateSender(_topicName);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // HACK: delete all Service Bus Subscriptions at startup
        var subscriptions = await _managementClient.GetSubscriptionsAsync(_topicName);
        foreach (var item in subscriptions)
        {
            await _managementClient.DeleteSubscriptionAsync(_topicName, item.SubscriptionName, stoppingToken);
        }

        await SimulateGpxRouteAsync(stoppingToken);
    }

    private async Task SimulateGpxRouteAsync(CancellationToken stoppingToken)
    {
        var gpx = await File.ReadAllTextAsync("Route.gpx", stoppingToken);
        var gpxDoc = XDocument.Parse(gpx);
        var gpxNs = gpxDoc.Root.GetDefaultNamespace();
        var points = gpxDoc.Descendants(gpxNs + "trkpt").ToList();

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var point in points)
            {
                var lat = double.Parse(point.Attribute("lat").Value, CultureInfo.InvariantCulture);
                var lon = double.Parse(point.Attribute("lon").Value, CultureInfo.InvariantCulture);

                var data = new UmarellTelemetryData
                {
                    Date = DateTime.UtcNow,
                    Latitude = lat,
                    Longitude = lon
                };
                await SendTelemetryDataAsync(data, stoppingToken);

                // TODO: da verificare la posizione
                var distance = GetDistance(41.884347835000028, 12.488813031000063, lat, lon);
                if(distance > 700) 
                    await SendUmarellGeofenceWarning(data, stoppingToken);

                await Task.Delay(3000, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;
            }
        }
    }

    /// <summary>
    /// Send message with Azure Web PubSub
    /// </summary>
    private async Task SendTelemetryDataAsync(UmarellTelemetryData data, CancellationToken stoppingToken)
    {
        var json    = JsonSerializer.Serialize(data);
        var request = RequestContent.Create(json);
        await _pubSubClient.SendToUserAsync(_topicName, request, ContentType.ApplicationJson);

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("SendTelemetryDataAsync: {data}", data);
    }

    /// <summary>
    /// Send event with Azure Service Bus
    /// </summary>
    private async Task SendUmarellGeofenceWarning(UmarellTelemetryData data, CancellationToken stoppingToken)
    {
        //if (_outOfZoneSent) return;
        var json    = JsonSerializer.Serialize(data);
        var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(json));
        await _serviceBusSender.SendMessageAsync(message, stoppingToken);
        _outOfZoneSent = true;

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("SendUmarellGeofenceWarning: {data}", data);
    }

    /// <summary>
    /// Calculate distance from two coordinates
    /// </summary>
    private double GetDistance(double startLat, double startLon, double endLat, double endLon)
    {
        var d1   = startLat * (Math.PI / 180.0);
        var num1 = startLon * (Math.PI / 180.0);
        var d2   = endLat * (Math.PI / 180.0);
        var num2 = endLon * (Math.PI / 180.0) - num1;
        var d3   = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);
        return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));

    }
}
