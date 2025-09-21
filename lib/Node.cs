using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ace
{
    /// <summary>
    /// Maps each card to its corresponding child node in the tree.
    /// </summary>
    using Children = ConcurrentDictionary<Card, Node>;

    /// <summary>
    /// Associates a unique info-state key with its corresponding node.
    /// </summary>
    using States = ConcurrentDictionary<uint, Node>;

    /// <summary>
    /// Represents the player side or perspective for a corresponding node.
    /// </summary>
    internal enum Role
    {
        /// <summary>
        /// Acting player (ourself, or the agent being evaluated).
        /// </summary>
        Self = 0,

        /// <summary>
        /// Our defender or dummy (acting cooperatively with us).
        /// </summary>
        Partner = 1,

        /// <summary>
        /// An opponent (competing against our partnership).
        /// </summary>
        Opponent = 2
    }

    /// <summary>
    /// Represents an info-state tree for caching and retrieving nodes by key.
    /// </summary>
    internal sealed class Tree
    {
        private readonly Node _root;
        private readonly States _states;

        /// <summary>
        /// Returns true if the tree has no stored nodes.
        /// </summary>
        internal bool IsEmpty => this._states.Count == 0;

        /// <summary>
        /// Gets the root node of the tree.
        /// </summary>
        internal Node Root => this._root;

        /// <summary>
        /// Initializes a new tree with a root node.
        /// </summary>
        internal Tree()
        {
            this._root = new Node(Role.Self);
            this._states = new States();
        }

        /// <summary>
        /// Gets or creates a node for the specified key and role.
        /// </summary>
        /// <param name="key">Unique info-state key.</param>
        /// <param name="role">Player role (perspective).</param>
        /// <returns>A node associated with provided key.</returns>
        internal Node GetOrCreate(uint key, Role role)
        {
            // Return the initial info-state
            if (key == 0u) return this._root;

            // Create new node if key does not exist
            Node factory(uint _) => new Node(role);
            return this._states.GetOrAdd(key, factory);
        }
    }

    /// <summary>
    /// Represents a node in the tree, storing statistics and child nodes for moves.
    /// </summary>
    internal sealed partial class Node
    {
        private long _tricks;
        private int _winnings;
        private int _visits;

        private readonly Role _role;
        private readonly Children _children;

        /// <summary>
        /// Gets the player role (perspective) this node represents.
        /// </summary>
        internal Role Role => this._role;

        /// <summary>
        /// Gets the number of times this node was visited during search.
        /// </summary>
        internal int Visits => Volatile.Read(ref this._visits);

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
                int visits = this.Visits;
                long tricks = Interlocked.Read(ref this._tricks);
                return visits != 0 ? (double)tricks / visits : 0d;
            }
        }

        /// <summary>
        /// Gets the average win rate from all leaf simulations.
        /// </summary>
        internal double Winrate
        {
            get
            {
                int visits = this.Visits;
                int winnings = Volatile.Read(ref this._winnings);
                return visits != 0 ? (double)winnings / visits : 0d;
            }
        }

        /// <summary>
        /// Creates a node for the given player role.
        /// </summary>
        /// <param name="role">Player role.</param>
        internal Node(in Role role = Role.Self)
        {
            this._role = role;
            this._children = new Children();
        }

        /// <summary>
        /// Adds a child node for the given card.
        /// </summary>
        /// <param name="card">Card played.</param>
        /// <param name="node">Child node.</param>
        internal void AddChild(in Card card, in Node node)
        {
            this._children.TryAdd(card, node);
        }

        /// <summary>
        /// Increments the visit count.
        /// </summary>
        internal void AddVisit()
        {
            Interlocked.Increment(ref this._visits);
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
        /// Records an evaluation result (win and tricks) at this node.
        /// </summary>
        /// <param name="win">True if this result is a win.</param>
        /// <param name="tricks">Number of tricks taken.</param>
        internal void Insert(bool win, int tricks)
        {
            Interlocked.Add(ref this._tricks, tricks);
            if (win) Interlocked.Increment(ref this._winnings);
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

            // Count how many times each child has been visited
            int visits = this._children.Values.Sum(c => c.Visits);

            // Work out the scaling factor so probabilities sum up to 1
            double scale = 1d / Math.Max(prior * childs + visits, childs);

            // Assign probability to each child node
            foreach (Node node in this._children.Values)
            {
                yield return (node, (node.Visits + prior) * scale);
            }
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
