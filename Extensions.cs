using System;
using System.Runtime.CompilerServices;

namespace Ace
{
    /// <summary>
    /// Utility extension methods and enums for bridge game.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Represents a player seat at the bridge table.
        /// </summary>
        public enum Player : int
        {
            North = 0,
            East  = 1,
            South = 2,
            West  = 3
        }

        /// <summary>
        /// Represents the suit of a card.
        /// </summary>
        public enum Suit : int
        {
            Clubs    = 0,
            Diamonds = 1,
            Hearts   = 2,
            Spades   = 3,
            NoTrump  = 4
        }

        /// <summary>
        /// Returns the next player in clockwise order.
        /// </summary>
        /// <param name="player">Current player.</param>
        /// <returns>Next player.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Player Next(this Player player)
        {
            return (Player)(((int)player + 1) % 4);
        }

        /// <summary>
        /// Returns the previous player in counter-clockwise order.
        /// </summary>
        /// <param name="player">Current player.</param>
        /// <returns>Previous player.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Player Prev(this Player player)
        {
            return (Player)(((int)player + 3) % 4);
        }

        /// <summary>
        /// Returns the player seated a given number of seats after the current player.
        /// </summary>
        /// <param name="player">Starting player.</param>
        /// <param name="steps">Number of seats to advance.</param>
        /// <returns>
        /// A player after advancing the specified number of seats.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Player Advance(this Player player, int steps)
        {
            return (Player)(((int)player + steps) % 4);
        }
    }
}
