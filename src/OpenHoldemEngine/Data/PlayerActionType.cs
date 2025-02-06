namespace OpenHoldemEngine.Data;

public enum PlayerActionType : byte
{
    SmallBlind = 1,
    BigBlind = 2,
    Fold = 3,
    Check = 4,
    Call = 5,
    Raise = 6,
    Post = 7,
    Bet = 8,
    AllIn = 9,
    Showdown = 100,
    Winner = 101,
    GameWinner = 150,
    BuyIn = 200
}