namespace TabletopCardEngine.Holdem.Tournaments;

/// <summary>
/// The entry point for Hold'em tournaments, next to <see cref="IHoldemTable"/>. A tournament is an orchestrator of
/// ordinary Hold'em games: its tables are games the players act on through <see cref="IHoldemTable"/>, while this
/// interface creates the tournament and takes the registrations.
/// <para>
/// Like <see cref="IHoldemTable"/>, it keeps no state in memory: every call reloads the tournament from the database,
/// applies the action and saves it. Resolve it from the container <c>AddHoldem</c> was called on.
/// </para>
/// </summary>
public interface IHoldemTournament
{
    /// <summary>Applies one action to a tournament and returns the tournament as it is afterwards.</summary>
    /// <param name="action">
    /// What to do. Every action but <see cref="TournamentActionCreate"/> needs a tournament that exists, and fails with
    /// <see cref="HoldemErrorCode.TournamentNotFound"/> otherwise.
    /// </param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>
    /// The tournament on success. On failure, the reason as a <see cref="HoldemError"/>; nothing was saved then.
    /// </returns>
    Task<HoldemResult<TournamentData>> ApplyAsync(TournamentAction action, CancellationToken cancellationToken = default);
}
