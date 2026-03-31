using static Ace.Extensions;

namespace Ace
{
    /// <summary>
    /// Represents the evaluated result of a position from given perspective.
    /// </summary>
    internal struct Outcome
    {
        /// <summary>
        /// Gets whether this partnership achieves its objective.
        /// </summary>
        internal bool IsWin { get; private set; }

        /// <summary>
        /// Gets the number of tricks available to the player.
        /// </summary>
        internal int Tricks { get; private set; }

        /// <summary>
        /// Gets the playing side this outcome is expressed for.
        /// </summary>
        internal Side Side { get; private set; }

        /// <summary>
        /// Creates an outcome for the specified partnership side.
        /// </summary>
        /// <param name="winning">Whether this side is winning.</param>
        /// <param name="tricks">Number of tricks for this side.</param>
        /// <param name="side">Side this outcome belongs to.</param>
        internal Outcome(bool winning, int tricks, Side side)
        {
            this.IsWin = winning;
            this.Tricks = tricks;
            this.Side = side;
        }

        /// <summary>
        /// Returns this outcome from the opposite side's perspective.
        /// </summary>
        private Outcome Flip()
        {
            Side side = (Side)(1 ^ (int)this.Side);
            return new Outcome(!this.IsWin, 13 - this.Tricks, side);
        }

        /// <summary>
        /// Updates this outcome if the other one is better for any side.
        /// </summary>
        /// <param name="other">Other outcome to compare against.</param>
        internal void Maximize(in Outcome other)
        {
            this.Side = other.Side;

            // Always choose a winning result
            if (other.IsWin) this.IsWin = true;

            // Prefer outcome that guarantees a higher number of tricks
            if (other.Tricks > this.Tricks) this.Tricks = other.Tricks;
        }

        /// <summary>
        /// Returns this outcome from the requested side's perspective.
        /// </summary>
        /// <param name="side">Playing side to normalize to.</param>
        internal Outcome Normalize(Side side)
        {
            return this.Side == side ? this : this.Flip();
        }
    }
}
