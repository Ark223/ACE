using System.Collections.Generic;

namespace Ace
{
    /// <summary>
    /// Implements the information-set search to evaluate available moves.
    /// </summary>
    internal sealed class ISS
    {
        private readonly Tree _tree;
        private readonly Model _model;
        private readonly double _epsilon = 1e-9d;

        /// <summary>
        /// Initializes an evaluator with the given tree and opponent model.
        /// </summary>
        /// <param name="tree">Current game tree to evaluate.</param>
        /// <param name="model">Model for opponent evaluation.</param>
        internal ISS(in Tree tree, in Model model)
        {
            this._tree = tree;
            this._model = model;
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

            // Maximize score here
            if (node.Maximizing)
            {
                double best = double.NegativeInfinity;
                foreach (Node child in node.Children.Values)
                {
                    double value = this.Evaluate(child);
                    if (value > best) best = value;
                }
                return best;
            }

            // Use the opponent model to aggregate scores
            return this._model.Backup(node, this.Evaluate);
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
