using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ace
{
    /// <summary>
    /// Represents the core game-solving engine for analyzing bridge games.
    /// </summary>
    public sealed partial class Engine
    {
        private int _side;
        private Game _game;
        private Tree _tree;
        private Sampler _sampler;
        private TimeSpan _elapsed;
        private long _iterations;
        private readonly int _threads;
        private readonly object _lock;

        /// <summary>
        /// Gets the total elapsed time spent performing the search operation.
        /// </summary>
        public TimeSpan Elapsed { get { lock (this._lock) return this._elapsed; } }

        /// <summary>
        /// Gets the current count of search iterations that have been performed so far.
        /// </summary>
        public long Iterations => Interlocked.Read(ref this._iterations);

        /// <summary>
        /// Initializes a new game engine with the specified thread count.
        /// </summary>
        /// <param name="threads">Number of the search threads.</param>
        public Engine(int threads)
        {
            this._threads = threads;
            this._lock = new object();
            this._elapsed = TimeSpan.Zero;
        }

        /// <summary>
        /// Factory method for creating a new <see cref="Engine"/> instance.
        /// </summary>
        /// <param name="threads">Number of the search threads.</param>
        /// <returns>A new <see cref="Engine"/> instance.</returns>
        public static Engine New(int threads)
        {
            return new Engine(threads);
        }
    }

    public sealed partial class Engine
    {
        /// <summary>
        /// Executes the internal search process with the specified options.
        /// </summary>
        /// <param name="time">Total search duration, in milliseconds.</param>
        /// <param name="interval">Interval for periodic progress update.</param>
        /// <param name="depth">Maximum search depth per simulation.</param>
        private async Task Execute(uint time, uint interval, uint depth)
        {
            // To Do!
        }

        /// <summary>
        /// Evaluates whether the investigated partnership can achieve their objective:<br></br>
        /// either making the contract (declarer's side) or setting the contract (defenders).<br></br>
        /// Also returns the number of tricks available to the leader's partnership in this state.
        /// </summary>
        /// <param name="world">Deal state to evaluate, representing a possible game scenario.</param>
        /// <returns>True if the evaluated partnership can succeed, and their number of tricks.</returns>
        private (bool win, int tricks) Evaluate(in Deal world)
        {
            // Determine leader side in the world state
            int world_side = ((int)world.Leader) & 1;

            // Determine side of the game contract declarer
            int dec_side = ((int)this._game.Declarer) & 1;

            // Get current tricks for the leader's pair
            byte current = this._game.Tricks[this._side];

            // Set tricks to the world's leader
            int[] tricks = new int[2];
            tricks[world_side] = world.Tricks();

            // Assign remaining tricks to the opposite side
            tricks[1 - world_side] = 13 - tricks[world_side];

            // Compute tricks required to make the contract
            int required = 6 + this._game.Contract.Level;

            // Can declarer's side make the contract?
            bool can_make = tricks[dec_side] >= required;

            // Can defenders set (defeat) the game contract?
            bool can_set = tricks[1 - dec_side] < required;

            // Are we evaluating from declarer's side?
            bool is_declarer = this._side == dec_side;

            // Return whether the partnership can succeed
            bool winnable = is_declarer ? can_make : can_set;
            return (winnable, tricks[this._side] - current);
        }

        /// <summary>
        /// Runs simulations to build out the search tree in a loop until stopped.
        /// </summary>
        /// <param name="depth">Maximum depth for each search iteration.</param>
        /// <param name="token">Token to signal when to cancel the search.</param>
        private void Simulate(int depth, in CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Sample determinization for simulation
                Deal deal = this._sampler.Generate();

                // Process sample if it meets constraints
                if (!this._sampler.Filter(deal)) continue;

                // Synchronize with current game state
                this._sampler.Synchronize(ref deal);

                // Start a search simulation from the tree root
                this.Query(this._tree.Root, ref deal, depth);
            }
        }

        /// <summary>
        /// Recursively explores the game tree from the current node to a fixed depth.
        /// </summary>
        /// <param name="node">Current info-state node for the search.</param>
        /// <param name="world">Current deal (world state) to evaluate.</param>
        /// <param name="depth">Search depth still remaining.</param>
        private void Query(Node node, ref Deal world, int depth)
        {
            // Increment visits
            node.AddVisit();

            // Maximum depth has been reached
            if (depth == 0 || world.IsOver())
            {
                // Calculate results using leaf evaluator
                var (win, tricks) = this.Evaluate(world);
                node.Insert(win, (byte)tricks); return;
            }

            // Get all legal moves from this state
            List<Card> moves = world.GetMoves();

            // Pick a random move from possible plays
            Card card = moves[Random.Next(moves.Count)];

            // Play the card and advance
            uint key = world.Play(card);

            // Record edge usage
            node.Record(card);

            // Look up a child node for this move
            if (!node.TryGet(card, out Node child))
            {
                // Determine side of the leading player
                int world_side = ((int)world.Leader) & 1;

                // Decide if this node should be a maximizer
                bool maximize = world_side == this._side;

                // Create a node in the tree for this state
                child = this._tree.GetOrCreate(key, maximize);
                node.AddChild(card, child);
            }

            // Continue the search with reduced depth
            this.Query(child, ref world, depth - 1);
        }
    }

    public sealed partial class Engine
    {
        /// <summary>
        /// Configures the engine with a new game to be evaluated.
        /// </summary>
        /// <param name="game">Game instance for analysis.</param>
        public void SetGame(in Game game)
        {
            this._game = game;
            this._sampler = game.Sampling();
            this._side = ((int)game.Leader) & 1;
        }

        /// <summary>
        /// Executes the main search process with the specified options.
        /// </summary>
        /// <param name="time">Total search duration, in milliseconds.</param>
        /// <param name="interval">Interval for periodic progress update.</param>
        /// <param name="depth">Maximum search depth per simulation.</param>
        public async Task Search(uint time, uint interval, uint depth = 3)
        {
            this._tree = new Tree();
            this._elapsed = TimeSpan.Zero;
            Interlocked.Exchange(ref this._iterations, 0);
            await this.Execute(time, interval, depth).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns the evaluation results for each move available from player.
        /// </summary>
        /// <param name="model">Opponent model to use when evaluating moves.</param>
        /// <returns>A mapping from each card to its evaluation result.</returns>
        public Dictionary<Card, double> Evaluate(in Model model)
        {
            lock (this._lock)
            {
                return new ISS(this._tree, model).Solve();
            }
        }
    }
}
