namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for re-applying the previously undone move.
    /// </summary>
    internal sealed class Redo : Command
    {
        internal Redo() : base
        (
            "redo", ["next"],

           @"Usage: redo|next

             Re-applies the previously undone move.

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

            // Try to redo undone move
            if (!session.Game.Redo())
            {
                return Output.Error("Nothing to redo.\n");
            }

            // Inform user of successful redo
            Output.Success("Move redone.\n");

            // Display the bridge game board
            session.ViewBoard(); return true;
        }
    }
}
