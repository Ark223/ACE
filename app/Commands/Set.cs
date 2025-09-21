using static Ace.Extensions;

namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for setting hand constraints for a selected player.
    /// </summary>
    internal sealed class Set : Command
    {
        internal Set() : base
        (
            "set", ["constraints", "const"],

           @"Usage: set|constraints|const <player> --<constraint> <min>,<max> ...

             Sets a minimum and maximum value for each specified hand constraint.

             Options:
                 -c, --clubs      Sets constraint for the number of clubs
                 -d, --diamonds   Sets constraint for the number of diamonds
                 -h, --hearts     Sets constraint for the number of hearts
                 -s, --spades     Sets constraint for the number of spades
                 -p, --hcp        Sets constraint for high card points (HCP)
                 --help           Show this message and exit

             Example:
                 set S --spades 5,13 --hcp 8,37"
        ){ }

        /// <summary>
        /// Tries to parse a range string ("min,max") into a <see cref="Range"/> object.
        /// </summary>
        /// <param name="range">Input string in the form "min,max".</param>
        /// <param name="result">Parsed <see cref="Range"/> object if successful.</param>
        /// <param name="points">True if parsing HCP; false for any suit length.</param>
        /// <returns>True if parsing was fully successful; otherwise, false.</returns>
        private static bool Parse(string? range, out Range result, bool points = false)
        {
            // Set the default range based on the constraint type
            result = points ? new Range(0, 38) : new Range(0, 13);

            // Return false if input is blank or whitespace
            if (string.IsNullOrWhiteSpace(range)) return false;

            // Split input string by comma
            var parts = range.Trim().Split(',');

            // Make sure two values are provided
            if (parts.Length != 2) return false;

            // Try to parse the minimum value from the first part
            if (!int.TryParse(parts[0], out int min)) return false;

            // Try to parse the maximum value from the second part
            if (!int.TryParse(parts[1], out int max)) return false;

            // Assign parsed values to the range object
            result = new Range(min, max); return true;
        }

        /// <summary>
        /// Runs the command with given input and session context.
        /// </summary>
        /// <param name="session">Current session state.</param>
        /// <param name="input">Input string after the prompt.</param>
        internal override bool Execute(Session session, string input)
        {
            int side; // Temporary variable

            // Ignore if a search is in progress
            if (!session.IsFinished) return true;

            // Display help section if user has requested it
            if (input.Contains("-help")) return this.PrintHelp();

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
            if (tokens.Length < 4) return this.PrintHelp();

            // Accept only N, E, S, or W as the valid player value
            if ((side = "NESW".IndexOf(tokens[1].Trim())) == -1)
            {
                return Output.Error("Player must be one of: N/E/S/W.\n");
            }

            // Get the player value for the selected side
            Player player = Enum.GetValues<Player>()[side];

            // Access the constraint for the selected player
            var consts = session.Game.Constraints[player];

            // Pull out any flags from tokens
            var flags = GetFlags(tokens);

            // Parse and apply clubs constraint if provided
            string? clubs = Extract(flags, ["-c", "--clubs"]);
            if (Parse(clubs, out var c)) consts.Clubs = c;

            // Parse and apply diamonds constraint if provided
            string? diamonds = Extract(flags, ["-d", "--diamonds"]);
            if (Parse(diamonds, out var d)) consts.Diamonds = d;

            // Parse and apply hearts constraint if provided
            string? hearts = Extract(flags, ["-h", "--hearts"]);
            if (Parse(hearts, out var h)) consts.Hearts = h;

            // Parse and apply spades constraint if provided
            string? spades = Extract(flags, ["-s", "--spades"]);
            if (Parse(spades, out var s)) consts.Spades = s;

            // Parse and apply HCP constraint if provided
            string? hcp = Extract(flags, ["-p", "--hcp"]);
            if (Parse(hcp, out var p, true)) consts.Hcp = p;

            // Inform user that constraints were set successfully
            Output.Success("All player constraints updated.\n");

            // Loop through players and display each constraint
            foreach (Player play in Enum.GetValues<Player>())
            {
                var co = session.Game.Constraints[play];
                Output.Info($"{play,-5}: S {co.Spades} H {co.Hearts}"
                    + $" D {co.Diamonds} C {co.Clubs} HCP {co.Hcp}");
            }

            // Print extra line for clarity
            Output.Info(""); return true;
        }
    }
}
