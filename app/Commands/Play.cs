namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for playing a specified card in the current game.
    /// </summary>
    internal sealed class Play : Command
    {
        internal Play() : base
        (
            "play", ["move", "card"],

           @"Usage: play|move|card <card>

             Plays the specified card in the current game.

             Options:
                 -h, --help   Show this message and exit

             Example:
                 play 7S"
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

            // Break the input into tokens for easier parsing
            var tokens = input.Trim().Split([' ', '\t', ':']);

            // Check that all expected arguments are present
            if (tokens.Length != 2) return this.PrintHelp();

            // Try to play this card in the current game
            if (!session.Game.Play(tokens[1].ToUpper()))
            {
                return Output.Error("Invalid play.\n");
            }

            // Let the user know the selected card was played
            Output.Success($"Played {tokens[1].ToUpper()}.\n");

            // Display the bridge game board
            session.ViewBoard(); return true;
        }
    }
}
