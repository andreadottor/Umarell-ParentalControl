﻿namespace Dottor.Umarell.ParentalControl.Components.Pages;

using Azure.Messaging.WebPubSub.Clients;
using Azure.Messaging.WebPubSub;
using Dottor.Umarell.ParentalControl.Client.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public partial class ServerTelemetryData
{
    private WebPubSubClient? _client;
    private string? _text;
    private IJSObjectReference? _module;
    private IJSObjectReference? _polyline;
    private IJSObjectReference? _mapJs;
    private ElementReference?   _mapEl;


    protected override async Task OnInitializedAsync()
    {
        string webPubSubConnectionString = Configuration.GetConnectionString("WebPubSubConnectionString");
        var serviceClient = new WebPubSubServiceClient(webPubSubConnectionString, "hub");
        var url = serviceClient.GetClientAccessUri(userId: "42");

        _client = new WebPubSubClient(url);
        _client.ServerMessageReceived += OnServerMessageReceived;

        await _client.StartAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Pages/ServerTelemetryData.razor.js");
            _mapJs = await _module.InvokeAsync<IJSObjectReference>("initMap", _mapEl, 41.884347835000028, 12.488813031000063);
            _polyline = await _module.InvokeAsync<IJSObjectReference>("createPolyline", _mapJs, 41.884347835000028, 12.488813031000063);
        }
    }

    private async Task OnServerMessageReceived(WebPubSubServerMessageEventArgs e)
    {
        var telemetryData = e.Message.Data.ToObjectFromJson<UmarellTelemetryData>();
        _text = $"lat {telemetryData.Latitude}, lon: {telemetryData.Longitude}";

        if (_module is not null)
            _ = _module.InvokeVoidAsync("updateMap",
                                        _mapJs,
                                        _polyline,
                                        telemetryData.Latitude,
                                        telemetryData.Longitude);

        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
            await _client.DisposeAsync();
    }
}