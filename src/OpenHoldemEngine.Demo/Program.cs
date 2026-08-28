using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OpenHoldemEngine.Demo;
using OpenHoldemEngine.Demo.Engine;
using OpenHoldemEngine.Demo.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddAntDesign();

// Configure HoldemEngine with SQLite for WebAssembly
DemoEngineHost.ConfigureHoldemEngine(builder.Services);

// Register demo services
builder.Services.AddScoped<DemoGameSession>();

await builder.Build().RunAsync();
