using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Diagnostics;
using System.Globalization;

namespace TabletopCardEngine.Demo.BrowserTests;

/// <summary>
/// The demo under test: one Release publish of the WebAssembly app, served over a local
/// static file host, plus one headless browser to drive it with.
/// </summary>
/// <remarks>
/// A WebAssembly publish takes roughly a minute, so it happens once per fixture and every
/// test gets its own browser context over the same host. The demo keeps its database in the
/// tab's memory and recreates it on every page load, so a fresh context is a fresh database
/// and there is nothing to clean up between tests.
/// </remarks>
internal sealed class DemoSite : IAsyncDisposable
{
    /// <summary>Set to 1 or true to run the browser gate; see <see cref="IsEnabled"/>.</summary>
    private const string EnabledVariable = "TCE_TEST_BROWSER_GATE";

    /// <summary>Publish into this directory and reuse it, instead of a throwaway one.</summary>
    private const string PublishDirectoryVariable = "TCE_TEST_BROWSER_GATE_PUBLISH_DIR";

    private readonly WebApplication _host;
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;
    private readonly string? _publishDirectoryToDelete;

    private DemoSite(WebApplication host, IPlaywright playwright, IBrowser browser, string baseUrl, string? publishDirectoryToDelete)
    {
        _host = host;
        _playwright = playwright;
        _browser = browser;
        _publishDirectoryToDelete = publishDirectoryToDelete;
        BaseUrl = baseUrl;
    }

    /// <summary>
    /// Whether the browser gate should run at all. It is off by default because the first run
    /// downloads a browser and publishes the WebAssembly app, neither of which belongs in an
    /// unsuspecting dotnet test.
    /// </summary>
    public static bool IsEnabled
    {
        get
        {
            string? value = Environment.GetEnvironmentVariable(EnabledVariable);
            return string.Equals(value, "1", StringComparison.Ordinal)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// The demo project this fixture publishes, found by walking up to the solution file.
    /// </summary>
    public static string DemoProjectPath =>
        Path.Combine(FindRepositoryRoot(), "src", "TabletopCardEngine.Demo", "TabletopCardEngine.Demo.csproj");

    /// <summary>The root URL the published demo is served from, without a trailing slash.</summary>
    public string BaseUrl { get; }

    /// <summary>Publishes the demo, starts the file host and launches the browser.</summary>
    public static async Task<DemoSite> StartAsync()
    {
        string? configuredPublishDirectory = Environment.GetEnvironmentVariable(PublishDirectoryVariable);
        string publishDirectory = string.IsNullOrWhiteSpace(configuredPublishDirectory)
            ? Path.Combine(Path.GetTempPath(), "tce-demo-browser-gate", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture))
            : configuredPublishDirectory;

        string wwwroot = Path.Combine(publishDirectory, "wwwroot");
        if (!File.Exists(Path.Combine(wwwroot, "index.html")))
        {
            PublishDemo(publishDirectory);
        }

        InstallBrowser();

        WebApplication host = StartFileHost(wwwroot);
        string baseUrl = ReadBaseUrl(host);

        IPlaywright playwright = await Playwright.CreateAsync();
        IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

        return new DemoSite(host, playwright, browser, baseUrl, configuredPublishDirectory is null ? publishDirectory : null);
    }

    /// <summary>Opens a page of the demo at the given viewport, wired for console-error capture.</summary>
    public async Task<DemoPage> NewPageAsync(int viewportWidth, int viewportHeight)
    {
        IBrowserContext context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = viewportWidth, Height = viewportHeight },
        });

        // The walkthrough modal opens on a first visit and covers the page; a returning visitor
        // is the state worth measuring, and this flag is exactly what BrowserStorage reads.
        await context.AddInitScriptAsync("window.localStorage.setItem('tce-demo-walkthrough-dismissed', 'true');");

        IPage page = await context.NewPageAsync();
        return new DemoPage(context, page, BaseUrl);
    }

    /// <summary>Stops the browser and the file host, and removes a throwaway publish.</summary>
    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
        await _host.StopAsync();
        await _host.DisposeAsync();

        if (_publishDirectoryToDelete is not null && Directory.Exists(_publishDirectoryToDelete))
        {
            try
            {
                Directory.Delete(_publishDirectoryToDelete, recursive: true);
            }
            catch (IOException)
            {
                // A leftover temporary publish is not worth failing a green run over.
            }
            catch (UnauthorizedAccessException)
            {
                // As above.
            }
        }
    }

    private static void PublishDemo(string publishDirectory)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = FindRepositoryRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("publish");
        startInfo.ArgumentList.Add(DemoProjectPath);
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(publishDirectory);

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet publish for the demo.");

        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Publishing the demo failed with exit code {process.ExitCode}. The wasm-tools workload is required."
                + $"{Environment.NewLine}{output.Result}{Environment.NewLine}{error.Result}");
        }
    }

    private static void InstallBrowser()
    {
        // Idempotent and quick once the browser is on disk; the first run downloads it.
        int exitCode = Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Playwright could not install Chromium (exit code {exitCode}). Run "
                + "pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps chromium from this project and try again.");
        }
    }

    private static WebApplication StartFileHost(string wwwroot)
    {
        if (!Directory.Exists(wwwroot))
        {
            throw new DirectoryNotFoundException($"The published demo was not found at {wwwroot}.");
        }

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        WebApplication host = builder.Build();

        // Not all of the extensions Blazor ships are known content types, and the runtime fails
        // to boot rather than fails to load a file when one comes back as the wrong type.
        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings[".blat"] = "application/octet-stream";
        contentTypes.Mappings[".dat"] = "application/octet-stream";
        contentTypes.Mappings[".dll"] = "application/octet-stream";
        contentTypes.Mappings[".pdb"] = "application/octet-stream";
        contentTypes.Mappings[".wasm"] = "application/wasm";

        host.UseFileServer(new FileServerOptions
        {
            FileProvider = new PhysicalFileProvider(wwwroot),
            StaticFileOptions =
            {
                ContentTypeProvider = contentTypes,
                ServeUnknownFileTypes = true,
                DefaultContentType = "application/octet-stream",
            },
        });

        host.StartAsync().GetAwaiter().GetResult();
        return host;
    }

    private static string ReadBaseUrl(WebApplication host)
    {
        IServerAddressesFeature? addresses = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        string address = addresses?.Addresses.FirstOrDefault()
            ?? throw new InvalidOperationException("The static file host reported no address.");

        return address.TrimEnd('/');
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TabletopCardEngine.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not find TabletopCardEngine.sln above {AppContext.BaseDirectory}.");
    }
}
