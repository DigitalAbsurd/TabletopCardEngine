using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;
using OpenHoldemEngine.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NHandRankType = NPokerEngine.Types.HandRankType;

namespace OpenHoldemEngine.Tests;
internal static class Helpers
{
    // PA hand log example
    /*
Poker Academy (Cash Game): $1/$2 NL , 6 players
Hand#1

Dummy ($200)
Gus Xensen ($200)
Jack Pott ($200)
Kale ($200)
Oliver Chips ($200)
Daniel Xn ($200)

Gus Xensen is the button.

Precards: 
Jack Pott posts the small blind $1, Kale posts the big blind $2.

Preflop: Dummy is dealt 2&diams; 5&diams; (6 active)
2 folds, Dummy calls $2, Gus Xensen calls $2, Jack Pott calls $1, Kale bets $8, Dummy calls $8, 2 folds.

Flop: 7&hearts; K&diams; A&spades; ($24, 2 active)
Kale bets $24, Dummy calls $24.

Turn: 7&hearts; K&diams; A&spades; 3&spades; ($72, 2 active)
Kale bets $36, Dummy calls $36.

River: 7&hearts; K&diams; A&spades; 3&spades; Q&spades; ($144, 2 active)
Kale bets $130 (all-in), Dummy calls $130 (all-in).

Final Pot: $404
Dummy, net: -$200, Lost at showdown, has 2&diams; 5&diams; (Ace High)
Kale, net: $204, Won at showdown, has K&spades; 9&spades; (a Flush, Ace High)
Gus Xensen, net: -$2, Folded Preflop, has 9&hearts; 4&hearts;
Jack Pott, net: -$2, Folded Preflop, has 4&clubs; Q&hearts;
Daniel Xn, Folded Preflop, has 5&hearts; T&spades;
Oliver Chips, Folded Preflop, has 6&diams; J&hearts;
    */

    internal static PapGameData GetPAPEmulatedGameData()
    {
        var log = ReadResourceAsString($"{nameof(OpenHoldemEngine)}.{nameof(OpenHoldemEngine.Tests)}.Resources.pap_emulated_game.txt")
            .Split("============================================================", StringSplitOptions.RemoveEmptyEntries)
            .Select((handLog, ix) => new { HandNo = (ix + 1), HandLog = handLog.Split("\r\n", StringSplitOptions.RemoveEmptyEntries) })
            .ToDictionary(k => k.HandNo, v => v.HandLog);

        var papGameData = new PapGameData();

        for (int ix = 1; ix <= log.Count; ix++)
        {
            var parsedHand = ParseHandLog(log[ix]);
            if (ix == 1)
            {
                papGameData.PlayersMap = parsedHand.startStacks.Keys
                    .AsEnumerable()
                    .Select((n, ix) => new { Name = n, SeatNo = (byte)(ix + 1) })
                    .ToDictionary(item => item.Name, item => item.SeatNo);
                papGameData.InitialSmallBlind = parsedHand.blinds.Values.First();
                papGameData.StartDealerSeatNo = papGameData.PlayersMap[parsedHand.button];
            }
            var papHandData = new PapHandData() { HandNo = ix };
            papHandData.DeckCards = parsedHand.communityCards.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
            papHandData.ButtonSeatNo = papGameData.PlayersMap.ContainsKey(parsedHand.button) ? papGameData.PlayersMap[parsedHand.button] : default;
            papHandData.SmallBlindSeatNo = papGameData.PlayersMap[parsedHand.blinds.First().Key];
            papHandData.BigBlindSeatNo = papGameData.PlayersMap[parsedHand.blinds.Last().Key];
            foreach (var kvp in parsedHand.startStacks)
            {
                papHandData.StartStacks.Add(papGameData.PlayersMap[kvp.Key], kvp.Value);
            }
            papHandData.Winners = parsedHand.winners.Select(w => papGameData.PlayersMap[w]).ToArray();
            papHandData.HoleCards = parsedHand.handResults.ToDictionary(res => papGameData.PlayersMap[res.Item1], res => new Card[] { res.hole1, res.hole2 });
            papHandData.FinalPot = parsedHand.finalPot;
            papHandData.HandResults = parsedHand.handResults.Select(res =>
            new PapHandResultData 
            { 
                SeatNo = papGameData.PlayersMap[res.Item1],
                Net = res.Item2,
                RankType = res.handRankType

            }).ToList();

            foreach (var actionItem in parsedHand.actionsStack)
            {
                papHandData.ActionStack.Add(new PapActionData
                {
                    No = actionItem.Item2.No,
                    SeatNo = papGameData.PlayersMap.ContainsKey(actionItem.Item1) ? papGameData.PlayersMap[actionItem.Item1] : default,
                    Street = actionItem.Item2.Street,
                    Size = actionItem.Item2.Size,
                    ActionType = actionItem.Item2.ActionType,
                });
            }

            papGameData.Hands.Add(papHandData);
        }

        return papGameData;

        (Dictionary<string, decimal> startStacks, string button, Dictionary<string, decimal> blinds, 
            Dictionary<Street, List<Card>> communityCards, List<(string, PlayerActionData)> actionsStack, decimal finalPot,
            List<(string, decimal, Card hole1, Card hole2, NHandRankType? handRankType)> handResults, List<string> winners) 
            ParseHandLog(string[] handLog)
        {
            bool playersStartStacksReaded = false;
            int actionNum = 0;
            Street? currentStreet = null;
            (Dictionary<string, decimal> startStacks, string button, Dictionary<string, decimal> blinds,
                Dictionary<Street, List<Card>> communityCards, List<(string, PlayerActionData)> actionsStack, decimal finalPot,
                List<(string, decimal, Card hole1, Card hole2, NHandRankType? handRankType)> handResults, List<string> winners) parsed = 
                (startStacks: new(), 
                button: string.Empty, 
                blinds: new(),
                communityCards: new(),
                actionsStack: new(),
                finalPot: 0,
                handResults: new(),
                winners: new());
            foreach (var logItem in handLog)
            {
                if (logItem.StartsWith("Poker Academy (Cash Game)"))
                    continue;
                if (logItem.StartsWith("Hand#"))
                    continue;

                if (!playersStartStacksReaded)
                {
                    if (logItem.Contains("is the button."))
                    {
                        playersStartStacksReaded = true;
                    }
                    else
                    {
                        parsed.startStacks.Add(logItem.Split("(")[0].Trim(), ParseDecimalFromString(logItem));
                        continue;
                    }
                }

                if (logItem.Contains("is the button."))
                {
                    parsed.button = logItem.Replace("is the button.", "").Trim();
                    continue;
                }
                
                if (logItem.Contains("posts the small blind"))
                {
                    var blindsSplit = logItem.Split(",");
                    parsed.blinds.Add(GetNameFromString(blindsSplit[0]), ParseDecimalFromString(blindsSplit[0]));
                    parsed.blinds.Add(GetNameFromString(blindsSplit[1]), ParseDecimalFromString(blindsSplit[1]));
                    continue;
                }
                if (logItem.StartsWith("Preflop:"))
                {
                    currentStreet = Street.PreFlop;
                    continue;
                }
                if (logItem.StartsWith("Flop:"))
                {
                    currentStreet = Street.Flop;
                    ParseCommunityCards(currentStreet.Value, logItem.Replace("Flop:", ""));
                    continue;
                }
                if (logItem.StartsWith("Turn:"))
                {
                    currentStreet = Street.Turn;
                    ParseCommunityCards(currentStreet.Value, logItem.Replace("Turn:", ""));
                    continue;
                }
                if (logItem.StartsWith("River:"))
                {
                    currentStreet = Street.River;
                    ParseCommunityCards(currentStreet.Value, logItem.Replace("River:", ""));
                    continue;
                }

                if (currentStreet != null)
                {
                    ParseActionData(currentStreet.Value, logItem);
                }

                if (logItem.StartsWith("Final Pot:"))
                {
                    currentStreet = null;
                    parsed.finalPot = ParseDecimalFromString(logItem);
                    continue;
                }
                if (currentStreet == null && parsed.finalPot > 0)
                {
                    var playerResultParts = logItem.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var netPart = playerResultParts.FirstOrDefault(t => t.Contains("net"));
                    var hasPart = playerResultParts.First(t => t.Contains("has")).Replace("has ", "");
                    var rankTypeSplit = logItem.Split('(', ')');
                    var rankTypePart = rankTypeSplit.Length > 1 ? rankTypeSplit[1] : null;
                    parsed.handResults.Add((
                        playerResultParts[0],
                        !string.IsNullOrEmpty(netPart) ? ParseDecimalFromString(netPart) : 0M,
                        hole1: ParseCard(hasPart.Split(";")[0].Trim()),
                        hole2: ParseCard(hasPart.Split(";")[1].Trim()),
                        handRankType: !string.IsNullOrEmpty(rankTypePart) ? ParseRankType(rankTypePart) : null
                        ));
                    if (logItem.Contains(" Won "))
                        parsed.winners.Add(playerResultParts[0]);
                }
            }

            return parsed;

            NHandRankType ParseRankType(string rankStr)
            {
                if (rankStr.Contains("a Pair"))
                    return NHandRankType.ONEPAIR;
                if (rankStr.Contains("High"))
                    return NHandRankType.HIGHCARD;
                if (rankStr.Contains("Three of a Kind"))
                    return NHandRankType.THREECARD;
                if (rankStr.Contains("Two Pair"))
                    return NHandRankType.TWOPAIR;
                if (rankStr.Contains("a Flush"))
                    return NHandRankType.FLASH;
                if (rankStr.Contains("a Full House"))
                    return NHandRankType.FULLHOUSE;

                throw new Exception();
            };

            string GetNameFromString(string strWithName)
               => parsed.startStacks.Keys.AsEnumerable().First(n => strWithName.Contains(n));
            
            decimal ParseDecimalFromString(string moneyString)
                => decimal.Parse(new string(moneyString.Where(char.IsDigit).ToArray()));

            void ParseCommunityCards(Street street, string cardsStr)
            {
                //parsed.communityCards.Clear();
                parsed.communityCards[street] =
                    cardsStr.Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Where(part => !part.Contains("("))
                        .Select(part => part.Trim())
                        .Select(ParseCard)
                        .ToList();
                    //.ForEach(parsed.communityCards.Add);
            }

            Card ParseCard(string cardStr)
            {
                var suitMap = new Dictionary<string, CardSuit>()
                {
                    { "diams", CardSuit.Diamond },
                    { "hearts", CardSuit.Heart },
                    { "spades", CardSuit.Spade },
                    { "clubs", CardSuit.Club }
                };
                CardType cardType;// Enum.Parse<CardType>(Enum.GetNames<CardType>().Single(n => char.ToUpperInvariant(n.First()) == char.ToUpperInvariant(cardStr[0])));
                if (char.IsDigit(cardStr[0]))
                {
                    cardType = (CardType)byte.Parse(cardStr[0].ToString());
                }
                else
                {
                    cardType = Enum.Parse<CardType>(Enum.GetValues<CardType>().Where(ct => (byte)ct < 2 || (byte)ct > 9).Select(ct => ct.ToString()).Single(n => char.ToUpperInvariant(n.First()) == char.ToUpperInvariant(cardStr[0])));
                }
                var card = new Card()
                {
                    CardSuit = suitMap[cardStr.Split('&').Last()],
                    CardType = cardType,
                };
                return card;
            }

            void ParseActionData(Street street, string actionLine)
            {

                if (actionLine.StartsWith("Final Pot:")) return;

                foreach (var line in actionLine.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim().TrimEnd('.')))
                {
                    PlayerActionType? actionType = null;
                    string? playerName = parsed.startStacks.Keys.AsEnumerable().SingleOrDefault(pn => line.StartsWith(pn)); // gamePlayersMap.Values.SingleOrDefault(n => line.StartsWith(n));
                    decimal size = line.Contains("$") ? ParseDecimalFromString(line) : 0;//  ExtractStackFromString(line);

                    if (line.Contains("posts the small blind"))
                        actionType = PlayerActionType.SmallBlind;
                    if (line.Contains("posts the big blind"))
                        actionType = PlayerActionType.BigBlind;
                    if (line.Contains("posts the big blind"))
                        actionType = PlayerActionType.BigBlind;
                    if (line.Contains("folds"))
                        actionType = PlayerActionType.Fold;
                    if (line.Contains("calls"))
                        actionType = PlayerActionType.Call;
                    if (line.Contains("checks"))
                        actionType = PlayerActionType.Check;
                    if (line.Contains("bets"))
                        actionType = PlayerActionType.Bet;
                    if (line.Contains("raises"))
                        actionType = PlayerActionType.Raise;


                    if (actionType == null) return;

                    if (actionType == PlayerActionType.Fold && string.IsNullOrWhiteSpace(playerName) && size == 0)
                    {
                        for (int y = 0; y < (int)ParseDecimalFromString(line); y++)
                        {
                            parsed.actionsStack.Add((string.Empty, new PlayerActionData(street, PlayerActionType.Fold, 0, ++actionNum, "")));
                            //actionsData.Add(new ActionData()
                            //{
                            //    No = ++actionNum,
                            //    ActionType = ActionType.Fold,
                            //    PlayerName = string.Empty,
                            //    Size = 0
                            //});
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(playerName)) throw new NotImplementedException();
                        //actionsData.Add(new ActionData()
                        //{
                        //    No = ++actionNum,
                        //    ActionType = actionType.Value,
                        //    PlayerName = playerName,
                        //    Size = size
                        //});
                        parsed.actionsStack.Add((playerName, new PlayerActionData(street, actionType.Value, size, ++actionNum, "")));
                    }

                }

                return;
            }
        }


    }

    private static string ReadResourceAsString(string resourceName)
    {
        using var stream = System.Reflection.Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resourceName);
        using var sr = new StreamReader(stream!);
        return sr.ReadToEnd();
    }

    internal record PapGameData
    {
        public decimal InitialSmallBlind { get; set; } = 1;
        public byte StartDealerSeatNo {  get; set; }
        public Dictionary<string, byte> PlayersMap { get; set; } = new();
        public List<PapHandData> Hands { get; set; } = new();
    }
    internal record PapHandData
    {
        public int HandNo { get; set; }
        public byte ButtonSeatNo { get; set; }
        public byte SmallBlindSeatNo { get; set; }
        public byte BigBlindSeatNo { get; set; }
        public decimal FinalPot { get; set; }
        public Dictionary<byte, decimal> StartStacks { get; set; } = new();
        public Dictionary<Street, Card[]> DeckCards { get; set; } = new();
        public Dictionary<byte, Card[]> HoleCards { get; set; } = new();
        public List<PapActionData> ActionStack { get; set; } = new();
        public byte[] Winners { get; set; } = Array.Empty<byte>();
        public List<PapHandResultData> HandResults { get; set; } = new();
    }
    internal record PapActionData
    {
        public int No {  get; set; }
        public Street Street { get; set; }
        public byte? SeatNo { get; set; }
        public PlayerActionType ActionType { get; set; }
        public decimal Size { get; set; }
    }
    internal record PapHandResultData
    {
        public byte SeatNo { get; set; }
        public decimal Net { get; set; }
        public NHandRankType? RankType { get; set; }
    }
}
