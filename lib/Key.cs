using System;

namespace Ace
{
    /// <summary>
    /// Represents a key that identifies an information set.
    /// </summary>
    internal readonly struct Key : IEquatable<Key>
    {
        private readonly uint _high;
        private readonly ulong _low;

        /// <summary>
        /// Initializes a new infoset key from explicit parts.
        /// </summary>
        /// <param name="high">The high 32 bits.</param>
        /// <param name="low">The low 64 bits.</param>
        internal Key(uint high, ulong low)
        {
            this._high = high;
            this._low = low;
        }

        /// <summary>
        /// Gets the additive identity (all bits set to zero).
        /// </summary>
        internal static Key Zero
        {
            get { return new Key(0u, 0ul); }
        }

        /// <summary>
        /// Returns a string representation of the key value.
        /// </summary>
        /// <returns>A key formatted as hexadecimal.</returns>
        public override string ToString()
        {
            return $"0x{this._high:x8}{this._low:x16}";
        }

        /// <summary>
        /// Checks value equality with another <see cref="Key"/>.
        /// </summary>
        /// <param name="other">Other key to compare.</param>
        /// <returns>True if both keys are the same.</returns>
        public bool Equals(Key other)
        {
            return this._high == other._high
                && this._low == other._low;
        }

        /// <summary>
        /// Checks value equality with another object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Key other && this.Equals(other);
        }

        /// <summary>
        /// Returns a combined hash code for this key.
        /// </summary>
        /// <returns>A hash code for the key value.</returns>
        public override int GetHashCode()
        {
            int high = this._high.GetHashCode();
            return high ^ this._low.GetHashCode();
        }
    }
}
