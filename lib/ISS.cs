using System.Collections.Generic;

namespace Ace
{
    /// <summary>
    /// Implements the information-set search to evaluate available moves.
    /// </summary>
    internal sealed class ISS
    {
        private readonly Tree _tree;
        private readonly Model _opponent, _partner;
        private readonly double _epsilon = 1e-9d;

        /// <summary>
        /// Initializes an evaluator with the tree and evaluation models.
        /// </summary>
        /// <param name="tree">Current game tree to evaluate.</param>
        /// <param name="opponent">Evaluation model for opponents.</param>
        /// <param name="partner">Evaluation model for our partner.</param>
        internal ISS(in Tree tree, in Model opponent, in Model partner)
        {
            this._tree = tree;
            this._opponent = opponent;
            this._partner = partner;
        }

        /// <summary>
        /// Recursively evaluates a subtree from the specified node.
        /// </summary>
        /// <param name="node">Current node to evaluate.</param>
        /// <returns>An evaluation score for this node.</returns>
        private double Evaluate(in Node node)
        {
            // Return leaf's static score
            if (node.Children.Count == 0)
            {
                return this.Score(node);
            }

            // Handle our turn case
            if (node.Role == Role.Self)
            {
                // We always try to maximize our outcome
                double best = double.NegativeInfinity;
                foreach (Node child in node.Children.Values)
                {
                    double value = this.Evaluate(child);
                    if (value > best) best = value;
                }
                return best;
            }

            // Partner's turn (defender or dummy)
            else if (node.Role == Role.Partner)
            {
                // This model will maximize if partner is a dummy
                return this._partner.Backup(node, this.Evaluate);
            }

            // Opponents typically try to minimize our result
            return this._opponent.Backup(node, this.Evaluate);
        }

        /// <summary>
        /// Calculates the reward score for this leaf node.
        /// </summary>
        /// <param name="node">Leaf node to evaluate.</param>
        /// <returns>A computed score for this node.</returns>
        private double Score(in Node node, double weight = 1e-3d)
        {
            double winrate = node.Winrate;
            double ratio = node.Tricks / 13d;

            // Penalize reward if losing
            if (winrate < this._epsilon)
                return -weight * (1d - ratio);

            // Boost reward for guaranteed win
            if (winrate > 1d - this._epsilon)
                return 1d + weight * ratio;

            // Standard reward
            return winrate;
        }

        /// <summary>
        /// Returns the evaluation results for each move available from player.
        /// </summary>
        /// <returns>A mapping from each card to its evaluation result.</returns>
        public Dictionary<Card, double> Solve()
        {
            var results = new Dictionary<Card, double>();
            foreach (var entry in this._tree.Root.Children)
            {
                results.Add(entry.Key, this.Evaluate(entry.Value));
            }
            return results;
        }
    }
}
