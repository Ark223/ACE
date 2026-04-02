namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for setting and revealing dummy's hand in the current game.
    /// </summary>
    internal sealed class Dummy : Command
    {
        internal Dummy() : base
        (
            "dummy", ["setdummy", "reveal"],

           @"Usage: dummy|setdummy|reveal <hand>

             Sets and reveals dummy's hand using PBN format.

             Options:
                 -h, --help   Show this message and exit

             Example:
                 dummy 3.AQT52.K963.K98"
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

            // Must inform the user to start a game first
            if (session.Game == null)
            {
                return Output.Error("No game in progress! " +
                    "Start a new game with 'newgame' command.\n");
            }

            // Break the input into tokens for easier parsing
            var tokens = input.Trim().Split([' ', '\t', ':']);
            if (tokens.Length != 2) return this.PrintHelp();

            // Apply dummy hand to the current game
            if (!session.Game.SetDummy(tokens[1]))
            {
                return Output.Error("Failed to set dummy! " +
                    "Invalid input or lead not played yet.\n");
            }

            // Display the updated bridge game board
            Output.Success("Dummy hand updated.\n");
            session.ViewBoard(); return true;
        }
    }
}
