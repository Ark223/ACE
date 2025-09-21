using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static Ace.Extensions;

namespace Ace
{
    /// <summary>
    /// Represents a single playing card in a bridge game.
    /// </summary>
    public readonly struct Card : IEquatable<Card>, IComparable<Card>
    {
        public int Rank { get; }
        public Suit Suit { get; }

        /// <summary>
        /// Maps rank characters ('2', ..., 'A') to integer values (2–14).
        /// </summary>
        internal static readonly Dictionary<char, int> RankFromChar = new Dictionary<char, int>
        {
            ['2'] = 2,  ['3'] = 3,  ['4'] = 4,  ['5'] = 5,
            ['6'] = 6,  ['7'] = 7,  ['8'] = 8,  ['9'] = 9,
            ['T'] = 10, ['J'] = 11, ['Q'] = 12, ['K'] = 13, ['A'] = 14
        };

        /// <summary>
        /// Maps rank values (2–14) to rank characters ('2', ..., 'A').
        /// </summary>
        internal static readonly Dictionary<int, char> RankToChar = new Dictionary<int, char>
        {
            [2]  = '2', [3]  = '3', [4]  = '4', [5]  = '5',
            [6]  = '6', [7]  = '7', [8]  = '8', [9]  = '9',
            [10] = 'T', [11] = 'J', [12] = 'Q', [13] = 'K', [14] = 'A'
        };

        /// <summary>
        /// Maps suit characters ('C', 'D', 'H', 'S') to <see cref="Suit"/> values.
        /// </summary>
        internal static readonly Dictionary<char, Suit> SuitFromChar = new Dictionary<char, Suit>
        {
            ['C'] = Suit.Clubs,
            ['D'] = Suit.Diamonds,
            ['H'] = Suit.Hearts,
            ['S'] = Suit.Spades
        };

        /// <summary>
        /// Maps <see cref="Suit"/> values to suit characters ('C', 'D', 'H', 'S').
        /// </summary>
        internal static readonly Dictionary<Suit, char> SuitToChar = new Dictionary<Suit, char>
        {
            [Suit.Clubs]    = 'C',
            [Suit.Diamonds] = 'D',
            [Suit.Hearts]   = 'H',
            [Suit.Spades]   = 'S'
        };

        /// <summary>
        /// Creates a new card with the input rank and suit.
        /// </summary>
        /// <param name="rank">Card rank (2..14).</param>
        /// <param name="suit">Card suit.</param>
        public Card(int rank, Suit suit)
        {
            this.Rank = rank;
            this.Suit = suit;
        }

        /// <summary>
        /// Creates a <see cref="Card"/> from a zero-based index (0..51).
        /// </summary>
        /// <param name="index">Zero-based card index.</param>
        /// <returns>
        /// A <see cref="Card"/> corresponding to the specified index.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Card Create(int index)
        {
            return new Card((index % 13) + 2, (Suit)(index / 13));
        }

        /// <summary>
        /// Gets the high card point (HCP) value for this card (A=4, K=3, Q=2, J=1, others=0).
        /// </summary>
        /// <returns>High card point value of this card.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Hcp()
        {
            return Math.Max(0, this.Rank - 10);
        }

        /// <summary>
        /// Gets the unique zero-based index (0..51) for this card, based on suit and rank.
        /// <br></br>Spades: 0–12, Hearts: 13–25, Diamonds: 26–38, Clubs: 39–51.
        /// </summary>
        /// <returns>Zero-based index corresponding to this card.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Index()
        {
            return (int)this.Suit * 13 + this.Rank - 2;
        }

        /// <summary>
        /// Safely parses a string (like "7S", "KD", "TC") into a <see cref="Card"/>.
        /// </summary>
        /// <param name="str">String to parse ("7S", "KD", ...).</param>
        /// <param name="card">Parsed card if successful.</param>
        /// <returns>True if parse was successful; otherwise, false.</returns>
        public static bool TryParse(string str, out Card card)
        {
            card = default;

            // Reject null, empty, or too-short input strings
            if (string.IsNullOrEmpty(str) || str.Length != 2)
                return false;

            // Pull out the rank and suit as uppercase
            char ca = char.ToUpperInvariant(str[0]);
            char cb = char.ToUpperInvariant(str[1]);

            // See if the char actually maps to a valid rank
            if (!RankFromChar.TryGetValue(ca, out var rank))
                return false;

            // Same for the suit — reject if unable to parse
            if (!SuitFromChar.TryGetValue(cb, out var suit))
                return false;

            card = new Card(rank, suit);
            return true;
        }

        /// <summary>
        /// Returns a string representation of the card (e.g., "AS", "TH").
        /// </summary>
        /// <returns>A two-character string: rank followed by suit.</returns>
        public override string ToString()
        {
            char rank = RankToChar[this.Rank];
            char suit = SuitToChar[this.Suit];
            return $"{rank}{suit}";
        }

        /// <summary>
        /// Compares this card with another by rank only.
        /// </summary>
        /// <param name="other">Other card to compare.</param>
        /// <returns>
        /// Negative if lower rank, zero if equal, positive if higher.
        /// </returns>
        public int CompareTo(Card other)
        {
            return this.Rank.CompareTo(other.Rank);
        }

        /// <summary>
        /// Checks value equality with another <see cref="Card"/>.
        /// </summary>
        /// <param name="other">Other card to compare.</param>
        /// <returns>True if rank and suit are equal.</returns>
        public bool Equals(Card other)
        {
            return this.Rank == other.Rank
                && this.Suit == other.Suit;
        }

        /// <summary>
        /// Checks value equality with another object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Card card && this.Equals(card);
        }

        /// <summary>
        /// Returns a hash code for this card based on its unique deck index.
        /// </summary>
        /// <returns>A unique hash code for this card.</returns>
        public override int GetHashCode()
        {
            return this.Index();
        }
    }
}
