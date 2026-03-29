namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for starting a new tree search for analysis.
    /// </summary>
    internal sealed class Search : Command
    {
        internal Search() : base
        (
            "search", ["sim", "go"],

           @"Usage: search|sim|go --depth <depth> --duration <ms> --interval <ms> --limit <iters>

             Starts a new tree search for game analysis with the specified options.

             Options:
                 -p, --depth <depth>    Maximum search depth per iteration (default: 2)
                 -d, --duration <ms>    Search duration in milliseconds (default: 1000)
                 -i, --interval <ms>    Progress interval in milliseconds (default: 500)
                 -l, --limit <iters>    Total number of iterations allowed (default: inf)
                 -h, --help             Show this message and exit

             Example:
                 search -p 52 -d 5000 -i 1000"
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

            if (session.Game == null)
            {
                // Must inform the user to start a game first
                return Output.Error("No game in progress! " +
                    "Start a new game with 'newgame' command.\n");
            }
            else if (session.Engine == null)
            {
                // Must inform the user to set up an engine first
                return Output.Error("Engine not initialized! " +
                    "Run the 'engine' command to set it up.\n");
            }

            // Break the input into tokens for easier parsing
            var tokens = input.Trim().Split([' ', '\t', ':']);
            if (tokens.Length < 3) return this.PrintHelp();

            // Reset flags for a new search
            session.IsFinishing = false;

            // Pull out any flags from tokens
            var flags = GetFlags(tokens);

            // Extract optional flags for search configuration
            string? depth    = Extract(flags, ["-p", "--depth"]);
            string? duration = Extract(flags, ["-d", "--duration"]);
            string? interval = Extract(flags, ["-i", "--interval"]);
            string? limit    = Extract(flags, ["-l", "--limit"]);

            // Parse these values into numbers, fall back to defaults
            int dur_ms = int.TryParse(duration, out var d) ? d : 1000;
            int int_ms = int.TryParse(interval, out var i) ? i : 500;
            int iters  = int.TryParse(limit,    out var l) ? l : 0;
            int plies  = int.TryParse(depth,    out var p) ? p : 52;

            // Limit total search iterations when provided
            if (iters > 0) session.Engine.SetIterations(iters);

            // Start new tree search with given options
            session.Engine.Search(dur_ms, int_ms, plies);
            Output.Success($"Search started (depth {plies})..\n");
            return true;
        }
    }
}
