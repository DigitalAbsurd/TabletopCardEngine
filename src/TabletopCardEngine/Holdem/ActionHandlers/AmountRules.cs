namespace TabletopCardEngine.Holdem.ActionHandlers;

/// <summary>Rules for valid amounts and player names across the engine.</summary>
internal static class AmountRules
{
    /// <summary>Maximum number of decimal places allowed for amounts.</summary>
    internal const int MaxDecimalPlaces = 2;

    /// <summary>Maximum length of a player name.</summary>
    internal const int PlayerNameMaxLength = 64;

    /// <summary>Checks whether an amount has at most two decimal places.</summary>
    internal static bool HasValidScale(decimal amount) => decimal.Round(amount, MaxDecimalPlaces) == amount;
}
