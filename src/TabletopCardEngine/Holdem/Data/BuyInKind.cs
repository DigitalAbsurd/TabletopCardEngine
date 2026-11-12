namespace TabletopCardEngine.Holdem.Data;

/// <summary>
/// Why chips entered a seat's stack, so a join can be told from a top-up, a rebuy and a tournament player's stack
/// carried over from another table. Stored as text, so a new member needs no migration.
/// </summary>
internal enum BuyInKind
{
    Join = 1,
    TopUp = 2,
    Rebuy = 3,
    Transfer = 4,
}
