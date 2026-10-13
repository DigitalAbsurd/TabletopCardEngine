using Microsoft.Extensions.DependencyInjection;

namespace TabletopCardEngine;

/// <summary>
/// The engine's registration, handed to the delegate of
/// <see cref="TabletopCardEngineServiceCollectionExtensions.AddTabletopCardEngine"/>.
/// <para>
/// It knows no game. Game packages extend it with <c>AddXxx</c> extension methods from their own
/// namespace, such as <c>AddHoldem</c> in <c>TabletopCardEngine.Holdem</c>, which register their
/// services into <see cref="Services"/>.
/// </para>
/// </summary>
public sealed class TabletopCardEngineBuilder
{
    internal TabletopCardEngineBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>The container the games are registered into.</summary>
    public IServiceCollection Services { get; }
}
