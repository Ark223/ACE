namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for displaying help information for the application.
    /// </summary>
    internal sealed class Help : Command
    {
        internal Help() : base
        (
            "help", ["commands", "cmds", "?"],

           @"Usage: help|commands|cmds|?

             ACE – Adaptive Cardplay Engine

             This is a console application for analyzing and simulating the cardplay phase of contract bridge.
             You can load deals, explore legal moves, play cards, and analyze outcomes using complex algorithms.

             Available commands:

                 newgame    - Start a new game with the given deal and options
                 islegal    - Check if a play is legal in the current position
                 moves      - List all legal moves for the current player
                 set        - Set hand constraints for a selected player
                 play       - Play a card for the current player
                 view       - Show the current state of the game
                 undo       - Undo the last move
                 redo       - Redo an undone move

                 engine     - Set up an engine with thread count
                 search     - Run tree search for the current game
                 cancel     - Stop the ongoing search
                 continue   - Resume tree search from the previous state
                 evaluate   - Evaluate each move and display its score

             Type ""<command> --help"" for details on a specific command.
             Type ""exit"" or ""quit"" to close the application."
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

            // Just print help and return
            this.PrintHelp(); return true;
        }
    }
}
