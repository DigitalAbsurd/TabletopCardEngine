"""Generates a synthetic no-limit, pot-limit or fixed-limit Texas hold'em PHH corpus with pokerkit.

pokerkit (MIT) is an independent implementation of the rules, so the hands it plays are an oracle
for this engine where no real data exists: PHH has no code for pot-limit hold'em at all, and the
real fixed-limit corpora never go all in. The State is built from Streets for both structures,
never from pokerkit's predefined FixedLimitTexasHoldem, whose pre-flop cap is one raise more than
this engine allows.

--antes classic|big-blind adds an ante drawn per hand from a few ratios to the big blind. A classic
ante is posted by every seat (ante_trimming_status true, the uniform case); a big blind ante by the
big blind's seat alone (ante_trimming_status false, which pokerkit documents for non-uniform antes).
The default, none, writes the zeros it always wrote and draws nothing extra, so a corpus generated
before the option existed regenerates byte for byte.

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

# Ratios of the ante to the big blind; every one is a whole number of chips at a big blind of 100.
ANTE_RATIOS = (0.1, 0.25, 0.5, 1, 1.5)
# The chance that one seat gets a stack below one ante plus one big blind, on purpose.
SHORT_OF_ANTE_CHANCE = 0.2

BIG_BLIND_ANTE_TAG = 'big blind ante posted before the blind'

FOLD_WEIGHT = 0.15
CHECK_OR_CALL_WEIGHT = 0.5
BET_OR_RAISE_WEIGHT = 0.35

# Hole cards, the board and the showdown are dealt by the generator so every card is written
# into the history, and so are the chips pushed to the winners, which lets the generator see an
# odd-chip split; everything else pokerkit does by itself.
AUTOMATIONS = (
    Automation.ANTE_POSTING,
    Automation.BET_COLLECTION,
    Automation.BLIND_OR_STRADDLE_POSTING,
    Automation.CARD_BURNING,
    Automation.HAND_KILLING,
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


BETTING_STRUCTURES = {
    'no-limit': BettingStructure.NO_LIMIT,
    'pot-limit': BettingStructure.POT_LIMIT,
    'fixed-limit': BettingStructure.FIXED_LIMIT,
}
VARIANTS = {'no-limit': 'NT', 'pot-limit': 'PT', 'fixed-limit': 'FT'}


def big_blind_index(seat_count):
    """The player pokerkit makes the big blind: index 1, or 0 heads-up, where pokerkit reverses the
    blinds the way the PHH histories do (the last player posts the small blind)."""
    return 0 if seat_count == 2 else 1


def create_state(structure, blinds, stacks, antes, trimming):
    return State(AUTOMATIONS, Deck.STANDARD, (StandardHighHand,), build_streets(structure),
                 BETTING_STRUCTURES[structure], trimming, antes, blinds, 0, stacks, len(stacks))


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


def draw_antes(rng, seat_count, stacks, ante_type):
    """Draws the hand's ante and returns the per-seat antes as passed to pokerkit. One seat in
    five gets a stack below one ante plus one big blind (the big blind's seat in half of those),
    so a seat all in on its ante and a big blind short of both its blind and its ante happen."""
    ante = round(rng.choice(ANTE_RATIOS) * BIG_BLIND)
    bb = big_blind_index(seat_count)
    if rng.random() < SHORT_OF_ANTE_CHANCE:
        seat = bb if ante_type == 'big-blind' and rng.random() < 0.5 else rng.randrange(seat_count)
        stacks[seat] = rng.randint(1, ante + BIG_BLIND - 1)
    if ante_type == 'classic':
        return [ante] * seat_count
    # pokerkit indexes antes like blinds_or_straddles, the big blind's entry being the second one
    # even heads-up, where it pays for player 0 (State.get_effective_ante reverses the two).
    antes = [0] * seat_count
    antes[1] = ante
    return antes


def play_hand(rng, structure, seat_count, ante_type='none'):
    """Plays one hand and returns its fields plus what the summary counts."""
    stacks = draw_stacks(rng, seat_count)
    antes = [0] * seat_count if ante_type == 'none' else draw_antes(rng, seat_count, stacks, ante_type)
    blinds = (SMALL_BLIND, BIG_BLIND) + (0,) * (seat_count - 2)
    deck = [rank + suit for rank in RANKS for suit in SUITS]
    rng.shuffle(deck)

    state = create_state(structure, blinds, stacks, antes, ante_type != 'big-blind')
    actions = []
    all_in = False
    side_pot = False

    # Seats all in on the ante they posted, and whether the big blind was short of its blind plus
    # its ante: pokerkit collects antes before blinds, ADR-0011 posts the big blind first.
    ante_all_in = sum(1 for seat in range(seat_count) if antes[seat] and state.stacks[seat] == 0)
    bb_ante = antes[1] if ante_type == 'big-blind' else 0
    ante_order = bb_ante > 0 and stacks[big_blind_index(seat_count)] < bb_ante + BIG_BLIND
    # A classic ante that empties a seat: the engine then picks the blind seats among the seats that
    # can still post, where pokerkit keeps them. Counted, not tagged: see the pull request.
    classic_empties_seat = ante_type == 'classic' and any(stack <= antes[0] for stack in stacks)

    # Per-street bookkeeping for the divergence tags. The tagging only observes the hand.
    street_index = None
    acted = set()
    raise_count = 0
    last_full_raise = BIG_BLIND
    incomplete_all_in = False
    divergent = False
    odd_chip = False

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
                last_full_raise = BIG_BLIND
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
                if structure == 'fixed-limit':
                    bet = SMALL_BET if street_index < 2 else BIG_BET
                    if state.stacks[actor] == 0 and amount - standing < bet:
                        incomplete_all_in = True
                    cap = state.streets[street_index].max_completion_betting_or_raising_count
                    if incomplete_all_in and cap is not None and raise_count >= cap:
                        divergent = True
                elif amount - standing >= last_full_raise:
                    last_full_raise = amount - standing
                elif state.stacks[actor] == 0:
                    incomplete_all_in = True
            acted.add(actor)
            mark_all_in()
        elif state.can_show_or_muck_hole_cards():
            shown = state.show_or_muck_hole_cards(True)
            cards = ''.join(card_text(card) for card in shown.hole_cards)
            actions.append(f'p{shown.player_index + 1} sm {cards}')
        elif state.can_push_chips():
            if len([pot for pot in state.pots if pot.amount]) > 1:
                side_pot = True
            amounts = [amount for amount in state.push_chips().amounts if amount]
            if len(set(amounts)) > 1:
                odd_chip = True
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
        'antes': antes,
        'starting_stacks': stacks,
        'actions': actions,
        'finishing_stacks': finishing,
        'all_in': all_in or any(stack == 0 for stack in finishing),
        'incomplete_all_in': divergent,
        'odd_chip': odd_chip,
        'ante_all_in': ante_all_in,
        'side_pot': side_pot,
        'ante_order': ante_order,
        'classic_empties_seat': classic_empties_seat,
    }


def toml_list(values):
    return '[' + ', '.join(f"'{value}'" if isinstance(value, str) else str(value) for value in values) + ']'


def hand_toml(structure, seed, number, hand, ante_type='none'):
    lines = [
        f"variant = '{VARIANTS[structure]}'",
        f"ante_trimming_status = {'false' if ante_type == 'big-blind' else 'true'}",
        f"antes = {toml_list(hand['antes'])}",
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
    tags = [tag for tag, applies in (('incomplete-all-in', hand['incomplete_all_in']),
                                     ('odd-chip', hand['odd_chip']),
                                     (BIG_BLIND_ANTE_TAG, hand['ante_order'])) if applies]
    if tags:
        lines.append(f"_tce_divergence = '{', '.join(tags)}'")
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
    parser.add_argument('--structure', choices=tuple(BETTING_STRUCTURES), required=True)
    parser.add_argument('--antes', choices=('none', 'classic', 'big-blind'), default='none')
    parser.add_argument('--hands', type=int, default=2000)
    parser.add_argument('--seed', type=int, required=True)
    parser.add_argument('--out', required=True)
    parser.add_argument('--first-number', type=int, default=1,
                        help='the number of the first hand, so that two runs can be concatenated')
    parser.add_argument('--append', action='store_true', help='append to --out instead of writing it')
    args = parser.parse_args()

    installed = importlib.metadata.version('pokerkit')
    if installed != '0.7.7':
        raise RuntimeError(f'pokerkit 0.7.7 is required, found {installed}')

    rng = random.Random(args.seed)
    seat_counts = collections.Counter()
    all_in_hands = 0
    incomplete_hands = 0
    odd_chip_hands = 0
    both_hands = 0
    ante_all_in_hands = 0
    side_pot_hands = 0
    ante_order_hands = 0
    classic_empties_hands = 0
    texts = []
    finishings = []
    for index in range(args.hands):
        seat_count = MIN_SEATS + index % (MAX_SEATS - MIN_SEATS + 1)
        hand = play_hand(rng, args.structure, seat_count, args.antes)
        seat_counts[seat_count] += 1
        all_in_hands += hand['all_in']
        incomplete_hands += hand['incomplete_all_in']
        odd_chip_hands += hand['odd_chip']
        both_hands += hand['incomplete_all_in'] and hand['odd_chip']
        ante_all_in_hands += hand['ante_all_in'] > 0
        side_pot_hands += hand['side_pot']
        ante_order_hands += hand['ante_order']
        classic_empties_hands += hand['classic_empties_seat']
        texts.append(hand_toml(args.structure, args.seed, args.first_number + index, hand, args.antes))
        finishings.append(hand['finishing_stacks'])

    # pokerkit's reader does not know the made-up PT code; every other structure is loaded back.
    if args.structure != 'pot-limit':
        step = 1 if args.antes != 'none' else max(1, args.hands // 200)
        verify_with_pokerkit(texts[::step], finishings[::step])
        print(f'pokerkit replayed {len(texts[::step])} written hands to the same finishing stacks')

    variants = {
        'no-limit': 'no-limit (NT)',
        'pot-limit': "pot-limit (non-standard 'PT')",
        'fixed-limit': 'fixed-limit (FT)',
    }
    ante_text = '' if args.antes == 'none' else f' with {args.antes} antes'
    with open(args.out, 'a' if args.append else 'w', encoding='utf-8', newline='\n') as output:
        if not args.append:
            output.write(f"# Synthetic {variants[args.structure]} Texas hold'em hands, generated with {GENERATOR}, "
                         f'seed {args.seed}.\n')
            output.write('# Generated by scripts/phh-synthetic/generate.py through '
                         'scripts/generate-phh-synthetic-fixture.ps1 - do not edit by hand.\n')
        elif args.antes != 'none':
            output.write(f'\n# Hands {args.first_number} to {args.first_number + args.hands - 1}: '
                         f'{args.antes} antes, seed {args.seed}.\n')
        for index, text in enumerate(texts):
            output.write(f'\n[{args.first_number + index}]\n{text}')

    print(f'{args.structure}{ante_text}: {args.hands} hands, seed {args.seed}')
    for seat_count in range(MIN_SEATS, MAX_SEATS + 1):
        print(f'  {seat_count:2} seats: {seat_counts[seat_count]} hands')
    print(f'  hands with an all-in: {all_in_hands}')
    print(f"  hands tagged 'incomplete-all-in': {incomplete_hands}")
    print(f"  hands tagged 'odd-chip': {odd_chip_hands}")
    print(f"  hands tagged with both: {both_hands}")
    if args.antes != 'none':
        print(f'  hands with a seat all in on its ante: {ante_all_in_hands}')
        print(f'  hands with a side pot: {side_pot_hands}')
        print(f"  hands tagged '{BIG_BLIND_ANTE_TAG}': {ante_order_hands}")
        print(f'  hands where a classic ante empties a seat before the blinds (not tagged): {classic_empties_hands}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
