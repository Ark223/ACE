using System;
using System.Collections.Generic;
using static Ace.Extensions;

namespace Ace
{
    /// <summary>
    /// Stores alias moves associated with a selected node for shared updates.
    /// </summary>
    using Aliases = Dictionary<Card, IReadOnlyList<Card>>;

    /// <summary>
    /// Performs an information-set Monte Carlo Tree Search for decision-making.
    /// </summary>
    internal sealed partial class MCTS
    {
        private Node _node;
        private int _depth;

        private readonly Deal _deal;
        private readonly Aliases _aliases;
        private readonly List<Node> _path;

        /// <summary>
        /// Sets up an MCTS search with a starting node and the current state.
        /// </summary>
        /// <param name="node">Root node of the search tree.</param>
        /// <param name="deal">Current world state to simulate from.</param>
        /// <param name="depth">Maximum depth to explore in the tree.</param>
        internal MCTS(in Node node, in Deal deal, int depth)
        {
            this._node = node;
            this._deal = deal;
            this._depth = depth;
            this._aliases = new Aliases();
            this._path = new List<Node>(64);
        }

        /// <summary>
        /// Advances the search position to the given node for further traversal.
        /// </summary>
        /// <param name="node">Node to continue path traversal from.</param>
        private void Traverse(in Node node)
        {
            this._node = node;
            this._node.ApplyLoss();
            this._path.Add(this._node);
        }

        /// <summary>
        /// Finds the nearest opposing ranks that bound aliases for the best move.
        /// </summary>
        /// <param name="best">Move used as the center of the alias search.</param>
        /// <returns>The closest lower and upper ranks in the same suit.</returns>
        private (int lower, int upper) GetBounds(in Card best)
        {
            int lower = 1, upper = 15;
            for (int seat = 0; seat < 4; seat++)
            {
                // Consider cards from other players' hands
                if (seat == (int)this._deal.Leader) continue;
                foreach (Card card in this._deal.Hands[seat])
                {
                    // Only cards in the same suit matter
                    if (card.Suit != best.Suit) continue;

                    // Track the closest blockers
                    if (card.Rank > best.Rank)
                    {
                        // Higher cards stop aliases above
                        upper = Math.Min(upper, card.Rank);
                    }
                    else if (card.Rank < best.Rank)
                    {
                        // Lower cards stop aliases below
                        lower = Math.Max(lower, card.Rank);
                    }
                }
            }
            return (lower, upper);
        }

        /// <summary>
        /// Finds legal moves in the same suit between the cards from other hands.
        /// </summary>
        /// <param name="best">Move used as the center of the alias search.</param>
        /// <returns>Legal moves considered aliases of the selected move.</returns>
        private IReadOnlyList<Card> GetAliases(in Card best)
        {
            var aliases = new List<Card>(12);
            var (lower, upper) = this.GetBounds(best);

            // Check our legal moves against bounds
            foreach (Card card in this._deal.Moves)
            {
                // Only cards in the same suit matter
                if (card.Suit != best.Suit) continue;

                // Keep moves between top one and upper blocker
                if (card.Rank > best.Rank && card.Rank < upper)
                {
                    aliases.Add(card);
                    continue;
                }

                // Keep moves between lower blocker and top one
                if (card.Rank < best.Rank && card.Rank > lower)
                {
                    aliases.Add(card);
                }
            }
            return aliases;
        }

        /// <summary>
        /// Scans all legal actions to detect unexpanded moves and select the best node.
        /// </summary>
        /// <param name="exploration">Exploration constant used for UCB scoring.</param>
        /// <returns>True if legal actions are already expanded; otherwise, false.</returns>
        private bool Scan(double exploration)
        {
            bool expanded = true;

            // Stop the search if this is a terminal state
            if (this._deal.Moves.Count == 0) return false;

            // Track the best node found during selection
            (Node node, double score) best = (null, -1d);

            // Iterate through all legal candidates
            foreach (Card action in this._deal.Moves)
            {
                // Check whether this action is already expanded
                if (this._node.TryGet(action, out Node child))
                {
                    // Score this node with UCB and update best
                    double score = child.UcbScore(exploration);
                    if (score > best.score) best = (child, score);
                }

                // Note that expansion remains possible
                else { expanded = false; continue; }

                // Mark node as available
                child.AddAvailability();
            }

            // Proceed only if node was expanded
            if (expanded && best.node != null)
            {
                // Read action from selected node
                Card action = best.node.Action;

                // Cache all aliases for later updates
                var aliases = this.GetAliases(action);
                this._aliases[action] = aliases;

                // Apply the selected node
                this.Traverse(best.node);
            }
            return expanded;
        }

        /// <summary>
        /// Applies outcome to the sibling nodes corresponding to equivalent actions.
        /// </summary>
        /// <param name="node">Node whose aliases should receive the same update.</param>
        /// <param name="outcome">Outcome to propagate to equivalent actions.</param>
        private void Update(in Node node, in Outcome outcome)
        {
            // Skip root which has no aliases
            if (node.Parent == null) return;

            // Skip alias updates if this move has no equivalent actions
            if (!this._aliases.TryGetValue(node.Action, out var aliases))
            {
                return;
            }

            // Update all equivalent siblings
            foreach (Card alias in aliases)
            {
                if (node.Parent.TryGet(alias, out Node sibling))
                {
                    sibling.Insert(outcome.IsWin, outcome.Tricks);
                    sibling.AddVisit();
                }
            }
        }
    }

    internal sealed partial class MCTS
    {
        /// <summary>
        /// Selects the best node down the tree until depth ends or a move is unexpanded.
        /// </summary>
        /// <param name="config">Parameters controlling selection and tree growth.</param>
        internal void Select(in Config config)
        {
            while (this._depth > 0)
            {
                // Stop traversal if node is not expanded
                if (!this.Scan(config.Exploration)) break;

                // Apply selected action and advance
                this._deal.Play(this._node.Action);
                this._depth--;

                // Stop at trick boundary if depth is too low
                bool limit = config.Limiter && this._depth < 4;
                if (limit && !this._deal.Trick.Any()) break;
            }
        }

        /// <summary>
        /// Expands the tree by creating nodes for all legal moves not yet included.
        /// </summary>
        /// <returns>A world state after the selection and expansion phase.</returns>
        internal Deal Expand()
        {
            Side side = this._deal.Leader.ToSide();
            foreach (Card action in this._deal.Moves)
            {
                // Register nodes for each action
                this._node.AddChild(action, side);
            }
            return this._deal;
        }

        /// <summary>
        /// Updates action nodes with simulation results and returns the best outcome.
        /// </summary>
        /// <param name="results">Simulation outcomes for each legal action.</param>
        internal Outcome Simulate(in IReadOnlyDictionary<Card, Outcome> results)
        {
            // Get terminal result when no actions remain
            bool terminal = this._deal.Moves.Count == 0;
            if (terminal) return results[Card.None];

            // Start with the worst possible result
            Outcome best = new Outcome(false, -1, 0);

            // Apply outcomes to all legal actions
            foreach (Card action in this._deal.Moves)
            {
                // Retrieve child node for this action
                this._node.TryGet(action, out Node child);

                // Normalize and insert result to the linked node
                var result = results[action].Normalize(child.Side);
                child.Insert(result.IsWin, result.Tricks);

                // Keep the best outcome
                best.Maximize(result);
                child.AddVisit();
            }
            return best;
        }

        /// <summary>
        /// Propagates the simulated outcome up the tree and updates node statistics.
        /// </summary>
        /// <param name="result">Outcome to backpropagate through the tree.</param>
        internal void Backpropagate(Outcome result)
        {
            try
            {
                int level = this._node.Level;
                while (this._node != null)
                {
                    // Normalize and apply result to this node
                    result = result.Normalize(this._node.Side);
                    this._node.Insert(result.IsWin, result.Tricks);

                    // Propagate it to sibling nodes
                    this.Update(this._node, result);

                    // Mark this node as reached
                    this._node.SetDepth(level);
                    this._node.AddVisit();

                    // Traverse upward to parent node
                    this._node = this._node.Parent;
                }
            }
            finally
            {
                // Revert virtual losses applied in traversal
                this._path.ForEach(node => node.RevertLoss());
                this._path.Clear();
            }
        }
    }
}
