using System.ComponentModel;

namespace OpenHoldemEngine.Primitives;

[DefaultValue(Street.PreFlop)]
public enum Street : byte
{
    PreFlop = 0,
    Flop = 3,
    Turn = 4,
    River = 5,
    Showdown = 6
}