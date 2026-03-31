using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static Ace.Extensions;

namespace Ace
{
    /// <summary>
    /// Provides configuration settings for the tree search algorithm.
    /// </summary>
    public sealed class Config
    {
        /// <summary>
        /// Exploration constant that balances search depth and breadth.
        /// </summary>
        /// <remarks>
        /// Higher values encourage more exploration of less-visited nodes,<br>
        /// </br>while lower values favor exploitation of the best-known nodes.
        /// </remarks>
        public double Exploration { get; set; } = 0.6061d;

        /// <summary>
        /// Controls whether the search is restricted to the current trick.
        /// </summary>
        /// <remarks>
        /// When enabled, the search tree grows within the ongoing trick,<br>
        /// </br>while when disabled it may also expand across multiple tricks.
        /// </remarks>
        public bool Limiter { get; set; } = false;
    }

    /// <summary>
    /// Represents the core game-solving engine for analyzing bridge games.
    /// </summary>
    public sealed partial class Engine
    {
        private Game _game;
        private Node _root;
        private Side _side;
        private Config _config;
        private Sampler _sampler;

        private int _depth;
        private long _max_iters;
        private long _iterations;
        private TimeSpan _elapsed;
        private int _seed = 0x5851f42d;

        private readonly int _threads;
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
        /// Gets the configuration currently used by the engine.
        /// </summary>
        public Config Config => this._config;

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
        /// Evaluates whether the investigated partnership can achieve their objective:<br></br>
        /// either making the contract (declarer's side) or setting the contract (defenders).<br></br>
        /// Also returns the number of tricks available to the leader's partnership in this state.
        /// </summary>
        /// <param name="world">Deal state to evaluate, representing a possible game scenario.</param>
        /// <returns>True if this partnership can succeed, and their number of extra tricks.</returns>
        private IReadOnlyDictionary<Card, Outcome> Evaluate(in Deal world)
        {
            // Determine leader side in world state
            int world_side = ((int)world.Leader) & 1;

            // Determine side of the game contract declarer
            int dec_side = ((int)this._game.Declarer) & 1;

            // Are we evaluating from declarer's side?
            bool declarer = (int)this._side == dec_side;

            // Store evaluation outcome for each candidate
            var evals = new Dictionary<Card, Outcome>();
            Dictionary<Card, int> dds = world.Tricks();

            // Iterate over playable cards or a terminal
            foreach (Card move in world.Moves.Count > 0 ?
                world.Moves : new List<Card> { Card.None })
            {
                // Set tricks to current leader
                int[] tricks = new int[2];
                tricks[world_side] = dds[move];

                // Assign remaining tricks to the opposite side
                tricks[1 - world_side] = 13 - tricks[world_side];

                // Assess whether the contract is made or set
                int required = 6 + this._game.Contract.Level;
                bool can_make = tricks[dec_side] >= required;
                bool can_set = tricks[dec_side] < required;

                // Evaluate if our partnership can achieve goal
                int score = tricks[(int)this._side] - required;
                evals[move] = new Outcome(declarer ? can_make :
                    can_set, Math.Min(13, score), this._side);
            }
            return evals;
        }

        /// <summary>
        /// Updates the search tree after a specific move is played.
        /// </summary>
        /// <param name="card">Card that was played in a game.</param>
        private void OnMovePlayed(Card card)
        {
            // No tree to update so ignore
            if (this._root == null) return;

            // Reset root when tree reuse is not applicable
            if (this._depth < 52 || card.Equals(Card.None))
            {
                this._root = null;
                return;
            }

            // Reuse subtree if this move was explored
            if (this._root.TryGet(card, out Node next))
            {
                next.Detach();
                this._root = next;
            }

            // Start from a fresh root
            else this._root = new Node();
        }

        /// <summary>
        /// Resets the engine state for a new or continued search.
        /// </summary>
        /// <param name="depth">Search depth per simulation.</param>
        /// <param name="config">Hyperparameters for a tree search.</param>
        /// <param name="hard_reset">Whether to clear previous data.</param>
        private bool Setup(int depth, in Config config, bool hard_reset)
        {
            // Changing depth invalidates the tree shape
            if (this._depth != depth) this._root = null;

            this._elapsed = TimeSpan.Zero;
            this._config = config;
            this._depth = depth;

            // Reset everything
            if (hard_reset)
            {
                // Can't proceed if game is undefined
                if (this._game == null) return false;

                // Create fresh root if no reusable tree exists
                if (this._root == null) this._root = new Node();

                // Reinitialize sampling and counters
                this._sampler = this._game.Sampling();
                this._iterations = 0L;
            }

            // Determine a new side from game leader
            this._side = this._game.Leader.ToSide();

            // Only return true if the setup is valid
            return hard_reset || this._root != null;
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
        /// Executes the internal search process with the specified options.
        /// </summary>
        /// <param name="duration">Total search duration, in milliseconds.</param>
        /// <param name="interval">Interval for periodic progress update.</param>
        private async Task Execute(int duration, int interval)
        {
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
                workers.Add(Task.Run(() => Run(token, thread), token));
            }

            // Start periodic progress reporting
            var progress = Task.Run(async () =>
            {
                // Keep the loop active until cancelled
                while (!token.IsCancellationRequested)
                {
                    // Wait for the next interval before triggering update
                    await Task.Delay(interval, token).ConfigureAwait(false);
                    this.Trigger(this.ProgressChanged, stopwatch.Elapsed);
                }
            }, token);

            // Combine all workers
            workers.Add(progress);

            try
            {
                // Await completion of all scheduled worker tasks
                await Task.WhenAll(workers).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {}

            // Stop everything
            stopwatch.Stop();
            this._cts.Dispose();
            this._cts = null;

            // Notify listeners that the search has been completed
            this.Trigger(this.SearchCompleted, stopwatch.Elapsed);
        }

        /// <summary>
        /// Starts a single tree search worker using its own random stream.
        /// </summary>
        /// <param name="token">Token for canceling this operation.</param>
        /// <param name="worker">Worker index used to derive a seed.</param>
        private void Run(in CancellationToken token, int worker)
        {
            // Force each worker to sample different deals deterministically
            Random.Bind(unchecked(this._seed ^ (worker + 1) * 0x165667b1));

            // Run simulations continuously until cancellation is requested
            while (!token.IsCancellationRequested) this.Simulate(this._root);
        }

        /// <summary>
        /// Runs simulations to build out the search tree until stopped.
        /// </summary>
        /// <param name="node">Node where the tree search begins.</param>
        private void Simulate(in Node node)
        {
            // Increment and fetch the counter of tested simulations
            long iters = Interlocked.Increment(ref this._iterations);

            // Cancel tasks if reached the iteration limit
            if (iters >= this._max_iters) this.Cancel();

            // Generate new determinization sample
            Deal deal = this._sampler.Generate();

            // Process sample if it meets constraints
            if (!this._sampler.Filter(deal)) return;

            // Synchronize with current game state
            this._sampler.Synchronize(ref deal);

            // Start a new tree search iteration from node
            this.Query(new MCTS(node, deal, this._depth));
        }

        /// <summary>
        /// Performs a single MCTS iteration on the given sampled world state.
        /// </summary>
        /// <param name="search">Instance used for this iteration.</param>
        private void Query(in MCTS search)
        {
            // Select most promising node
            search.Select(this._config);

            // Expand tree with legal actions
            Deal deal = search.Expand();

            // Evaluate the current world state
            var results = this.Evaluate(deal);

            // Update action nodes with DDS results
            var outcome = search.Simulate(results);

            // Back up simulation outcome
            search.Backpropagate(outcome);
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
            if (this._game != null)
            {
                // Remove move tracker from previous game
                this._game.MovePlayed -= this.OnMovePlayed;
            }
            this._game = game;
            if (this._game != null)
            {
                // Track moves at this game for tree reuse
                this._game.MovePlayed += this.OnMovePlayed;
            }
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
        /// Sets the seed for the engine's random number generator.
        /// </summary>
        /// <param name="seed">Seed value for randomization.</param>
        public void SetSeed(int seed)
        {
            this._seed = seed;
        }

        /// <summary>
        /// Requests cancellation of any active search process.
        /// </summary>
        public void Cancel()
        {
            this._cts?.Cancel();
        }

        /// <summary>
        /// Returns the evaluation results for each move available from player.
        /// </summary>
        /// <returns>A mapping from each card to its evaluation result.</returns>
        public List<Evaluation> Evaluate()
        {
            var results = new List<Evaluation>(13);
            foreach (var entry in this._root.Children)
            {
                results.Add(new Evaluation(entry.Value));
            }
            results.Sort();
            return results;
        }

        /// <summary>
        /// Continues the main search process using the default configuration settings.
        /// </summary>
        /// <param name="duration">Total allowed search duration in milliseconds.</param>
        /// <param name="interval">Interval for progress reporting in milliseconds.</param>
        public Task Continue(int duration, int interval)
        {
            return this.Continue(duration, interval, new Config());
        }

        /// <summary>
        /// Continues the main search process using the specified configuration settings.
        /// </summary>
        /// <param name="duration">Total allowed search duration in milliseconds.</param>
        /// <param name="interval">Interval for progress reporting in milliseconds.</param>
        /// <param name="config">Tree search configuration. Default set if omitted.</param>
        public async Task Continue(int duration, int interval, Config config)
        {
            if (!this.Setup(this._depth, config, false)) return;
            await this.Execute(duration, interval).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the main search process using the default configuration settings.
        /// </summary>
        /// <param name="duration">Total allowed search duration in milliseconds.</param>
        /// <param name="interval">Interval for progress reporting in milliseconds.</param>
        /// <param name="depth">Maximum depth to explore a game tree per simulation.</param>
        public Task Search(int duration, int interval, int depth)
        {
            return this.Search(duration, interval, depth, new Config());
        }

        /// <summary>
        /// Executes the main search process using the specified configuration settings.
        /// </summary>
        /// <param name="duration">Total allowed search duration in milliseconds.</param>
        /// <param name="interval">Interval for progress reporting in milliseconds.</param>
        /// <param name="depth">Maximum depth to explore a game tree per simulation.</param>
        /// <param name="config">Tree search configuration. Default set if omitted.</param>
        public async Task Search(int duration, int interval, int depth, Config config)
        {
            if (!this.Setup(Math.Max(1, depth), config, true)) return;
            await this.Execute(duration, interval).ConfigureAwait(false);
        }
    }
}
