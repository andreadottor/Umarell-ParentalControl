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

public class UmarellSimulatorService : BackgroundService
{

    private readonly IConfiguration configuration;
    private readonly WebPubSubServiceClient _serviceClient;
    private readonly ILogger<UmarellSimulatorService> _logger;

    public UmarellSimulatorService(IConfiguration configuration,
                                    ILogger<UmarellSimulatorService> logger)
    {
        this.configuration = configuration;
        string webPubSubConnectionString = configuration.GetConnectionString("WebPubSubConnectionString");
        ArgumentNullException.ThrowIfNullOrWhiteSpace(webPubSubConnectionString, nameof(webPubSubConnectionString));

        _serviceClient = new WebPubSubServiceClient(webPubSubConnectionString, "hub");
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
                await SendMessageAsync(data, stoppingToken);
                await Task.Delay(2000, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;
            }
        }
    }

    private async Task SendMessageAsync(UmarellTelemetryData data, CancellationToken stoppingToken)
    {
        var json = JsonSerializer.Serialize(data);

        var request = RequestContent.Create(json);
        await _serviceClient.SendToUserAsync("42", request, ContentType.ApplicationJson);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Message sent: {data}", data);
        }
    }
}
