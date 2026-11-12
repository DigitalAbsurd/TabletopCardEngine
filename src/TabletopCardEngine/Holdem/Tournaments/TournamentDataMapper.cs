using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Mappers;

namespace TabletopCardEngine.Holdem.Tournaments;

/// <summary>Converts between the tournament's entities and its public data, in both directions for the options.</summary>
internal static class TournamentDataMapper
{
    /// <summary>
    /// The <see cref="TournamentEntry.Place"/> of a withdrawn registration. An <c>Unregister</c> keeps the row, so that
    /// the highest entry number ever given stays in the table and is never given again; no real place is <c>0</c>.
    /// Every reader of the entries skips these rows through <see cref="IsWithdrawn"/>.
    /// </summary>
    internal const int WithdrawnPlace = 0;

    /// <summary>Whether the row is a withdrawn registration rather than a player of the tournament.</summary>
    internal static bool IsWithdrawn(this TournamentEntry entry) => entry.Place == WithdrawnPlace;

    /// <summary>The entries that are players of the tournament, ascending by entry number.</summary>
    internal static IEnumerable<TournamentEntry> ActiveEntries(this Tournament tournament)
        => tournament.Entries.Where(entry => !entry.IsWithdrawn()).OrderBy(entry => entry.EntryNo);

    internal static TournamentData ToTournamentData(Tournament tournament)
    {
        var entries = tournament.ActiveEntries().ToList();
        var state = (TournamentState)tournament.State;

        return new TournamentData(tournament.Id, ToOptions(tournament), state, tournament.Version)
        {
            Entries = new EquatableList<TournamentEntryData>(entries.Select(entry => ToEntryData(entry, state))),
            Tables = new EquatableList<TournamentTableData>(tournament.Tables
                .OrderBy(table => table.TableNo)
                .Select(table => new TournamentTableData(table.TableNo, table.GameId)
                {
                    IsBroken = table.IsBroken,
                    PlayerCount = entries.Count(entry => entry.TableNo == table.TableNo && EntryState(entry, state) == TournamentEntryState.Seated),
                })),
            StartedAt = tournament.StartedAt,
            FinishedAt = tournament.FinishedAt,
            PrizePool = tournament.BuyIn * entries.Count,
        };
    }

    internal static TournamentOptionData ToOptions(Tournament tournament)
        => new()
        {
            BettingStructure = tournament.BettingStructure,
            TableFormat = tournament.TableFormat,
            MaxPlayersPerTable = tournament.MaxPlayersPerTable,
            RaiseCap = tournament.RaiseCap,
            AnteType = tournament.AnteType,
            StartingStack = tournament.StartingStack,
            BuyIn = tournament.BuyIn,
            MinEntries = tournament.MinEntries,
            MaxEntries = tournament.MaxEntries,
            Levels = new EquatableList<TournamentLevelData>(tournament.Levels.Select(level => new TournamentLevelData
            {
                SmallBlind = level.SmallBlind,
                Ante = level.Ante,
                Duration = TimeSpan.FromSeconds(level.DurationSeconds),
            })),
            Payouts = new EquatableList<decimal>(tournament.Payouts),
        };

    /// <summary>The levels as they are stored; validation has made every duration a whole number of seconds that fits.</summary>
    internal static List<TournamentLevel> ToEntityLevels(IEnumerable<TournamentLevelData> levels)
        => [.. levels.Select(level => new TournamentLevel(level.SmallBlind, level.Ante, (int)level.Duration.TotalSeconds))];

    private static TournamentEntryData ToEntryData(TournamentEntry entry, TournamentState state)
        => new(entry.EntryNo, entry.Name)
        {
            State = EntryState(entry, state),
            TableNo = entry.TableNo,
            SeatNo = entry.SeatNo,
            Place = entry.Place,
            Prize = entry.Prize,
        };

    /// <summary>
    /// The entry's state, read from its columns since it has none of its own: a place is final, a carried stack is a
    /// move between tables, and a table is a seat.
    /// </summary>
    private static TournamentEntryState EntryState(TournamentEntry entry, TournamentState state)
    {
        if (entry.Place is { } place)
        {
            return place == 1 && state == TournamentState.Finished ? TournamentEntryState.Winner : TournamentEntryState.Eliminated;
        }

        if (entry.CarriedStack is not null)
        {
            return TournamentEntryState.Moving;
        }

        return entry.TableNo is not null ? TournamentEntryState.Seated : TournamentEntryState.Registered;
    }
}
