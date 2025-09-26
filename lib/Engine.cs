using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static Ace.Extensions;

namespace Ace
{
    /// <summary>
    /// Represents evaluation scores for all legal moves.
    /// </summary>
    using Evaluation = Dictionary<Card, double>;

    /// <summary>
    /// Represents the core game-solving engine for analyzing bridge games.
    /// </summary>
    public sealed partial class Engine
    {
        private int _side;
        private Game _game;
        private Tree _tree;
        private Sampler _sampler;

        private int _depth;
        private long _max_iters;
        private long _iterations;
        private TimeSpan _elapsed;

        private readonly int _threads;
        private readonly object _lock;
        private CancellationTokenSource _cts;
        private readonly SynchronizationContext _context;

        /// <summary>
        /// Event fired when the search progress is updated.
        /// </summary>
        public event Action ProgressChanged;

        /// <summary>
        /// Event fired when the search has finished running.
        /// </summary>
        public event Action SearchCompleted;

        /// <summary>
        /// Gets the total elapsed time of the search operation.
        /// </summary>
        public TimeSpan Elapsed => this._elapsed;

        /// <summary>
        /// Indicates whether a search operation is currently active.
        /// </summary>
        public bool IsSearching => this._cts != null;

        /// <summary>
        /// Gets the total count of search iterations performed so far.
        /// </summary>
        public long Iterations => Interlocked.Read(ref this._iterations);

        /// <summary>
        /// Initializes a new game engine with the specified thread count.
        /// </summary>
        /// <param name="threads">Number of the search threads.</param>
        public Engine(int threads)
        {
            this._lock = new object();
            this._elapsed = TimeSpan.Zero;
            this._max_iters = long.MaxValue;
            this._threads = Math.Max(1, threads);
            this._context = SynchronizationContext.Current;
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
        /// Determines the role of the current leader in this deal.
        /// </summary>
        /// <param name="world">Current deal state to evaluate.</param>
        /// <returns>A role for the currently leading player.</returns>
        private Role GetRole(in Deal world)
        {
            // Retrieve player at the root
            Player root = this._game.Leader;

            // If leader matches root, it's our move
            if (root == world.Leader) return Role.Self;

            // Determine side of the leading player
            int world_side = ((int)world.Leader) & 1;

            // Is this leader on our partnership?
            bool our_side = world_side == this._side;

            // Return role based on partnership alignment
            return our_side ? Role.Partner : Role.Opponent;
        }

        /// <summary>
        /// Resets the engine state for a new or continued search.
        /// </summary>
        /// <param name="depth">Search depth per simulation.</param>
        /// <param name="hard_reset">Whether to clear content.</param>
        private bool Setup(int depth, bool hard_reset)
        {
            this._depth = depth;
            this._elapsed = TimeSpan.Zero;

            // Reset everything
            if (hard_reset)
            {
                // Can't proceed if game is undefined
                if (this._game == null) return false;

                // Initialize the sampler for new run
                this._sampler = this._game.Sampling();

                // Clear the current tree
                this._tree = new Tree();

                // Reset iteration counter
                this._iterations = 0L;
            }

            // Determine a new side from game leader
            this._side = ((int)this._game.Leader) & 1;

            // Only return true if the setup is valid
            return hard_reset || this._tree != null;
        }

        /// <summary>
        /// Fires the given callback, using the UI context if there is one.
        /// </summary>
        /// <param name="callback">Event handler to invoke.</param>
        /// <param name="elapsed">Elapsed time to record.</param>
        private void Trigger(Action callback, TimeSpan elapsed)
        {
            this._elapsed = elapsed;
            if (callback == null) return;

            // Call directly if not running on UI
            if (this._context == null) callback();

            // Post this callback to the correct context
            else this._context.Post(_ => callback(), null);
        }
    }

    public sealed partial class Engine
    {
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

            // Set tricks to the world's leader
            int[] tricks = new int[2];
            tricks[world_side] = world.Tricks();

            // Assign remaining tricks to the opposite side
            tricks[1 - world_side] = 13 - tricks[world_side];

            // Compute tricks required to make the contract
            int required = 6 + this._game.Contract.Level;

            // Can declarer's side make the contract?
            bool can_make = tricks[dec_side] >= required;

            // Can defenders set the game contract?
            bool can_set = tricks[dec_side] < required;

            // Are we evaluating from declarer's side?
            bool declarer = this._side == dec_side;

            // Return whether our partnership can succeed
            bool winnable = declarer ? can_make : can_set;
            return (winnable, tricks[this._side]);
        }

        /// <summary>
        /// Executes the internal search process with the specified options.
        /// </summary>
        /// <param name="duration">Total search duration, in milliseconds.</param>
        /// <param name="interval">Interval for periodic progress update.</param>
        private async Task Execute(int duration, int interval)
        {
            // Ensure sensible minimum values
            interval = Math.Max(50, interval);
            duration = Math.Max(250, duration);
            if (interval > duration) interval = duration;

            // Set up a stopwatch to control the duration 
            var time = TimeSpan.FromMilliseconds(duration);
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Token triggers after the allocated time
            this._cts = new CancellationTokenSource();
            this._cts.CancelAfter(time);
            var token = this._cts.Token;

            // Start workers for running parallel simulations
            List<Task> workers = new List<Task>(this._threads);
            for (int thread = 0; thread < this._threads; thread++)
            {
                // Each worker contributes to the shared search tree
                workers.Add(Task.Run(() => Simulate(token), token));
            }

            // Start periodic progress reporting
            var progress = Task.Run(async () =>
            {
                try
                {
                    // Keep the loop active until cancelled
                    while (!token.IsCancellationRequested)
                    {
                        // Wait for the next interval before triggering update
                        await Task.Delay(interval, token).ConfigureAwait(false);

                        // Notify listeners that progress has been updated
                        this.Trigger(this.ProgressChanged, stopwatch.Elapsed);
                    }
                }
                catch (OperationCanceledException) {}
            }, token);

            try
            {
                // Include progress reporter into awaited tasks
                workers.Add(progress);

                // Wait for all currently running tasks to finish
                await Task.WhenAll(workers).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {}

            // Stop the timer
            stopwatch.Stop();

            // Clean up resources related to token
            this._cts.Dispose(); this._cts = null;

            // Notify listeners that the search has been completed
            this.Trigger(this.SearchCompleted, stopwatch.Elapsed);
        }

        /// <summary>
        /// Runs simulations to build out the search tree until stopped.
        /// </summary>
        /// <param name="token">Token for canceling the operation.</param>
        private void Simulate(in CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Increment and fetch the counter of tested simulations
                long iters = Interlocked.Increment(ref this._iterations);

                // Cancel tasks if reached the iteration limit
                if (iters >= this._max_iters) this.Cancel();

                // Generate new determinization sample
                Deal deal = this._sampler.Generate();

                // Process sample if it meets constraints
                if (!this._sampler.Filter(deal)) continue;

                // Synchronize with current game state
                this._sampler.Synchronize(ref deal);

                // Start a search simulation from the tree root
                this.Query(this._tree.Root, ref deal, this._depth);
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
                node.Insert(win, tricks); return;
            }

            // Get all legal moves from this state
            List<Card> moves = world.GetMoves();

            // Pick a random move from possible plays
            Card card = moves[Random.Next(moves.Count)];

            // Play the card and advance
            uint key = world.Play(card);

            // Look up a child node for played move
            if (!node.TryGet(card, out Node child))
            {
                // Assign player role for this node
                Role role = this.GetRole(world);

                // Create a node in the tree for this state
                child = this._tree.GetOrCreate(key, role);
                node.AddChild(card, child);
            }

            // Continue the search with reduced depth
            this.Query(child, ref world, depth - 1);
        }
    }

    public sealed partial class Engine
    {
        /// <summary>
        /// Assigns a new game for the engine to analyze.
        /// </summary>
        /// <param name="game">New game instance.</param>
        public void SetGame(in Game game)
        {
            this._game = game;
        }

        /// <summary>
        /// Sets the maximum number of search iterations allowed.
        /// </summary>
        /// <param name="iterations">Limit on iterations.</param>
        public void SetIterations(long iterations)
        {
            this._max_iters = iterations;
        }

        /// <summary>
        /// Requests cancellation of any active search process.
        /// </summary>
        public void Cancel()
        {
            this._cts?.Cancel();
        }

        /// <summary>
        /// Continues the main search process with the specified options.
        /// </summary>
        /// <param name="duration">Total search duration, in milliseconds.</param>
        /// <param name="interval">Interval for periodic progress update.</param>
        public async Task Continue(int duration, int interval)
        {
            if (!this.Setup(this._depth, false)) return;
            await this.Execute(duration, interval).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the main search process with the specified options.
        /// </summary>
        /// <param name="duration">Total search duration, in milliseconds.</param>
        /// <param name="interval">Interval for periodic progress update.</param>
        /// <param name="depth">Maximum search depth per simulation.</param>
        public async Task Search(int duration, int interval, int depth)
        {
            if (!this.Setup(Math.Max(1, Math.Min(3, depth)), true)) return;
            await this.Execute(duration, interval).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns the evaluation results for each move available from player.
        /// </summary>
        /// <param name="opponent">Evaluation model for opponent side.</param>
        /// <param name="partner">Evaluation model for partner/our side.</param>
        /// <returns>A mapping from each card to its evaluation result.</returns>
        public Evaluation Evaluate(in Model opponent, in Model partner = null)
        {
            lock (this._lock)
            {
                var model = partner ?? Model.Optimistic();
                if (this._tree.IsEmpty) return new Evaluation();
                return new ISS(this._tree, opponent, model).Solve();
            }
        }
    }
}
