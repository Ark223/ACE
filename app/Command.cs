namespace Ace.App
{
    /// <summary>
    /// Represents a command that can be executed in the console.
    /// </summary>
    /// <param name="name">Main name of the command.</param>
    /// <param name="aliases">Alternative names for the command.</param>
    /// <param name="summary">Brief command description (optional).</param>
    internal abstract class Command(string name, string[] aliases, string summary)
    {
        private readonly string _name = name;
        private readonly string _summary = summary;
        private readonly string[] _aliases = aliases;

        /// <summary>
        /// Checks if the input line refers to this command.
        /// </summary>
        /// <param name="input">Raw console line input.</param>
        /// <returns>True if the verb matches name.</returns>
        internal virtual bool CanHandle(string input)
        {
            // Skip blank commands - this should not happen!
            if (string.IsNullOrWhiteSpace(input)) return false;

            // Strip leading whitespace for parsing
            string command = input.TrimStart();

            // Find separator index (space, tab, or colon)
            int sep = command.IndexOfAny([' ', '\t', ':']);

            // Take everything up to separator as the verb
            string verb = sep < 0 ? command : command[..sep];

            // Return true if main name matches
            if (verb == this._name) return true;

            // Check if verb matches any known command alias
            return Array.IndexOf(this._aliases, verb) >= 0;
        }

        /// <summary>
        /// Looks up a list of possible flag keys and returns the first non-empty value found.
        /// </summary>
        /// <param name="flags">Dictionary containing flag-value pairs parsed from input.</param>
        /// <param name="keys">List of possible keys to search for in the flag dictionary.</param>
        /// <returns>Flag value that is present and non-empty, or null if none found.</returns>
        protected static string? Extract(IReadOnlyDictionary<string, string> flags, string[] keys)
        {
            // Check each key and try to fetch its value from flags
            var values = keys.Select(k => flags.GetValueOrDefault(k));

            // From all matching values, pick the first one that is valid
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        }

        /// <summary>
        /// Extracts flag-value pairs from an input line (e.g. "-d N -c 3NT").
        /// </summary>
        /// <param name="tokens">Input split into tokens (including arguments).</param>
        /// <returns>Dictionary mapping flag names to their associated values.</returns>
        protected static IReadOnlyDictionary<string, string> GetFlags(string[] tokens)
        {
            // Stores the results as pairs: flag -> value
            var dictionary = new Dictionary<string, string>();

            // Utility to check if the token is a flag (starts with '-')
            static bool IsFlag(string s) => s.Length > 0 && s[0] == '-';

            // Move to the flag and process remaining tokens in pairs
            var chunks = tokens.SkipWhile(t => !IsFlag(t)).Chunk(2);

            // Filter out incomplete pairs or cases where the value is flag
            var pairs = chunks.Where(c => c.Length == 2 && !IsFlag(c[1]));

            // Build and return the final flag-value dictionary
            return pairs.ToDictionary(c => c[0], c => c[1]);
        }

        /// <summary>
        /// Displays this command's help text or summary.
        /// </summary>
        internal bool PrintHelp()
        {
            // Break the summary into lines for further processing
            var lines = this._summary.Replace("\r", "").Split('\n');

            // Use the third line as reference for how much to unindent
            int indent = lines[2].TakeWhile(char.IsWhiteSpace).Count();

            // Trim off the shared indent for a cleaner look in the console
            var text = lines.Select(t => t.Length > indent ? t[indent..] : t);

            // Keep the first line as-is and process the rest
            text = new[] { lines[0] }.Concat(text.Skip(1));

            // Output each unindented line to the console
            foreach (var line in text) Output.Info(line);

            // Enter next line and return success
            Console.WriteLine(); return true;
        }

        /// <summary>
        /// Runs the command with given input and session context.
        /// </summary>
        /// <param name="session">Current session state.</param>
        /// <param name="input">Input string after the prompt.</param>
        internal abstract bool Execute(Session session, string input);
    }
}
