using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Ace
{
    /// <summary>
    /// Maps each card to its corresponding edge in the tree.
    /// </summary>
    using Edges = ConcurrentDictionary<Card, Edge>;

    /// <summary>
    /// Associates a unique info-state key with its corresponding node.
    /// </summary>
    using States = ConcurrentDictionary<Key, Node>;

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
        internal Node GetOrCreate(Key key, Role role)
        {
            Node factory(Key _) => new Node(role);
            return this._states.GetOrAdd(key, factory);
        }
    }

    /// <summary>
    /// Represents a single action edge from an info-state node.
    /// </summary>
    internal sealed class Edge
    {
        private long _total;
        private readonly ConcurrentDictionary<Node, int> _counts;

        /// <summary>
        /// Creates a new empty edge with no observed successors yet.
        /// </summary>
        internal Edge()
        {
            this._counts = new ConcurrentDictionary<Node, int>();
        }

        /// <summary>
        /// Gets the number of successor nodes observed for this action.
        /// </summary>
        internal int Count => this._counts.Count;

        /// <summary>
        /// Gets the total number of observed successors for this action.
        /// </summary>
        internal long Total => Interlocked.Read(ref this._total);

        /// <summary>
        /// Records one observed transition outcome to a child node.
        /// </summary>
        /// <param name="child">Resulting node for this action.</param>
        internal void Update(in Node child)
        {
            // Update transition ending in this child
            int factory(Node _, int count) => count + 1;
            this._counts.AddOrUpdate(child, 1, factory);

            // Increment times this action was taken
            Interlocked.Increment(ref this._total);
        }

        /// <summary>
        /// Returns the probability distribution over successor nodes for this action.
        /// </summary>
        /// <param name="prior">Additive smoothing applied to observed successors.</param>
        /// <returns>A sequence representing the probability for each child node.</returns>
        internal IEnumerable<(Node child, double probability)> Dynamics(double prior = 0d)
        {
            // Freeze counts so enumeration is stable
            var snapshot = this._counts.ToArray();

            // No observed successors yet
            int length = snapshot.Length;
            if (length == 0) yield break;

            // Get number of transitions for this action
            long total = Interlocked.Read(ref this._total);
            if (total <= 0) yield break;

            // Include normalization for smoothing
            double denom = total + prior * length;
            if (denom <= 0) yield break;

            // Yield each successor with its probability
            foreach (var (child, count) in snapshot)
            {
                double prob = (count + prior) / denom;
                yield return (child, prob);
            }
        }
    }

    /// <summary>
    /// Represents a node in the tree, storing statistics and outgoing action edges.
    /// </summary>
    internal sealed partial class Node
    {
        private readonly Role _role;
        private readonly Edges _edges;
        private long _evals, _tricks, _wins;

        /// <summary>
        /// Gets the player role (perspective) this node represents.
        /// </summary>
        internal Role Role => this._role;

        /// <summary>
        /// Gets the outgoing edges for this node, keyed by the played card.
        /// </summary>
        internal IReadOnlyDictionary<Card, Edge> Edges => this._edges;

        /// <summary>
        /// Creates a node for the given player role.
        /// </summary>
        /// <param name="role">Player role.</param>
        internal Node(in Role role = Role.Self)
        {
            this._role = role;
            this._edges = new Edges();
        }

        /// <summary>
        /// Gets or creates a new edge for the given action.
        /// </summary>
        /// <param name="action">Action that was played.</param>
        /// <returns>Edge object associated with action.</returns>
        internal Edge AddEdge(in Card action)
        {
            return this._edges.GetOrAdd(action, _ => new Edge());
        }

        /// <summary>
        /// Connects this node to a successor through the given action.
        /// </summary>
        /// <param name="action">Action that was played.</param>
        /// <param name="child">Successor info-state node.</param>
        internal void Connect(in Card action, in Node child)
        {
            this.AddEdge(action).Update(child);
        }

        /// <summary>
        /// Records an evaluation result (win and tricks) at this node.
        /// </summary>
        /// <param name="win">True if this result is a win.</param>
        /// <param name="tricks">Number of tricks taken.</param>
        internal void Insert(bool win, int tricks)
        {
            Interlocked.Increment(ref _evals);
            Interlocked.Add(ref this._tricks, tricks);
            if (win) Interlocked.Increment(ref this._wins);
        }
    }

    internal sealed partial class Node
    {
        /// <summary>
        /// Gets the average number of tricks from all leaf simulations.
        /// </summary>
        internal double Tricks
        {
            get
            {
                long evals = Interlocked.Read(ref this._evals);
                long tricks = Interlocked.Read(ref this._tricks);
                return evals != 0 ? (double)tricks / evals : 0d;
            }
        }

        /// <summary>
        /// Gets the average win rate from all leaf simulations.
        /// </summary>
        internal double Winrate
        {
            get
            {
                long evals = Interlocked.Read(ref this._evals);
                long wins = Interlocked.Read(ref this._wins);
                return evals != 0 ? (double)wins / evals : 0d;
            }
        }
    }
}
