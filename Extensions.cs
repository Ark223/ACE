using System.Collections.Generic;

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
        public static Player Next(this Player player)
        {
            return (Player)(((int)player + 1) % 4);
        }

        /// <summary>
        /// Returns the previous player in counter-clockwise order.
        /// </summary>
        /// <param name="player">Current player.</param>
        /// <returns>Previous player.</returns>
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
        public static Player Advance(this Player player, int steps)
        {
            return (Player)(((int)player + steps) % 4);
        }

        /// <summary>
        /// Attempts to add the specified key and value to the dictionary (for environments before C# 8.0).
        /// </summary>
        /// <typeparam name="TKey">Type of the key.</typeparam>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="dict">Dictionary in which to insert the new key-value pair.</param>
        /// <param name="key">Key to add. If this key exists, the dictionary is unchanged.</param>
        /// <param name="value">Value to associate with the key if it is not already present.</param>
        /// <returns>True if the key and value were added; false if this key already exists.</returns>
        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, value);
                return true;
            }
            return false;
        }
    }
}
