using Azure.Messaging.WebPubSub;
using Dottor.Umarell.ParentalControl.Client.Models;
using Dottor.Umarell.ParentalControl.Components;
using Dottor.Umarell.ParentalControl.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();


builder.Services.AddHostedService<UmarellSimulatorService>();


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


// return the Client Access URL with negotiate endpoint
app.MapGet("/negotiate", (HttpContext context, IConfiguration configuration) =>
{
    string webPubSubConnectionString = builder.Configuration.GetConnectionString("WebPubSubConnectionString");
    ArgumentNullException.ThrowIfNullOrWhiteSpace(webPubSubConnectionString, nameof(webPubSubConnectionString));
    
    var serviceClient = new WebPubSubServiceClient(webPubSubConnectionString, "hub");

    return new NegotiateResponse
    {
        Url = serviceClient.GetClientAccessUri(userId: "42").AbsoluteUri
    };
});

app.Run();
