namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for continuing the ongoing engine search.
    /// </summary>
    internal sealed class Continue : Command
    {
        internal Continue() : base
        (
            "continue", ["cont", "resume"],

           @"Usage: continue|cont|resume --duration <ms> --interval <ms>

             Continues the ongoing tree search operation.

             Options:
                 -d, --duration <ms>   Duration of search in milliseconds (default: 1000)
                 -i, --interval <ms>   Progress interval in milliseconds (default: 500)
                 -h, --help            Show this message and exit

             Example:
                 continue -d 5000 -i 1000"
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
                return Output.Error("Engine is not ready yet! " +
                    "Run the 'engine' command to set it up.\n");
            }

            // Break the input into tokens for easier parsing
            var tokens = input.Trim().Split([' ', '\t', ':']);

            // Check that all expected arguments are present
            if (tokens.Length != 5) return this.PrintHelp();

            // Reset flags for a new search
            session.IsFinishing = false;

            // Pull out any flags from tokens
            var flags = GetFlags(tokens);

            // Retrieve the duration value from the supported flags
            string? duration = Extract(flags, ["-d", "--duration"]);

            // Retrieve the interval value from the supported flags
            string? interval = Extract(flags, ["-i", "--interval"]);

            // Parse the duration from flags, defaulting to 1000 ms
            int dur_ms = int.TryParse(duration, out var d) ? d : 1000;

            // Parse the interval from flags, defaulting to 500 ms
            int int_ms = int.TryParse(interval, out var i) ? i : 500;

            // Continue the search with given options
            session.Engine.Continue(dur_ms, int_ms);

            // Check if search actually started
            if (!session.Engine.IsSearching)
            {
                // Inform the user that there is no search to resume
                Output.Error("No previous search to continue.\n");

                // Mark the session as finished and exit
                session.IsFinished = true; return true;
            }

            // Notify the user that the search is continued
            Output.Success("Ongoing search continued..\n");
            return true;
        }
    }
}
