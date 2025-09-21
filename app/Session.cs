using static Ace.Extensions;

namespace Ace.App
{
    /// <summary>
    /// Represents the shared session state holding game options.
    /// </summary>
    internal sealed partial class Session
    {
        /// <summary>
        /// The currently loaded game being analyzed or played.
        /// </summary>
        internal Game? Game { get; private set; }

        /// <summary>
        /// The engine instance used for analysis and search.
        /// </summary>
        internal Engine? Engine { get; private set; }

        /// <summary>
        /// The evaluation model to use for the opponent side.
        /// </summary>
        internal Model? Opponent { get; set; }

        /// <summary>
        /// The evaluation model to use for our partner.
        /// </summary>
        internal Model? Partner { get; set; }

        /// <summary>
        /// Indicates the search is in its final progress phase.
        /// </summary>
        internal bool IsFinishing { get; set; } = true;

        /// <summary>
        /// Indicates the engine has just completed the search.
        /// </summary>
        internal bool IsFinished { get; set; } = true;
    }

    internal sealed partial class Session
    {
        /// <summary>
        /// Invoked when the engine reports search progress.
        /// </summary>
        internal Action OnProgressChanged => () =>
        {
            // Skip if search is completed
            if (this.IsFinished) return;

            // Holds evaluation results for moves
            Dictionary<Card, double>? eval = null;

            // Make sure both models are set before evaluation
            if (this.Opponent != null && this.Partner != null)
            {
                // Evaluate legal moves using the currently set up models
                eval = this.Engine?.Evaluate(this.Opponent, this.Partner);
            }

            // Clear all previous results
            Output.Clear(); Output.Track();

            // Print results if table is filled
            if (eval != null && eval.Count > 0)
            {
                // Display number of search iterations performed so far
                Output.Info($"Iterations\n{this.Engine?.Iterations}");

                // Display the column headers for evaluation results
                Output.Info("\nMove" + new string(' ', 3) + "Score");

                // Show each move and its score, sorted from best to worst
                foreach (var (move, score) in eval.OrderBy(kv => -kv.Value))
                {
                    Output.Info($"{move, -6} {score, 8:F6}");
                }
            }

            // Extra line for clarity
            Output.Info("");

            // Print completion message if we are in the final phase
            if (this.IsFinishing) Output.Success("Task completed.\n");

            // Mark search as finished on the last update
            if (this.IsFinishing) this.IsFinished = true;

            // Reset tracker if search is finished
            if (this.IsFinished) Output.Reset();

            // Print next prompt
            Output.Prompt();
        };

        /// <summary>
        /// Invoked when the engine signals search completion.
        /// </summary>
        internal Action OnSearchCompleted => () =>
        {
            this.IsFinishing = true;
            this.OnProgressChanged();
        };

        /// <summary>
        /// Initializes the engine with the specified number of threads.
        /// </summary>
        /// <param name="threads">Number of threads to use.</param>
        internal void InitEngine(int threads)
        {
            this.Engine = new Engine(threads);
            this.Engine.ProgressChanged += this.OnProgressChanged;
            this.Engine.SearchCompleted += this.OnSearchCompleted;
        }

        /// <summary>
        /// Creates a new game with the given deal, declarer, and contract.
        /// </summary>
        /// <param name="deal">Input deal string in PBN format.</param>
        /// <param name="declarer">Player who declares the contract.</param>
        /// <param name="contract">Contract to be played (e.g., 3NT).</param>
        internal void NewGame(string deal, Player declarer, Contract contract)
        {
            this.Game = new Game(new GameOptions
            {
                Deal = deal,
                Declarer = declarer,
                Contract = contract,
                Constraints = ConstraintSet.Empty
            });
        }

        /// <summary>
        /// Displays the current game board state in the console.
        /// </summary>
        internal void ViewBoard() => Output.Info($"{this.Game}\n");
    }
}
