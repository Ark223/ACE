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

            // Check if engine exists and the search is in progress
            if (session.Engine == null || !session.Engine.IsSearching)
            {
                return Output.Error("No search is currently running.\n");
            }

            // Cancel ongoing analysis
            session.Cancel();

            // Inform user that the search was cancelled
            Output.Success("Ongoing search cancelled.\n");
            return true;
        }
    }
}
