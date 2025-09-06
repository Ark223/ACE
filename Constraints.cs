using System;
using System.Collections.Generic;
using static Ace.Extensions;

namespace Ace
{
    /// <summary>
    /// Represents constraints on a bridge hand, such as HCP and suit length.
    /// </summary>
    public sealed class Constraints
    {
        private Range _hcp      = Range.Any(37);
        private Range _clubs    = Range.Any(13);
        private Range _diamonds = Range.Any(13);
        private Range _hearts   = Range.Any(13);
        private Range _spades   = Range.Any(13);

        /// <summary>
        /// True if any range has been changed.
        /// </summary>
        public bool Edited { get; private set; }

        /// <summary>
        /// Range for high card points.
        /// </summary>
        public Range Hcp
        {
            get => this._hcp;
            set => this.Set(ref this._hcp, value);
        }

        /// <summary>
        /// Range for clubs count.
        /// </summary>
        public Range Clubs
        {
            get => this._clubs;
            set => this.Set(ref this._clubs, value);
        }

        /// <summary>
        /// Range for diamonds count.
        /// </summary>
        public Range Diamonds
        {
            get => this._diamonds;
            set => this.Set(ref this._diamonds, value);
        }

        /// <summary>
        /// Range for hearts count.
        /// </summary>
        public Range Hearts
        {
            get => this._hearts;
            set => this.Set(ref this._hearts, value);
        }

        /// <summary>
        /// Range for spades count.
        /// </summary>
        public Range Spades
        {
            get => this._spades;
            set => this.Set(ref this._spades, value);
        }

        /// <summary>
        /// Updates a range property and marks this instance as edited.
        /// </summary>
        /// <param name="range">Reference to the backing range to update.</param>
        /// <param name="value">A new value to assign to the range.</param>
        private void Set(ref Range range, Range value)
        {
            range = value;
            this.Edited = true;
        }
    }

    /// <summary>
    /// A collection of <see cref="Constraints"/> indexed by <see cref="Player"/>.
    /// </summary>
    public sealed class ConstraintSet
    {
        private readonly Dictionary<Player, Constraints> _map;

        /// <summary>
        /// Creates a new <see cref="ConstraintSet"/> with default constraints for each player.
        /// </summary>
        public ConstraintSet()
        {
            this._map = new Dictionary<Player, Constraints>();
            foreach (Player player in Enum.GetValues(typeof(Player)))
            {
                this._map[player] = new Constraints();
            }
        }

        /// <summary>
        /// Gets the <see cref="Constraints"/> for a specific player.
        /// </summary>
        /// <param name="player">The player (North, East, South, West).</param>
        /// <returns>The <see cref="Constraints"/> for that player.</returns>
        public Constraints this[Player player]
        {
            get { return this._map[player]; }
        }

        /// <summary>
        /// Returns an empty <see cref="ConstraintSet"/> (with default constraints for all players).
        /// </summary>
        /// <returns>A new, unconstrained <see cref="ConstraintSet"/> instance.</returns>
        public static ConstraintSet Empty
        {
            get { return new ConstraintSet(); }
        }
    }
}
