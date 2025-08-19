using System;
using static Ace.Extensions;

namespace Ace
{
    /// <summary>
    /// Represents a state of the current trick.
    /// </summary>
    internal struct Trick
    {
        /// <summary>
        /// Cards currently played to the trick.
        /// </summary>
        internal Card[] Cards;

        /// <summary>
        /// Player who led (started) this trick.
        /// </summary>
        internal Player Leader;

        /// <summary>
        /// Number of cards played so far (0 to 4).
        /// </summary>
        internal byte Count;

        /// <summary>
        /// Initializes a new <see cref="Trick"/> with the specified leader.
        /// </summary>
        /// <param name="leader">Player who leads the trick.</param>
        internal Trick(Player leader)
        {
            this.Cards = new Card[4];
            this.Leader = leader;
            this.Count = 0;
        }

        /// <summary>
        /// Adds a next card to the current trick.
        /// </summary>
        /// <param name="card">Card to add.</param>
        internal void Insert(in Card card)
        {
            this.Cards[this.Count++] = card;
        }

        /// <summary>
        /// Creates a deep copy of the trick.
        /// </summary>
        /// <returns>A new <see cref="Trick"/> instance.</returns>
        internal Trick Copy()
        {
            Trick copy = new Trick(this.Leader);
            Array.Copy(this.Cards, copy.Cards, this.Count);
            copy.Count = this.Count;
            return copy;
        }
    }
}
