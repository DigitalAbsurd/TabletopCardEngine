"""Generates a synthetic pot-limit or fixed-limit Texas hold'em PHH corpus with pokerkit.

pokerkit (MIT) is an independent implementation of the rules, so the hands it plays are an oracle
for this engine where no real data exists: PHH has no code for pot-limit hold'em at all, and the
real fixed-limit corpora never go all in. The State is built from Streets for both structures,
never from pokerkit's predefined FixedLimitTexasHoldem, whose pre-flop cap is one raise more than
this engine allows.

Standard library plus pokerkit 0.7.7. A single random.Random(seed) drives every draw, so a given
seed gives byte-identical output. Run it through scripts/generate-phh-synthetic-fixture.ps1, which
pins the interpreter image and the pokerkit version; it is not part of the build or CI.
"""

import argparse
import collections
import importlib.metadata
import random
import sys

from pokerkit import (
    Automation,
    BettingStructure,
    Deck,
    HandHistory,
    Opening,
    StandardHighHand,
    State,
    Street,
)

GENERATOR = 'pokerkit 0.7.7'
SMALL_BLIND = 50
BIG_BLIND = 100
SMALL_BET = 100
BIG_BET = 200
MIN_SEATS = 2
MAX_SEATS = 10

# Stack bands in big blinds; each seat's stack is a uniform whole-chip amount inside its band.
DEEP = (100, 300)
MEDIUM = (20, 60)
SHORT = (1, 10)

FOLD_WEIGHT = 0.15
CHECK_OR_CALL_WEIGHT = 0.5
BET_OR_RAISE_WEIGHT = 0.35

# Hole cards, the board and the showdown are dealt by the generator so every card is written
# into the history; everything else pokerkit does by itself.
AUTOMATIONS = (
    Automation.ANTE_POSTING,
    Automation.BET_COLLECTION,
    Automation.BLIND_OR_STRADDLE_POSTING,
    Automation.CARD_BURNING,
    Automation.HAND_KILLING,
    Automation.CHIPS_PUSHING,
    Automation.CHIPS_PULLING,
)

RANKS = '23456789TJQKA'
SUITS = 'cdhs'


def build_streets(structure):
    """Four hold'em streets. Fixed-limit allows a bet and three raises on every street, the big
    blind being the bet pre-flop, which pokerkit counts as three raises pre-flop and four after."""
    if structure == 'fixed-limit':
        amounts = (SMALL_BET, SMALL_BET, BIG_BET, BIG_BET)
        caps = (3, 4, 4, 4)
    else:
        amounts = (BIG_BLIND,) * 4
        caps = (None,) * 4
    board_counts = (0, 3, 1, 1)
    return tuple(
        Street(False, (False, False) if index == 0 else (), board_counts[index], False,
               Opening.POSITION, amounts[index], caps[index])
        for index in range(4))


def create_state(structure, blinds, stacks):
    betting = BettingStructure.FIXED_LIMIT if structure == 'fixed-limit' else BettingStructure.POT_LIMIT
    return State(AUTOMATIONS, Deck.STANDARD, (StandardHighHand,), build_streets(structure), betting,
                 True, (0,) * len(stacks), blinds, 0, stacks, len(stacks))


def draw_stacks(rng, seat_count):
    """About a third of hands carry at least one short seat, so all-ins, side pots and
    incomplete raises happen; the rest mix deep and medium stacks."""
    bands = [rng.choice((DEEP, MEDIUM)) for _ in range(seat_count)]
    if rng.random() < 1 / 3:
        bands[rng.randrange(seat_count)] = SHORT
        for index in range(seat_count):
            if bands[index] is not SHORT and rng.random() < 0.25:
                bands[index] = SHORT
    return [rng.randint(low * BIG_BLIND, high * BIG_BLIND) for low, high in bands]


def card_text(card):
    return f'{card.rank.value}{card.suit.value}'


def play_hand(rng, structure, seat_count):
    """Plays one hand and returns its fields plus what the summary counts."""
    stacks = draw_stacks(rng, seat_count)
    blinds = (SMALL_BLIND, BIG_BLIND) + (0,) * (seat_count - 2)
    deck = [rank + suit for rank in RANKS for suit in SUITS]
    rng.shuffle(deck)

    state = create_state(structure, blinds, stacks)
    actions = []
    all_in = False

    # Per-street bookkeeping for the divergence tag (fixed-limit only).
    street_index = None
    acted = set()
    raise_count = 0
    incomplete_all_in = False
    divergent = False

    def mark_all_in():
        nonlocal all_in
        if any(stack == 0 for stack in state.stacks):
            all_in = True

    for player in range(seat_count):
        cards = deck.pop() + deck.pop()
        state.deal_hole(cards, player)
        actions.append(f'd dh p{player + 1} {cards}')
    mark_all_in()

    while state.status:
        if state.actor_index is not None:
            if state.street_index != street_index:
                street_index = state.street_index
                acted = set()
                raise_count = 0
                incomplete_all_in = False
            actor = state.actor_index
            if incomplete_all_in and actor in acted:
                divergent = True

            options = []
            if state.can_fold():
                options.append(('f', FOLD_WEIGHT))
            if state.can_check_or_call():
                options.append(('cc', CHECK_OR_CALL_WEIGHT))
            if state.can_complete_bet_or_raise_to():
                options.append(('cbr', BET_OR_RAISE_WEIGHT))
            choice = rng.choices([name for name, _ in options], [weight for _, weight in options])[0]

            if choice == 'f':
                state.fold()
                actions.append(f'p{actor + 1} f')
            elif choice == 'cc':
                state.check_or_call()
                actions.append(f'p{actor + 1} cc')
            else:
                standing = max(state.bets)
                low = state.min_completion_betting_or_raising_to_amount
                high = state.max_completion_betting_or_raising_to_amount
                amount = low if structure == 'fixed-limit' else rng.randint(low, high)
                state.complete_bet_or_raise_to(amount)
                actions.append(f'p{actor + 1} cbr {amount}')
                raise_count += 1
                bet = SMALL_BET if street_index < 2 else BIG_BET
                if state.stacks[actor] == 0 and amount - standing < bet:
                    incomplete_all_in = True
                cap = state.streets[street_index].max_completion_betting_or_raising_count
                if incomplete_all_in and cap is not None and raise_count >= cap:
                    divergent = True
            acted.add(actor)
            mark_all_in()
        elif state.can_show_or_muck_hole_cards():
            shown = state.show_or_muck_hole_cards(True)
            cards = ''.join(card_text(card) for card in shown.hole_cards)
            actions.append(f'p{shown.player_index + 1} sm {cards}')
        elif state.can_deal_board():
            count = state.board_dealing_count
            cards = ''.join(deck.pop() for _ in range(count))
            state.deal_board(cards)
            actions.append(f'd db {cards}')
        else:
            raise RuntimeError(f'Stuck hand: no actor, nothing to show or deal ({actions})')

    finishing = list(state.stacks)
    if sum(finishing) != sum(stacks):
        raise RuntimeError(f'Chips not conserved: {stacks} -> {finishing}')
    return {
        'blinds': list(blinds),
        'starting_stacks': stacks,
        'actions': actions,
        'finishing_stacks': finishing,
        'all_in': all_in or any(stack == 0 for stack in finishing),
        'divergent': structure == 'fixed-limit' and divergent,
    }


def toml_list(values):
    return '[' + ', '.join(f"'{value}'" if isinstance(value, str) else str(value) for value in values) + ']'


def hand_toml(structure, seed, number, hand):
    seat_count = len(hand['starting_stacks'])
    lines = [
        f"variant = '{'FT' if structure == 'fixed-limit' else 'PT'}'",
        'ante_trimming_status = true',
        f'antes = {toml_list([0] * seat_count)}',
        f"blinds_or_straddles = {toml_list(hand['blinds'])}",
    ]
    if structure == 'fixed-limit':
        lines += [f'small_bet = {SMALL_BET}', f'big_bet = {BIG_BET}']
    else:
        lines.append(f'min_bet = {BIG_BLIND}')
    lines += [
        f"starting_stacks = {toml_list(hand['starting_stacks'])}",
        f"actions = {toml_list(hand['actions'])}",
        f'hand = {number}',
        f"finishing_stacks = {toml_list(hand['finishing_stacks'])}",
        f"_generator = '{GENERATOR}'",
        f'_seed = {seed}',
    ]
    if hand['divergent']:
        lines.append("_tce_divergence = 'incomplete-all-in'")
    return '\n'.join(lines) + '\n'


def verify_with_pokerkit(texts, expected):
    """Loads written fixed-limit hands back through pokerkit's own PHH reader and replays them."""
    for text, finishing in zip(texts, expected):
        history = HandHistory.loads(text)
        state = None
        for state in history:
            pass
        if state is None or list(state.stacks) != finishing:
            raise RuntimeError(f'pokerkit replays to {state and list(state.stacks)}, expected {finishing}:\n{text}')


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--structure', choices=('pot-limit', 'fixed-limit'), required=True)
    parser.add_argument('--hands', type=int, default=2000)
    parser.add_argument('--seed', type=int, required=True)
    parser.add_argument('--out', required=True)
    args = parser.parse_args()

    installed = importlib.metadata.version('pokerkit')
    if installed != '0.7.7':
        raise RuntimeError(f'pokerkit 0.7.7 is required, found {installed}')

    rng = random.Random(args.seed)
    seat_counts = collections.Counter()
    all_in_hands = 0
    tagged = 0
    texts = []
    finishings = []
    for index in range(args.hands):
        seat_count = MIN_SEATS + index % (MAX_SEATS - MIN_SEATS + 1)
        hand = play_hand(rng, args.structure, seat_count)
        seat_counts[seat_count] += 1
        all_in_hands += hand['all_in']
        tagged += hand['divergent']
        texts.append(hand_toml(args.structure, args.seed, index + 1, hand))
        finishings.append(hand['finishing_stacks'])

    if args.structure == 'fixed-limit':
        step = max(1, args.hands // 200)
        verify_with_pokerkit(texts[::step], finishings[::step])
        print(f'pokerkit replayed {len(texts[::step])} sampled hands to the same finishing stacks')

    variant = 'fixed-limit (FT)' if args.structure == 'fixed-limit' else "pot-limit (non-standard 'PT')"
    with open(args.out, 'w', encoding='utf-8', newline='\n') as output:
        output.write(f'# Synthetic {variant} Texas hold\'em hands, generated with {GENERATOR}, seed {args.seed}.\n')
        output.write('# Generated by scripts/phh-synthetic/generate.py through '
                     'scripts/generate-phh-synthetic-fixture.ps1 - do not edit by hand.\n')
        for index, text in enumerate(texts):
            output.write(f'\n[{index + 1}]\n{text}')

    print(f'{args.structure}: {args.hands} hands, seed {args.seed}')
    for seat_count in range(MIN_SEATS, MAX_SEATS + 1):
        print(f'  {seat_count:2} seats: {seat_counts[seat_count]} hands')
    print(f'  hands with an all-in: {all_in_hands}')
    if args.structure == 'fixed-limit':
        print(f"  hands tagged _tce_divergence = 'incomplete-all-in': {tagged}")
    return 0


if __name__ == '__main__':
    sys.exit(main())
