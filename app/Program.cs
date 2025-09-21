using Ace.App.Commands;
using System.Diagnostics;
using Core = Ace.App.Commands.Engine;

namespace Ace.App
{
    internal class Program
    {
        static readonly Session _session = new();

        static readonly Command[] _commands =
        {
            new NewGame(),   // Start a new bridge game
            new IsLegal(),   // Check legality of a play
            new Moves(),     // List all available moves
            new Set(),       // Update hand constraints
            new Play(),      // Play a card
            new Undo(),      // Undo last move
            new Redo(),      // Redo undone move
            new View(),      // Display current state

            new Core(),      // Set up the analysis engine
            new Search(),    // Run tree search for current game
            new Cancel(),    // Cancel the running search
            new Continue(),  // Continue a paused tree search
            new Evaluate(),  // Evaluate moves and show scores

            new Help()       // Display help information
        };

        static void Main()
        {
            // Raise priority to high to improve performance
            var process = Process.GetCurrentProcess();
            process.PriorityClass = ProcessPriorityClass.High;
            
            while (true)
            {
                // Prompt only when the search is not running
                if (_session.IsFinished) Output.Prompt();

                // Read user input from the console
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;

                // Check for exit/quit commands to terminate the loop
                if (input.Equals("exit") || input.Equals("quit")) break;
                Console.WriteLine();

                // Clear the entire console screen if user requested it
                if (input.Equals("clear")) { Console.Clear(); continue; }

                // Try to match input to a known command and execute if found
                var cmd = _commands.FirstOrDefault(c => c.CanHandle(input));
                if (cmd != null) { cmd.Execute(_session, input); continue; }

                // Suggest help by listing commands if user enters an unknown command
                Output.Error("Unknown command. Type 'help' for a list of commands.\n");
            }
        }
    }
}
