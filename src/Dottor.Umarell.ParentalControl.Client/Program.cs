using Dottor.Umarell.ParentalControl.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddFluentUIComponents();
builder.Services.AddScoped<IGeofenceService, GeofenceRemoteService>();

await builder.Build().RunAsync();
