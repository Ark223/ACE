using static Ace.Extensions;

namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for starting a new bridge game with a given options.
    /// </summary>
    internal sealed class NewGame : Command
    {
        internal NewGame() : base
        (
            "newgame", ["new", "game"],

           @"Usage: newgame|new|game <deal> --declarer <declarer> --contract <contract>

             Starts a new game using the specified PBN deal, declarer, and contract.

             Options:
                 -d, --declarer <player>     Sets the declarer (N / E / S / W)
                 -c, --contract <contract>   Sets the contract (e.g., 3NT, 4H)
                 -h, --help                  Show this message and exit

             Example:
                 newgame AJ82.6.A74.QJ643 ... 3.AQT52.K963.K98 ... -d N -c 3NT"
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

            // Break the input into tokens for easier parsing
            var tokens = input.Trim().Split([' ', '\t', ':']);

            // Check that all expected arguments are present
            if (tokens.Length != 9) return this.PrintHelp();

            // Pull out any flags from tokens
            var flags = GetFlags(tokens);

            // Retrieve the declarer value from the supported flags
            string? declarer = Extract(flags, ["-d", "--declarer"]);

            // Accept only N, E, S, or W as valid declarer values
            if (!"NESW".Contains((declarer ?? "N").Trim()))
            {
                return Output.Error("Declarer must be one of: N/E/S/W.\n");
            }

            // Retrieve the contract value from the supported flags
            string? contract = Extract(flags, ["-c", "--contract"]);

            // Try parsing the contract string; otherwise throw error
            if (!Contract.TryParse(contract, out Contract result))
            {
                return Output.Error("Invalid contract (e.g., 3NT, 4H).\n");
            }

            // Convert the declarer letter (N/E/S/W) into a player
            Player player = (Player)"NESW".IndexOf(declarer ?? "N");

            // Reconstruct the full deal from input and start a new game
            session.NewGame(string.Join(" ", tokens[1..5]), player, result);

            // Let the user know everything's set up and ready to go
            Output.Success("New game created. Ready for play.\n");

            // Attach this new game to the engine
            session.Engine?.SetGame(session.Game);

            // Display the bridge game board
            session.ViewBoard(); return true;
        }
    }
}
