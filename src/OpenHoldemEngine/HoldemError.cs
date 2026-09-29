namespace OpenHoldemEngine;

/// <summary>
/// One reason an action was rejected.
/// </summary>
/// <param name="Code">The stable, machine-readable reason.</param>
/// <param name="Message">The localised, human-readable text for <paramref name="Code"/>.</param>
public sealed record HoldemError(HoldemErrorCode Code, string Message);
