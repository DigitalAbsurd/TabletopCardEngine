using System.ComponentModel.DataAnnotations;

namespace TabletopCardEngine.AspNetCore;

/// <summary>
/// The engine settings a host can bind from configuration. They are validated through the Options
/// pattern, so a bad value fails the host on start instead of throwing from service registration.
/// <para>
/// The configuration section is the source of the engine's settings: it overwrites any
/// <c>WithSettings</c> call made inside the <c>configure</c> callback of
/// the <c>AddHoldem(configuration, ...)</c> overload in <see cref="HoldemHostingBuilderExtensions"/>.
/// </para>
/// </summary>
public sealed class HoldemHostOptions : IValidatableObject
{
    /// <summary>Fewest seats a game may have. This release allows 2 to 10; a setting can only narrow it.</summary>
    [Range(2, 10)]
    public int GamePlayersMinCount { get; set; } = 2;

    /// <summary>Most seats a game may have. This release allows 2 to 10; a setting can only narrow it.</summary>
    [Range(2, 10)]
    public int GamePlayersMaxCount { get; set; } = 10;

    /// <summary>
    /// When <c>true</c>, the engine logs hole cards and the board at Trace level. Never enable it in production.
    /// </summary>
    public bool EnableSensitiveCardLogging { get; set; }

    /// <summary>
    /// When <c>true</c>, the hosted service migrates the database schema on start-up, before any request reaches
    /// the engine. A failed migration throws out of <c>StartAsync</c> and fails the host. Default is <c>false</c>,
    /// so a deployment must opt in explicitly.
    /// </summary>
    public bool MigrateDatabaseOnStartup { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (GamePlayersMinCount > GamePlayersMaxCount)
        {
            yield return new ValidationResult(
                "GamePlayersMinCount must not exceed GamePlayersMaxCount.",
                [nameof(GamePlayersMinCount), nameof(GamePlayersMaxCount)]);
        }
    }
}
