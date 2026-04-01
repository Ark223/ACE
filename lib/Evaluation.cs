using System;

namespace Ace
{
    /// <summary>
    /// Represents the evaluated result of a possible move in the search tree.
    /// </summary>
    public readonly struct Evaluation : IComparable<Evaluation>
    {
        /// <summary>
        /// The move (card) this evaluation represents.
        /// </summary>
        public Card Move { get; }

        /// <summary>
        /// The score value (win rate) associated with the move.
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// The number of times this move was visited during search.
        /// </summary>
        public int Visits { get; }

        /// <summary>
        /// How far ahead this particular move is in the search tree.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Initializes a new <see cref="Evaluation"/> from the specified node.
        /// </summary>
        /// <param name="node">Node providing the move and statistics.</param>
        internal Evaluation(in Node node)
        {
            this.Move = node.Action;
            this.Value = node.Value();
            this.Visits = node.Visits;
            this.Depth = node.Depth;
        }

        /// <summary>
        /// Returns a formatted string representation of the evaluation.
        /// </summary>
        /// <returns>A formatted string containing evaluation data.</returns>
        public override string ToString()
        {
            string pattern = "+0.000000;-0.000000;0.000000";
            string value = this.Value.ToString(pattern);

            // Format fields to keep all columns aligned
            return string.Format("{0,-6} {1,9} {2,8} {3,7}",
                this.Move, value, this.Visits, this.Depth);
        }

        /// <summary>
        /// Compares this evaluation with another evaluation.
        /// </summary>
        /// <param name="other">Other evaluation to compare.</param>
        /// <returns>
        /// Negative if lower rank, zero if equal, positive if higher.
        /// </returns>
        public int CompareTo(Evaluation other)
        {
            // Compare depth when visit counts are close
            if (Math.Abs(this.Visits - other.Visits) < 2)
            {
                return other.Depth.CompareTo(this.Depth);
            }

            // Otherwise prefer the most promising move
            return other.Visits.CompareTo(this.Visits);
        }
    }
}
