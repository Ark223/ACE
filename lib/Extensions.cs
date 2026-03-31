using System.Runtime.CompilerServices;
using System.Threading;

namespace Ace
{
    /// <summary>
    /// Utility extension methods and enums for library.
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
        /// Represents a partnership at the bridge table.
        /// </summary>
        public enum Side : int
        {
            NorthSouth = 0,
            EastWest   = 1
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
        /// <returns>Next player to the left.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Player Next(this Player player)
        {
            return (Player)(((int)player + 1) % 4);
        }

        /// <summary>
        /// Returns the previous player in counter-clockwise order.
        /// </summary>
        /// <param name="player">Current player.</param>
        /// <returns>Previous player to the right.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Player Prev(this Player player)
        {
            return (Player)(((int)player + 3) % 4);
        }

        /// <summary>
        /// Returns the player a given number of seats after the current player.
        /// </summary>
        /// <param name="player">Player to start from.</param>
        /// <param name="steps">Number of seats to advance.</param>
        /// <returns>
        /// A player after advancing the specified number of seats.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Player Advance(this Player player, int steps)
        {
            return (Player)(((int)player + steps) % 4);
        }

        /// <summary>
        /// Returns the partnership the specified player belongs to.
        /// </summary>
        /// <param name="player">The player to evaluate.</param>
        /// <returns>The side corresponding to the given player.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Side ToSide(this Player player)
        {
            return (Side)((int)player & 1);
        }

        /// <summary>
        /// Atomically updates the target to the maximum value.
        /// </summary>
        /// <param name="target">The value to update.</param>
        /// <param name="value">The candidate maximum value.</param>
        /// <returns>The resulting value after the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Maximum(ref int target, int value)
        {
            int current = target;
            while (current < value)
            {
                // Try to set target if it was unchanged
                int prior = Interlocked.CompareExchange(
                    ref target, value, current);

                // Return or retry with updated one
                if (prior == current) return value;
                current = prior;
            }
            return current;
        }
    }
}
