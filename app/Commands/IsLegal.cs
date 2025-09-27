namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for checking if a specific card is a legal play for the current player.
    /// </summary>
    internal sealed class IsLegal : Command
    {
        internal IsLegal() : base
        (
            "islegal", ["legal", "check"],

           @"Usage: islegal|legal|check <card>

             Checks if the specified card is a legal play for the current player.

             Options:
                 -h, --help   Show this message and exit

             Example:
                 islegal 7S"
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

            // Normalize card input to uppercase
            string card = tokens[1].ToUpper();

            // Check if the given card is legal
            if (!session.Game.IsLegal(card))
            {
                // Notify the user that this card is not a legal move
                return Output.Warning($"{card} is not a legal play.\n");
            }

            // Confirm to user that the card is a legal play
            Output.Success($"{card} is a legal play.\n");
            return true;
        }
    }
}
