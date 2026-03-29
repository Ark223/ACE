namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for cancelling the ongoing engine search.
    /// </summary>
    internal sealed class Cancel : Command
    {
        internal Cancel() : base
        (
            "cancel", ["stop"],

           @"Usage: cancel|stop

             Cancels the ongoing tree search operation.

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
            // Display help section if user has requested it
            if (input.Contains("-h")) return this.PrintHelp();

            if (session.Engine == null || !session.Engine.IsSearching)
            {
                // Must inform the user to run a tree search process first
                return Output.Error("No search is currently running.\n");
            }

            session.Cancel();
            return Output.Success("Ongoing search cancelled.\n");
        }
    }
}
