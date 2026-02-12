using System.Collections.Generic;

namespace Ace
{
    /// <summary>
    /// Implements the information-set search to evaluate available moves.
    /// </summary>
    internal sealed class ISS
    {
        private readonly Tree _tree;
        private readonly double _prior;
        private readonly double _epsilon = 1e-9d;
        private readonly Dictionary<Node, double> _cache;

        /// <summary>
        /// Initializes an evaluator with the game tree.
        /// </summary>
        /// <param name="tree">Current game tree.</param>
        /// <param name="prior">Additive smoothing.</param>
        internal ISS(in Tree tree, double prior = 0d)
        {
            this._tree = tree;
            this._prior = prior;
            this._cache = new Dictionary<Node, double>();
        }

        /// <summary>
        /// Gets the root node of the current tree.
        /// </summary>
        internal Node Root => this._tree.Root;

        /// <summary>
        /// Calculates the expected value of a specified action.
        /// </summary>
        /// <param name="node">Current node to evaluate.</param>
        /// <param name="edge">Action edge with statistics.</param>
        /// <returns>Expected value of choosing action.</returns>
        private double Eval(in Node node, in Edge edge)
        {
            bool empty = true;
            double expected = 0d;

            // Get distribution for this action edge
            var dynamics = edge.Dynamics(this._prior);

            // Accumulate expected value over successors
            foreach (var (child, probability) in dynamics)
            {
                expected += probability * this.Value(child);
                empty = false;
            }

            // Fall back only if edge has no outcomes
            return !empty ? expected : this.Score(node);
        }

        /// <summary>
        /// Calculates the node value using the minimax rule.
        /// </summary>
        /// <param name="node">Current node to evaluate.</param>
        /// <returns>The backed-up value for this node.</returns>
        private double Value(in Node node)
        {
            // Use cached result if node was already evaluated
            if (this._cache.TryGetValue(node, out var cached))
            {
                return cached;
            }

            // Return leaf's static score
            if (node.Edges.Count == 0)
            {
                double score = this.Score(node);
                this._cache[node] = score;
                return score;
            }

            // Consider cases based on role
            if (node.Role == Role.Opponent)
            {
                // Assume opponents minimize our outcome
                double best = double.PositiveInfinity;
                foreach (Edge edge in node.Edges.Values)
                {
                    double value = this.Eval(node, edge);
                    if (value < best) best = value;
                }
                return best;
            }
            else
            {
                // Choose action to maximize our outcome
                double best = double.NegativeInfinity;
                foreach (Edge edge in node.Edges.Values)
                {
                    double value = this.Eval(node, edge);
                    if (value > best) best = value;
                }
                return best;
            }
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
        /// Returns the evaluation for each move available from player.
        /// </summary>
        /// <returns>A mapping from each card to its result.</returns>
        public Dictionary<Card, double> Solve()
        {
            var results = new Dictionary<Card, double>();
            foreach (var (move, edge) in this.Root.Edges)
            {
                results.Add(move, this.Eval(this.Root, edge));
            }
            return results;
        }
    }
}
