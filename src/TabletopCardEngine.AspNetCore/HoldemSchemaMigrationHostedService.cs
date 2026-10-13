using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TabletopCardEngine.Holdem;

namespace TabletopCardEngine.AspNetCore;

/// <summary>
/// An optional hosted service that migrates the database schema on application start-up when
/// <see cref="HoldemHostOptions.MigrateDatabaseOnStartup"/> is <c>true</c>. It is always
/// registered but does nothing when the flag is off.
/// </summary>
internal sealed class HoldemSchemaMigrationHostedService(
    IOptions<HoldemHostOptions> options,
    IHoldemSchemaManager schemaManager) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.Value.MigrateDatabaseOnStartup)
        {
            await schemaManager.MigrateAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
