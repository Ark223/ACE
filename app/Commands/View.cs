namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for displaying the current game board to the user.
    /// </summary>
    internal sealed class View : Command
    {
        internal View() : base
        (
            "view", ["board"],

           @"Usage: view|board

             Displays board of the current bridge game."
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

            // Display the bridge game board
            session.ViewBoard(); return true;
        }
    }
}
