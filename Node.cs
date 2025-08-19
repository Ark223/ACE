using System.Collections.Generic;

namespace Ace
{
    /// <summary>
    /// Represents a state tree for caching and retrieving nodes by infostate key.
    /// </summary>
    internal sealed class Tree
    {
        private readonly Node _root;
        private readonly Dictionary<string, Node> _states;

        /// <summary>
        /// Gets the root node of the tree.
        /// </summary>
        internal Node Root => this._root;

        /// <summary>
        /// Initializes a new tree with a root node.
        /// </summary>
        internal Tree()
        {
            this._root = new Node();
            this._states = new Dictionary<string, Node>();
        }

        /// <summary>
        /// Gets or creates the node associated with the given key.
        /// </summary>
        /// <param name="key">Unique info-state key.</param>
        /// <returns>A node associated with the key.</returns>
        internal Node GetOrCreate(string key)
        {
            // Return the initial info-state if key is empty
            if (string.IsNullOrEmpty(key)) return this._root;

            // Create a new node if this key does not exist
            if (!this._states.TryGetValue(key, out Node node))
            {
                this._states.Add(key, node = new Node());
            }
            return node;
        }

        /// <summary>
        /// Checks if the given key exists in the tree.
        /// </summary>
        /// <param name="key">Unique info-state key.</param>
        /// <returns>True whether the node exists.</returns>
        internal bool Contains(string key)
        {
            return string.IsNullOrEmpty(key) || this._states.ContainsKey(key);
        }
    }

    /// <summary>
    /// Represents a node in the tree, storing statistics and child nodes for moves.
    /// </summary>
    internal sealed class Node
    {
        private bool _leaf;
        private int _visits;

        private readonly List<int> _tricks;
        private readonly List<bool> _winnings;
        private readonly Dictionary<Card, Node> _children;

        /// <summary>
        /// Gets whether this node is terminal.
        /// </summary>
        internal bool IsLeaf => this._leaf;

        /// <summary>
        /// Gets the number of times this node has been visited.
        /// </summary>
        internal int Visits => this._visits;

        /// <summary>
        /// Initializes a new node with empty statistics and children.
        /// </summary>
        internal Node()
        {
            this._children = new Dictionary<Card, Node>();
            this._winnings = new List<bool>();
            this._tricks = new List<int>();
            this._leaf = false;
            this._visits = 0;
        }

        /// <summary>
        /// Adds a child node for the given card.
        /// </summary>
        /// <param name="card">Card played.</param>
        /// <param name="node">Child node.</param>
        internal void Add(Card card, Node node)
        {
            this._children.Add(card, node);
        }

        /// <summary>
        /// Checks if a child node exists for the specified card.
        /// </summary>
        /// <param name="card">Card played.</param>
        /// <returns>True whether the child exists.</returns>
        internal bool Contains(Card card)
        {
            return this._children.ContainsKey(card);
        }

        /// <summary>
        /// Gets the child node for the specified card.
        /// </summary>
        /// <param name="card">Card played.</param>
        /// <returns>A node for the given card.</returns>
        internal Node Get(Card card)
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
            this._tricks.Add(tricks);
            this._winnings.Add(win);
            this._leaf = true;
            this._visits++;
        }
    }
}
