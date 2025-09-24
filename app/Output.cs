namespace Ace.App
{
    /// <summary>
    /// Provides logging methods for the console app.
    /// </summary>
    internal static class Output
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

            // Restore the console color for further output
            Console.ForegroundColor = ConsoleColor.White;
            return true;
        }
    }
}
