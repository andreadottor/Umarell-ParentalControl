using Azure.Messaging.WebPubSub;
using Dottor.Umarell.ParentalControl.Client.Models;
using Dottor.Umarell.ParentalControl.Client.Services;
using Dottor.Umarell.ParentalControl.Components;
using Dottor.Umarell.ParentalControl.Hubs;
using Dottor.Umarell.ParentalControl.Services;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();


builder.Services.AddHostedService<GeofenceWorker>();
builder.Services.AddHostedService<UmarellSimulatorWorker>();

builder.Services.AddScoped<IGeofenceService, GeofenceService>();

builder.Services.AddFluentUIComponents();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Dottor.Umarell.ParentalControl.Client._Imports).Assembly);

app.MapHub<NotificationHub>("/notification-hub");

// return the Client Access URL with negotiate endpoint
app.MapGet("/negotiate", (HttpContext context, IConfiguration configuration) =>
{
    string? webPubSubConnectionString = builder.Configuration.GetConnectionString("WebPubSubConnectionString");
    ArgumentNullException.ThrowIfNullOrWhiteSpace(webPubSubConnectionString, nameof(webPubSubConnectionString));
    
    var serviceClient = new WebPubSubServiceClient(webPubSubConnectionString, "hub");

    return new NegotiateResponse
    {
        Url = serviceClient.GetClientAccessUri(userId: "42").AbsoluteUri
    };
});


app.Run();
