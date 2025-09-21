using static Ace.Extensions;

namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for showing all legal plays for the current player.
    /// </summary>
    internal sealed class Moves : Command
    {
        internal Moves() : base
        (
            "moves", ["plays"],

           @"Usage: moves|plays

             Lists all pseudo-legal cards that the current player can play.

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

            // Get the list of legal moves for the player
            List<Card> moves = session.Game.GetMoves();

            // Loop through suits in PBN order
            foreach (Suit suit in PBN.PbnOrder)
            {
                // Gather the ranks for all legal cards in this suit
                var cards = moves.Where(card => card.Suit == suit)
                    .Select(card => card.ToString()[0]).Reverse();

                // Print the suit and its available cards, e.g. "S AKT", etc.
                Output.Info($"{suit.ToString()[0]} {string.Concat(cards)}");
            }

            // Print blank line for spacing
            Output.Info(""); return true;
        }
    }
}
