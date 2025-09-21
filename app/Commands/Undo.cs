namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for reverting the last move in the current game.
    /// </summary>
    internal sealed class Undo : Command
    {
        internal Undo() : base
        (
            "undo", ["prev"],

           @"Usage: undo|prev

             Reverts the last play in the current game.

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

            // Check if game is defined
            if (session.Game == null)
            {
                // Must inform the user to start a game first
                return Output.Error("No game in progress! " +
                    "Start a new game with 'newgame' command.\n");
            }

            // Try to undo last move
            if (!session.Game.Undo())
            {
                return Output.Error("Nothing to undo.\n");
            }

            // Inform user of the successful undo
            Output.Success("Last move undone.\n");

            // Display the bridge game board
            session.ViewBoard(); return true;
        }
    }
}
