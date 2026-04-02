using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using static Ace.Extensions;

namespace Ace
{
    /// <summary>
    /// Maps each card to its corresponding child node in the tree.
    /// </summary>
    using Children = ConcurrentDictionary<Card, Node>;

    /// <summary>
    /// Represents a node in the tree, storing statistics and its children.
    /// </summary>
    internal sealed partial class Node
    {
        private int _wins = 0;
        private int _depth = 0;
        private int _level = 0;
        private int _vloss = 0;
        private int _visits = 0;
        private long _avails = 1u;
        private long _tricks = 0u;

        private Node _parent = null;
        private readonly Side _side;
        private readonly Card _action;
        private readonly Children _children;

        /// <summary>
        /// Gets the partnership perspective (0 = NS, 1 = EW).
        /// </summary>
        internal Side Side => this._side;

        /// <summary>
        /// Gets the action that led to this node from its parent.
        /// </summary>
        internal Card Action => this._action;

        /// <summary>
        /// Gets the fixed depth level of this node in the tree.
        /// </summary>
        internal int Level => this._level;

        /// <summary>
        /// Gets the child nodes representing all moves played from this node.
        /// </summary>
        internal IReadOnlyDictionary<Card, Node> Children => this._children;

        /// <summary>
        /// Gets the number of times this node was available for selection.
        /// </summary>
        internal long Avails => Interlocked.Read(ref this._avails);

        /// <summary>
        /// Gets the maximum depth level of this node in the tree.
        /// </summary>
        internal int Depth => Volatile.Read(ref this._depth);

        /// <summary>
        /// Gets the number of times this node has been visited.
        /// </summary>
        internal int Visits => Volatile.Read(ref this._visits);

        /// <summary>
        /// Gets the current virtual-loss count applied to this node.
        /// </summary>
        internal int VLoss => Volatile.Read(ref this._vloss);

        /// <summary>
        /// Gets the number of winnings recorded for this node.
        /// </summary>
        internal int Winnings => Volatile.Read(ref this._wins);

        /// <summary>
        /// Gets the total number of tricks accumulated at this node.
        /// </summary>
        internal long Tricks => Interlocked.Read(ref this._tricks);

        /// <summary>
        /// Gets the parent node of this node in the tree.
        /// </summary>
        internal Node Parent => Volatile.Read(ref this._parent);

        /// <summary>
        /// Creates a new root as the starting point of the tree.
        /// </summary>
        internal Node()
        {
            this._side = default;
            this._action = default;
            this._children = new Children();
        }

        /// <summary>
        /// Creates a new child node with the given parent and action.
        /// </summary>
        /// <param name="parent">Parent node in the tree.</param>
        /// <param name="action">Action that was played.</param>
        /// <param name="side">Partnership perspective.</param>
        internal Node(Node parent, Card action, Side side)
        {
            this._side = side;
            this._action = action;
            this._parent = parent;
            this._level = parent._level + 1;
            this._children = new Children();
        }

        /// <summary>
        /// Increments the availability count for this node.
        /// </summary>
        internal void AddAvailability()
        {
            Interlocked.Increment(ref this._avails);
        }

        /// <summary>
        /// Creates a child node for the action taken at this node.
        /// </summary>
        /// <param name="action">Action that was played.</param>
        /// <param name="side">Partnership perspective.</param>
        internal Node AddChild(in Card action, Side side)
        {
            Node factory(Card card) => new Node(this, card, side);
            return this._children.GetOrAdd(action, factory);
        }

        /// <summary>
        /// Increments the visit count for this node.
        /// </summary>
        internal void AddVisit()
        {
            Interlocked.Increment(ref this._visits);
        }

        /// <summary>
        /// Applies one unit of virtual loss to this node.
        /// </summary>
        internal void ApplyLoss()
        {
            Interlocked.Add(ref this._vloss, 1);
        }

        /// <summary>
        /// Detaches this node and shifts its entire subtree upward.
        /// </summary>
        internal void Detach()
        {
            this._parent = null;
            this.ShiftUp();
        }

        /// <summary>
        /// Records an evaluation result (win and tricks) at this node.
        /// </summary>
        /// <param name="winning">True if this result is a win.</param>
        /// <param name="tricks">Number of extra tricks obtained.</param>
        internal void Insert(bool winning, int tricks)
        {
            Interlocked.Add(ref this._tricks, tricks);
            if (winning) Interlocked.Increment(ref this._wins);
        }

        /// <summary>
        /// Removes one unit of previously applied virtual loss.
        /// </summary>
        internal void RevertLoss()
        {
            Interlocked.Add(ref this._vloss, -1);
        }

        /// <summary>
        /// Updates the maximum depth of this node within the tree.
        /// </summary>
        /// <param name="depth">The new depth to assign.</param>
        internal void SetDepth(int depth)
        {
            Maximum(ref this._depth, Math.Max(1, depth));
        }

        /// <summary>
        /// Recursively shifts subtree levels and depths up by one.
        /// </summary>
        internal void ShiftUp()
        {
            // Move node one level closer to the root
            this._level = Math.Max(0, this._level - 1);
            this._depth = Math.Max(0, this._depth - 1);

            // Apply this shift to every child in subtree
            foreach (Node child in this._children.Values)
            {
                child.ShiftUp();
            }
        }

        /// <summary>
        /// Attempts to retrieve the child node for the specified action.
        /// </summary>
        /// <param name="action">Action played from this node.</param>
        /// <param name="node">Child node if found; otherwise, null.</param>
        /// <returns>True if the child node exists; otherwise, false.</returns>
        internal bool TryGet(in Card action, out Node node)
        {
            return this._children.TryGetValue(action, out node);
        }
    }

    internal sealed partial class Node
    {
        /// <summary>
        /// Computes the Upper Confidence Bound score for this node.
        /// </summary>
        /// <param name="exploration">Exploration constant.</param>
        /// <returns>The UCB score used to rank this node.</returns>
        internal double UcbScore(double exploration)
        {
            // Virtual loss prevents parallel selection
            double visits = this.Visits + this.VLoss;

            // Unvisited nodes should be tried at least once
            if (visits == 0d) return double.PositiveInfinity;

            // Compute probability of success
            double winrate = this.Winrate();

            // Adjust score for any extra tricks
            double utility = this.Utility(winrate);

            // Bonus encourages trying actions that are under-sampled
            double bonus = Math.Sqrt(Math.Log(this.Avails) / visits);

            // Final score: exploitation + exploration term
            return winrate + utility + exploration * bonus;
        }

        /// <summary>
        /// Returns a small bonus based on extra tricks in certain outcomes.
        /// </summary>
        /// <param name="winrate">Estimated probability of success.</param>
        /// <param name="weight">Strength of the score adjustment.</param>
        /// <returns>A bonus from tricks when outcome is certain.</returns>
        internal double Utility(double winrate, double weight = 0.01d)
        {
            // Mean score: positive for overtricks, negative for undertricks
            double bonus = weight * this.Tricks / (this.Visits + this.VLoss);

            // Only apply bonus when the outcome is effectively certain
            return (winrate < 1e-9 || winrate > 1d - 1e-9) ? bonus : 0d;
        }

        /// <summary>
        /// Calculates the value estimate of this node based on results.
        /// </summary>
        /// <param name="weight">Strength of the score adjustment.</param>
        /// <returns>A value representing the score of this node.</returns>
        internal double Value(double weight = 0.1d)
        {
            double visits = this.Visits;
            if (visits == 0d) return 0d;

            // Compute empirical components of this node
            double tricks = (double)this.Tricks / visits;
            double winrate = (double)this.Winnings / visits;

            // Boost score if simulations show consistent success
            if (winrate > 1d - 1e-9) return 1d + weight * tricks;

            // Penalize score for clearly unsuccessful outcomes
            return winrate < 1e-9 ? weight * tricks : winrate;
        }

        /// <summary>
        /// Computes the empirical win rate of this node.
        /// </summary>
        /// <returns>The fraction of all games won.</returns>
        internal double Winrate()
        {
            // Virtual loss prevents parallel selection
            double visits = this.Visits + this.VLoss;

            // No simulation outcomes yet
            if (visits == 0d) return 0d;

            // Compute Bernoulli win probability
            return (double)this.Winnings / visits;
        }
    }
}
