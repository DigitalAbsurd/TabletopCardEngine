namespace TabletopCardEngine.Holdem.Data;

/// <summary>Why chips entered a seat's stack, so a join can be told from a top-up and a rebuy.</summary>
internal enum BuyInKind
{
    Join = 1,
    TopUp = 2,
    Rebuy = 3,
}
