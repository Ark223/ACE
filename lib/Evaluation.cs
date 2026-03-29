using System;
using static Ace.Extensions;

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
        /// The lower confidence bound associated with the move.
        /// </summary>
        public double Lcb { get; }

        /// <summary>
        /// The metric used to rank moves among candidate options.
        /// </summary>
        public Metric Metric { get; }

        /// <summary>
        /// Initializes a new <see cref="Evaluation"/> from the specified node.
        /// </summary>
        /// <param name="node">Node providing the move and statistics.</param>
        /// <param name="config">Configuration settings for evaluation.</param>
        internal Evaluation(in Node node, in Config config)
        {
            this.Move = node.Action;
            this.Value = node.Value();
            this.Visits = node.Visits;
            this.Depth = node.Depth;

            double constant = config.Exploration;
            this.Lcb = node.LcbScore(constant);
            this.Metric = config.Metric;
        }

        /// <summary>
        /// Produces a sortable key used to rank multiple evaluations.
        /// </summary>
        /// <param name="eval">The evaluation to build a key for.</param>
        /// <returns>A tuple used for lexicographical comparison.</returns>
        private static (int, double, double) Key(in Evaluation eval)
        {
            // Sort results directly by value
            if (eval.Metric == Metric.Value)
                return (0, 0d, -eval.Value);

            // Determine which confidence metric to use
            bool visits = eval.Metric == Metric.Visits;
            double score = visits ? eval.Visits : eval.Lcb;

            // Certain win: top priority, higher value comes first
            if (eval.Value >= 1d) return (0, -eval.Value, -score);

            // Certain loss: low priority, but keep value ordering
            if (eval.Value <= 0d) return (2, -eval.Value, -score);

            // Prefer the most confident play
            return (1, -score, -eval.Value);
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
            return Key(this).CompareTo(Key(other));
        }
    }
}
