using System.Collections.Generic;

namespace Ace
{
    /// <summary>
    /// Represents an info-state tree for caching and retrieving nodes by key.
    /// </summary>
    internal sealed class Tree
    {
        private readonly Node _root;
        private readonly Dictionary<uint, Node> _states;

        /// <summary>
        /// Gets the root node of the tree.
        /// </summary>
        internal Node Root => this._root;

        /// <summary>
        /// Initializes a new tree with a root node.
        /// </summary>
        internal Tree()
        {
            this._root = new Node(true);
            this._states = new Dictionary<uint, Node>();
        }

        /// <summary>
        /// Gets or creates the node associated with the given key.
        /// </summary>
        /// <param name="key">Unique info-state key.</param>
        /// <param name="maximize">Whether node is a maximizer.</param>
        /// <returns>A node associated with the specified key.</returns>
        internal Node GetOrCreate(uint key, bool maximize)
        {
            // Return the initial info-state
            if (key == 0U) return this._root;

            // Create a new node if this key does not exist
            if (!this._states.TryGetValue(key, out Node node))
            {
                this._states.Add(key, node = new Node(maximize));
            }
            return node;
        }
    }

    /// <summary>
    /// Represents a node in the tree, storing statistics and child nodes for moves.
    /// </summary>
    internal sealed partial class Node
    {
        private ulong _tricks;
        private uint _edge_count;
        private uint _winnings;
        private uint _samples;
        private uint _visits;

        private readonly bool _maximizing;
        private readonly Dictionary<Card, Node> _children;
        private readonly Dictionary<Card, uint> _edge_visits;

        /// <summary>
        /// Gets the number of times this node was visited during search.
        /// </summary>
        internal uint Visits => this._visits;

        /// <summary>
        /// True if this node is set to maximize; false if it minimizes.
        /// </summary>
        internal bool Maximizing => this._maximizing;

        /// <summary>
        /// Gets reachable child nodes keyed by the card played.
        /// </summary>
        internal IReadOnlyDictionary<Card, Node> Children => this._children;

        /// <summary>
        /// Gets the average number of tricks from all leaf simulations.
        /// </summary>
        internal double Tricks
        {
            get
            {
                if (this._samples == 0U) return 0d;
                return (double)this._tricks / this._samples;
            }
        }

        /// <summary>
        /// Gets the average win rate from all leaf simulations.
        /// </summary>
        internal double Winrate
        {
            get
            {
                if (this._samples == 0U) return 0d;
                return (double)this._winnings / this._samples;
            }
        }

        /// <summary>
        /// Creates a node as either a maximizer or minimizer in the search tree.
        /// </summary>
        /// <param name="maximizing">Whether this node is maximizing.</param>
        internal Node(bool maximizing = true)
        {
            this._maximizing = maximizing;
            this._children = new Dictionary<Card, Node>();
            this._edge_visits = new Dictionary<Card, uint>();
        }

        /// <summary>
        /// Adds a child node for the given card.
        /// </summary>
        /// <param name="card">Card played.</param>
        /// <param name="node">Child node.</param>
        internal void AddChild(in Card card, in Node node)
        {
            this._children.Add(card, node);
        }

        /// <summary>
        /// Increments the visit count.
        /// </summary>
        internal void AddVisit()
        {
            this._visits++;
        }

        /// <summary>
        /// Checks if a child node exists for the specified card.
        /// </summary>
        /// <param name="card">Card played.</param>
        /// <returns>True whether the child exists.</returns>
        internal bool Contains(in Card card)
        {
            return this._children.ContainsKey(card);
        }

        /// <summary>
        /// Gets the child node for the specified card.
        /// </summary>
        /// <param name="card">Card played.</param>
        /// <returns>A node for the given card.</returns>
        internal Node GetChild(in Card card)
        {
            return this._children[card];
        }

        /// <summary>
        /// Get the current visit count for a move edge from this node.
        /// </summary>
        /// <param name="card">Card played.</param>
        /// <returns></returns>
        internal uint GetEdgeVisits(in Card card)
        {
            return this._edge_visits.TryGetValue(
                card, out uint count) ? count : 0U;
        }

        /// <summary>
        /// Records an evaluation result (win and tricks) at this node.
        /// </summary>
        /// <param name="win">True if this result is a win.</param>
        /// <param name="tricks">Number of tricks taken.</param>
        internal void Insert(bool win, byte tricks)
        {
            this._samples++;
            this._tricks += tricks;
            if (win) this._winnings++;
        }

        /// <summary>
        /// Returns the policy distribution over available moves from this node.
        /// </summary>
        /// <param name="prior">Smoothing factor for unseen or low-count moves.</param>
        /// <returns>A sequence of pairs representing the move distribution.</returns>
        internal IEnumerable<(Node child, double probability)> Policy(double prior)
        {
            int childs = this._children.Count;
            if (childs == 0) yield break;

            // Use uniform probability if no data
            double edges = this._edge_count;
            if (edges == 0d && prior == 0d)
            {
                double probability = 1d / childs;
                foreach (var child in this._children.Values)
                {
                    yield return (child, probability);
                }
                yield break;
            }

            // Calculate probability for each child node
            double scale = 1d / (edges + prior * childs);
            foreach (var entry in this._children)
            {
                double visits = this.GetEdgeVisits(entry.Key);
                double probability = (visits + prior) * scale;
                yield return (entry.Value, probability);
            }
        }

        /// <summary>
        /// Records that this card has been played from this node.
        /// </summary>
        /// <param name="card">Card played.</param>
        internal void Record(in Card card)
        {
            if (!this._edge_visits.TryAdd(card, 1U))
            {
                this._edge_visits[card]++;
            }
            this._edge_count++;
        }

        /// <summary>
        /// Attempts to get the child node for the specified card.
        /// </summary>
        /// <param name="card">Card played.</param>
        /// <param name="node">Child node if found; otherwise, null.</param>
        /// <returns>True if the child node exists; otherwise, false.</returns>
        internal bool TryGet(in Card card, out Node node)
        {
            return this._children.TryGetValue(card, out node);
        }
    }
}
