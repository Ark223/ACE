namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for starting a new tree search for analysis.
    /// </summary>
    internal sealed class Search : Command
    {
        internal Search() : base
        (
            "search", ["sim"],

           @"Usage: search|sim --depth <depth> --duration <ms> --interval <ms>

             Starts a new tree search for analysis with the specified options.

             Options:
                 -p, --depth <depth>   Maximum search depth per simulation (default: 1)
                 -d, --duration <ms>   Duration of search in milliseconds (default: 1000)
                 -i, --interval <ms>   Progress interval in milliseconds (default: 500)
                 -h, --help            Show this message and exit

             Example:
                 search -p 2 -d 5000 -i 1000"
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

            // Check if game is defined
            if (session.Game == null)
            {
                // Must inform the user to start a game first
                return Output.Error("No game in progress! " +
                    "Start a new game with 'newgame' command.\n");
            }

            // Check if engine is defined
            if (session.Engine == null)
            {
                // Must inform the user to set up an engine first
                return Output.Error("Engine not initialized! " +
                    "Run the 'engine' command to set it up.\n");
            }

            // Break the input into tokens for easier parsing
            var tokens = input.Trim().Split([' ', '\t', ':']);

            // Check that all expected arguments are present
            if (tokens.Length != 7) return this.PrintHelp();

            // Reset flags for a new search
            session.IsFinishing = false;

            // Pull out any flags from tokens
            var flags = GetFlags(tokens);

            // Retrieve the depth value from the supported flags
            string? depth = Extract(flags, ["-p", "--depth"]);

            // Retrieve the duration value from the supported flags
            string? duration = Extract(flags, ["-d", "--duration"]);

            // Retrieve the interval value from the supported flags
            string? interval = Extract(flags, ["-i", "--interval"]);

            // Parse the duration from flags, defaulting to 1000 ms
            int dur_ms = int.TryParse(duration, out var d) ? d : 1000;

            // Parse the interval from flags, defaulting to 500 ms
            int int_ms = int.TryParse(interval, out var i) ? i : 500;

            // Parse the search depth from flags, defaulting to 1
            int plies = int.TryParse(depth, out var p) ? p : 1;

            // Start the new tree search with given options
            session.Engine.Search(dur_ms, int_ms, plies);

            // Notify the user that the tree search has started
            Output.Success($"Search started (depth {plies})..\n");
            return true;
        }
    }
}
