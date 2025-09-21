namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for initializing and configuring the engine.
    /// </summary>
    internal sealed class Engine : Command
    {
        internal Engine() : base
        (
            "engine", ["ace"],

           @"Usage: engine|ace --threads <threads>

             Initializes or reconfigures the analysis engine.

             Options:
                 -t, --threads   Sets the number of search threads
                 -h, --help      Show this message and exit"
        ){ }

        /// <summary>
        /// Runs the command with given input and session context.
        /// </summary>
        /// <param name="session">Current session state.</param>
        /// <param name="input">Input string after the prompt.</param>
        internal override bool Execute(Session session, string input)
        {
            // Ignore if a search is in progress
            if (!session.IsFinished) return true;

            // Display help section if user has requested it
            if (input.Contains("-h")) return this.PrintHelp();

            // Break the input into tokens for easier parsing
            var tokens = input.Trim().Split([' ', '\t', ':']);

            // Check that all expected arguments are present
            if (tokens.Length != 3) return this.PrintHelp();

            // Pull out any flags from tokens
            var flags = GetFlags(tokens);

            // Retrieve the threads value from the supported flags
            string? threads = Extract(flags, ["-t", "--threads"]);

            // Parse the thread count from flags, defaulting to 1
            int count = int.TryParse(threads, out var t) ? t : 1;

            // Initialize engine with this input
            session.InitEngine(threads: count);

            // Attach current game to the engine
            session.Engine?.SetGame(session.Game);

            // Inform user how many threads the engine will be using
            Output.Success($"Engine set up with {count} thread(s).\n");
            return true;
        }
    }
}
