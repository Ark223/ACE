namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for evaluating all available moves using the search tree.
    /// </summary>
    internal sealed class Evaluate : Command
    {
        internal Evaluate() : base
        (
            "evaluate", ["eval", "solve"],

           @"Usage: evaluate|eval|solve

             Evaluates each available move from the search tree.

             Options:
                 -h, --help   Show this message and exit"
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

            // Check if engine is defined
            if (session.Engine == null)
            {
                // Must inform the user to set up an engine first
                return Output.Error("Engine is not ready yet! " +
                    "Run the 'engine' command to set it up.\n");
            }

            // Check if search has made a progress
            if (session.Engine.Iterations > 0)
            {
                // Reset flags to print results
                session.IsFinishing = false;

                // Directly invoke this event to force it
                session.OnSearchCompleted(); return true;
            }

            // Engine exists but no search has been initiated yet
            return Output.Error("There is nothing to evaluate! " +
                "Run 'search' command to build the game tree.\n");
        }
    }
}
