using System.ComponentModel;

namespace OpenHoldemEngine.Primitives;
public enum CardSuit : byte
{
    [Description("♣")] Club = 0,
    [Description("♦")] Diamond = 1,
    [Description("♥")] Heart = 2,
    [Description("♠")] Spade = 3
}
