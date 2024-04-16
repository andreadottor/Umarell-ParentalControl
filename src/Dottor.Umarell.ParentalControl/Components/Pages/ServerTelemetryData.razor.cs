namespace Dottor.Umarell.ParentalControl.Components.Pages;

using Azure.Messaging.WebPubSub.Clients;
using Azure.Messaging.WebPubSub;
using Dottor.Umarell.ParentalControl.Client.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public partial class ServerTelemetryData
{
    private WebPubSubClient?    _client;
    private string?             _text;
    private IJSObjectReference? _module;
    private IJSObjectReference? _polyline;
    private IJSObjectReference? _mapJs;
    private ElementReference?   _mapEl;


    protected override async Task OnInitializedAsync()
    {
        var cs            = Configuration.GetConnectionString("WebPubSubConnectionString") ?? throw new ArgumentException("WebPubSubConnectionString is missing");
        var serviceClient = new WebPubSubServiceClient(cs, "hub");
        var url           = serviceClient.GetClientAccessUri(userId: "42");

        _client = new WebPubSubClient(url);
        _client.ServerMessageReceived += OnServerMessageReceived;

        await _client.StartAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Pages/ServerTelemetryData.razor.js");
            _mapJs  = await _module.InvokeAsync<IJSObjectReference>("initMap", _mapEl, 41.884347835000028, 12.488813031000063);
        }
    }

    private async Task OnServerMessageReceived(WebPubSubServerMessageEventArgs e)
    {
        var telemetryData = e.Message.Data.ToObjectFromJson<UmarellTelemetryData>();
        _text = $"lat {telemetryData.Latitude}, lon: {telemetryData.Longitude}";

        if (_module is not null)
        {
            if (_polyline is null)
                _polyline = await _module.InvokeAsync<IJSObjectReference>("createPolyline", _mapJs, telemetryData.Latitude, telemetryData.Longitude);
            else
                await _module.InvokeVoidAsync("updateMap",
                                          _mapJs,
                                          _polyline,
                                          telemetryData.Latitude,
                                          telemetryData.Longitude);
        }

        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_polyline is not null) await _polyline.DisposeAsync();
            if (_mapJs is not null)    await _mapJs.DisposeAsync();
            if (_module is not null)   await _module.DisposeAsync();
            if (_client is not null)
            {
                _client.ServerMessageReceived -= OnServerMessageReceived;
                await _client.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }
    }
}
