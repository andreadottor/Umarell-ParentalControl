namespace Dottor.Umarell.ParentalControl.Client.Pages;

using Azure.Messaging.WebPubSub.Clients;
using Dottor.Umarell.ParentalControl.Client.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Json;

public partial class WasmTelemetryData
{
    private WebPubSubClient?    _client;
    private string?             _text;
    private IJSObjectReference? _module;
    private IJSObjectReference? _polyline;
    private IJSObjectReference? _mapJs;
    private ElementReference?   _mapEl;

    protected override async Task OnInitializedAsync()
    {
        var response = await Http.GetFromJsonAsync<NegotiateResponse>("negotiate");
        if (!string.IsNullOrWhiteSpace(response.Url))
        {
            _client = new WebPubSubClient(new Uri(response.Url));
            _client.ServerMessageReceived += OnServerMessageReceived;
            await _client.StartAsync();
        }
        else
        {
            throw new Exception("Fail to negotiate websocket url");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Pages/WasmTelemetryData.razor.js");
            _mapJs = await _module.InvokeAsync<IJSObjectReference>("initMap", _mapEl, 41.884347835000028, 12.488813031000063);
        }
    }

    private async Task OnServerMessageReceived(WebPubSubServerMessageEventArgs e)
    {
        var telemetryData = e.Message.Data.ToObjectFromJson<UmarellTelemetryData>();
        _text = $"lat {telemetryData.Latitude}, lon: {telemetryData.Longitude}";
        ToastService.ShowToast(ToastIntent.Info, _text);

        if (_module is not null)
        {
            if(_polyline is null)
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
        if (_polyline is not null) await _polyline.DisposeAsync();
        if (_mapJs is not null) await _mapJs.DisposeAsync();
        if (_module is not null) await _module.DisposeAsync();
        if (_client is not null)
        {
            _client.ServerMessageReceived -= OnServerMessageReceived;
            await _client.DisposeAsync();
        }
    }
}
