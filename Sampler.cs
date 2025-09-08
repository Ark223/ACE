using System.Collections.Generic;
using System.Linq;
using static Ace.Extensions;

namespace Ace
{
    /// <summary>
    /// Supports generating and filtering random deals consistent with the current game state.
    /// </summary>
    internal sealed partial class Sampler
    {
        private byte[] _lefts;
        private byte[] _needed;
        private byte[][] _known;

        private readonly Game _game;
        private readonly ulong[] _hands;
        private readonly ulong[] _plays;

        /// <summary>
        /// Initializes a new <see cref="Sampler"/> based on the current game state.
        /// </summary>
        /// <param name="game">Current game state to sample possible deals from.</param>
        internal Sampler(in Game game)
        {
            this._game = game;
            this._hands = game.Hands.ToArray();
            this._plays = game.Plays.ToArray();
            this.UnplayTrick();
            this.Precompute();
        }
    }

    internal sealed partial class Sampler
    {
        /// <summary>
        /// Converts an array of card indices into a list of <see cref="Card"/> objects.
        /// </summary>
        /// <param name="hand">Hand as an array of card indices.</param>
        /// <returns>A new sequence containing the mapped cards.</returns>
        private List<Card> Assign(in byte[] hand)
        {
            var cards = new List<Card>(hand.Length);
            for (int idx = 0; idx < hand.Length; idx++)
            {
                cards.Add(Game.Deck[hand[idx]]);
            }
            return cards;
        }

        /// <summary>
        /// Converts a 52-bit bitmask into an array of card indexes (0..51).
        /// </summary>
        /// <param name="bitmask">Bitmask representing cards to be converted.</param>
        /// <returns>Array of card indexes extracted from the input bitmask.</returns>
        private byte[] Convert(ulong bitmask)
        {
            byte size = Utilities.PopCount(bitmask);
            List<byte> list = new List<byte>(size);
            while (bitmask != 0ul)
            {
                // Extract each set bit and store its index
                ulong bit = bitmask & (ulong)-(long)bitmask;
                list.Add(Utilities.TrailingZeroCount(bit));
                bitmask ^= bit;
            }
            return list.ToArray();
        }

        /// <summary>
        /// Precomputes baseline card distributions and missing card counts for each seat.
        /// </summary>
        private void Precompute()
        {
            this._needed = new byte[4];
            this._known = new byte[4][];
            for (int seat = 0; seat < 4; seat++)
            {
                // Combine held and played cards
                ulong assigned = this._hands[seat];
                assigned |= this._plays[seat];

                // Extract cards explicitly known
                var known = this.Convert(assigned);
                int count = known.Length;

                // Store cards and compute missing count
                this._needed[seat] = (byte)(13 - count);
                this._known[seat] = known;
            }

            // Transform hidden bitmask into unknown cards
            this._lefts = this.Convert(this._game.Hidden);
        }

        /// <summary>
        /// Restores all cards from the current trick to the bitmasks.
        /// </summary>
        private void UnplayTrick()
        {
            Trick trick = this._game.Trick;
            Player player = trick.Leader;
            for (int i = 0; i < trick.Count; i++)
            {
                ref Card card = ref trick.Cards[i];
                ulong bit = 1ul << card.Index();

                // Restore card to player's hand
                this._hands[(int)player] |= bit;

                // Remove card from played cards
                this._plays[(int)player] &= ~bit;

                // Advance to next player
                player = player.Next();
            }
        }
    }

    internal sealed partial class Sampler
    {
        /// <summary>
        /// Returns true if the specified deal meets all hand constraints for each player.
        /// </summary>
        /// <param name="deal">Full deal specifying all cards held by each player.</param>
        /// <returns>True if the deal satisfies all constraints; otherwise, false.</returns>
        internal bool Filter(in Deal deal)
        {
            var constraints = this._game.Constraints;
            for (int seat = 0; seat < 4; seat++)
            {
                ref var hand = ref deal.Hands[seat];
                var checks = constraints[(Player)seat];

                // Skip constraint check here
                if (!checks.Edited) continue;

                // Accumulate constaints
                int[] counts = new int[5];
                foreach (Card card in hand)
                {
                    counts[(int)card.Suit]++;
                    counts[4] += card.Hcp();
                }

                // Does the hand have required high card points?
                if (!checks.Hcp.Contains(counts[4])) return false;

                // Does the hand have required number of clubs?
                int clubs = counts.ElementAt((int)Suit.Clubs);
                if (!checks.Clubs.Contains(clubs)) return false;

                // Does the hand have required number of diamonds?
                int diamonds = counts.ElementAt((int)Suit.Diamonds);
                if (!checks.Diamonds.Contains(diamonds)) return false;

                // Does the hand have required number of hearts?
                int hearts = counts.ElementAt((int)Suit.Hearts);
                if (!checks.Hearts.Contains(hearts)) return false;

                // Does the hand have required number of spades?
                int spades = counts.ElementAt((int)Suit.Spades);
                if (!checks.Spades.Contains(spades)) return false;
            }
            return true;
        }

        /// <summary>
        /// Generates a random complete deal consistent with the current game state.
        /// </summary>
        /// <returns>
        /// A fully specified <see cref="Deal"/> with all cards allocated to player hands.
        /// </returns>
        internal Deal Generate()
        {
            Trick trick = this._game.Trick;
            var tricks = this._game.Tricks;
            ushort voids = this._game.Voids;

            // Copy remaining cards and shuffle
            var lefts = this.Assign(this._lefts);
            Random.Shuffle<Card>(lefts);

            // Initialize queue for dealing
            var pool = new Queue<Card>(lefts);
            var deal = new List<Card>[4];

            // True if seat is void in this suit
            bool Void(int seat, in Card card)
            {
                int bit = (seat << 2) | (int)card.Suit;
                return ((voids >> bit) & 1) != 0;
            }

            // Assign all cards to each player
            for (int seat = 0; seat < 4; seat++)
            {
                ref var hand = ref this._known[seat];
                deal[seat] = this.Assign(hand);

                // Fill hand until all hidden cards are assigned
                for (int need = this._needed[seat]; need-- > 0;)
                {
                    // Try each card in the pool at most once
                    for (int run = pool.Count; run-- > 0;)
                    {
                        // Draw random card from pool
                        Card card = pool.Dequeue();

                        // Check void restriction for this player
                        if (Void(seat, card)) pool.Enqueue(card);
                        else { deal[seat].Add(card); break; }
                    }
                }
            }

            // Create and return a fully specified deal
            Suit trump = this._game.Contract.Strain;
            return new Deal(deal, trick, trump, tricks);
        }

        /// <summary>
        /// Synchronizes a deal with the current game state.
        /// </summary>
        /// <param name="deal">Deal to update in-place.</param>
        /// <returns>Always true after update and replay.</returns>
        internal bool Synchronize(ref Deal deal)
        {
            // Remove all played cards
            deal.CutHands(this._plays);

            // Build PBN from this state
            deal.Pbn = deal.ToPBN();

            // Replay the current trick
            return deal.Replay();
        }
    }
}
