namespace Ace.App
{
    /// <summary>
    /// Provides logging methods for the console app.
    /// </summary>
    internal static partial class Output
    {
        private static int _starter, _tracker;
        private static bool _tracking = false;

        /// <summary>
        /// Clears the last tracked lines from the console.
        /// </summary>
        internal static void Clear()
        {
            // Do nothing if not actively tracking lines
            if (!_tracking || _tracker == 0) return;

            // Move up to where we started tracking
            Console.SetCursorPosition(0, _starter);

            // Execute for each line in the tracked block
            for (int line = 0; line < _tracker; ++line)
            {
                // Overwrite line with spaces to erase previous output
                Console.Write(new string(' ', Console.WindowWidth));
            }

            // Return cursor to the start of block
            Console.SetCursorPosition(0, _starter);
        }

        /// <summary>
        /// Prints the command prompt to the console.
        /// </summary>
        internal static void Prompt()
        {
            // Get the current cursor position
            int current = Console.CursorTop;
            Console.SetCursorPosition(0, current);

            // Overwrite line with spaces to erase previous output
            Console.Write(new string(' ', Console.WindowWidth));

            // Move the cursor back to the start
            Console.SetCursorPosition(0, current);

            // Print the prompt message in red color
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("ACE > ");
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Resets tracking of the previous output region.
        /// </summary>
        internal static void Reset()
        {
            _tracker = 0;
            _tracking = false;
        }

        /// <summary>
        /// Starts tracking lines from the cursor position.
        /// </summary>
        internal static void Track()
        {
            _tracker = 0;
            _starter = Console.CursorTop;
            _tracking = true;
        }
    }

    internal static partial class Output
    {
        /// <summary>
        /// Prints a standard message and optionally tracks lines.
        /// </summary>
        internal static bool Info(string message)
        {
            return Write("", ConsoleColor.Gray, message);
        }

        /// <summary>
        /// Prints a success message with a green prefix.
        /// </summary>
        internal static bool Success(string message)
        {
            return Write("Success. ", ConsoleColor.Green, message);
        }

        /// <summary>
        /// Prints an error message with dark red prefix.
        /// </summary>
        internal static bool Error(string message)
        {
            return Write("Error! ", ConsoleColor.DarkRed, message);
        }

        /// <summary>
        /// Prints a warning message with a dark yellow prefix.
        /// </summary>
        internal static bool Warning(string message)
        {
            return Write("Warning! ", ConsoleColor.DarkYellow, message);
        }

        /// <summary>
        /// Writes a message to the console with a colored prefix and natural message body.
        /// </summary>
        private static bool Write(string prefix, ConsoleColor color, string message)
        {
            // Write the type prefix in color
            Console.ForegroundColor = color;
            Console.Write(prefix);

            // Write the standard message after prefix
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);

            // Tally how many lines this message will take
            int extra = message.Count(c => c == '\n') + 1;
            if (_tracking) _tracker += extra;

            // Restore the console color for further output
            Console.ForegroundColor = ConsoleColor.White;
            return true;
        }
    }
}
