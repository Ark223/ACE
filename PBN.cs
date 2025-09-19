using static Ace.Extensions;

namespace Ace
{
    /// <summary>
    /// Utility for parsing and formatting hands in PBN (Portable Bridge Notation) format.
    /// </summary>
    public sealed class PBN
    {
        /// <summary>
        /// The order of suits in PBN format: Spades, Hearts, Diamonds, Clubs.
        /// </summary>
        public static readonly Suit[] PbnOrder =
        {
            Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs
        };

        /// <summary>
        /// Parses a PBN deal into an array of bitmasks.
        /// </summary>
        /// <param name="pbn">A string with hands in PBN format, separated by spaces.</param>
        /// <returns>An array of values, each representing a hand as a 52-bit mask.</returns>
        internal static ulong[] ParseDeal(string pbn)
        {
            var hands = pbn.Split(' ');
            ulong[] result = new ulong[4];
            for (int seat = 0; seat < 4; seat++)
            {
                result[seat] = ParseHand(hands[seat]);
            }
            return result;
        }

        /// <summary>
        /// Parses a single PBN hand (e.g. "AKQ.JT9.876.5432") into a 52-bit mask.
        /// </summary>
        /// <param name="hand">A string describing a single hand in PBN format.</param>
        /// <returns>A 52-bit mask where each bit represents a card in the hand.</returns>
        private static ulong ParseHand(string hand)
        {
            ulong mask = 0ul;
            if (string.IsNullOrEmpty(hand) || hand == "...")
            {
                return mask;
            }
            var suits = hand.Split('.');
            for (int suit = 0; suit < 4; suit++)
            {
                foreach (char rank in suits[suit])
                {
                    mask |= 1ul << ((int)PbnOrder[suit] *
                        13 + Card.RankFromChar[rank] - 2);
                }
            }
            return mask;
        }
    }
}
