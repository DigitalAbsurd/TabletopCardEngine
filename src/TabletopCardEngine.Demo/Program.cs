using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TabletopCardEngine.Demo.Engine;
using TabletopCardEngine.Demo.Services;
using TabletopCardEngine.Holdem;

namespace TabletopCardEngine.Demo;

internal sealed class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddAntDesign();

        // Configure HoldemEngine with SQLite for WebAssembly
        DemoEngineHost.ConfigureHoldemEngine(builder.Services);

        // Register demo services
        builder.Services.AddScoped<BrowserStorage>();
        builder.Services.AddScoped<DemoTheme>();
        builder.Services.AddScoped<WalkthroughState>();
        builder.Services.AddScoped<NewGameState>();
        builder.Services.AddScoped<DemoGameSession>();
        builder.Services.AddScoped<SqliteBrowser>();
        builder.Services.AddScoped<DemoDatabase>();
        builder.Services.AddScoped<PhhReplayController>();

        var host = builder.Build();

        // One migration covers the play engine and the replay engine: they share the connection.
        await host.Services.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();

        await host.RunAsync();
    }
}
