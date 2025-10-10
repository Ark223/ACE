namespace Ace.App.Commands
{
    /// <summary>
    /// Responds for evaluating all available moves using the search tree.
    /// </summary>
    internal sealed class Evaluate : Command
    {
        internal Evaluate() : base
        (
            "evaluate", ["eval", "solve"],

           @"Usage: evaluate|eval|solve --opponent <model> --partner <model>

             Evaluates each available move from the search tree and displays their scores.

             Options:
                 -o, --opponent <model>   Sets opponent’s evaluation model
                 -p, --partner <model>    Sets partner’s evaluation model
                 -h, --help               Show this message and exit

             Models:
                 opt()               optimistic    – Best-case scenario (default for our dummy)
                 adv()               adversarial   – Worst-case scenario (opponent optimal defense)
                 exp([prior])        expectation   – Averages possible outcomes (expected value)
                 lb(lambda[,prior])  linearblend   – Weighted blend of best-case and expectation
                 smax(tau[,prior])   softmax       – Probability-weighted blend (for our partner)
                 smin(tau[,prior])   softmin       – Probability-weighted blend (for opponents)

             Example:
                 evaluate --opponent softmin(0.3) --partner optimistic"
        ){ }

        /// <summary>
        /// Creates a <see cref="Model"/> instance from the specified parameters.
        /// </summary>
        /// <param name="name">Model name (e.g. "opt", "lb", "exp").</param>
        /// <param name="values">Optional numeric parameters for the model.</param>
        /// <returns>Corresponding model object for the game evaluation.</returns>
        private static Model Parse(string name, params double[] values)
        {
            // Get model parameters, defaulting to 1 if missing
            double first = values.Length > 0 ? values[0] : 0d;
            double second = values.Length > 1 ? values[1] : 0d;

            // Adversarial; no parameters (always worst-case)
            if (name.Contains("adv")) return Model.Adversarial();

            // Expectation: prior smooths probability for low visits
            if (name.Contains("exp")) return Model.Expectation(first);

            // Linear blend: lambda linearly mixes both models
            if (name.Equals("lb") || name.Equals("linearblend"))
            {
                return Model.LinearBlend(first, second);
            }

            // Softmax: tau controls softness of the maximum
            if (name.Equals("smax") || name.Equals("softmax"))
            {
                return Model.SoftMax(first, second);
            }

            // Softmin: tau controls softness of the minimum
            if (name.Equals("smin") || name.Equals("softmin"))
            {
                return Model.SoftMin(first, second);
            }

            // Optimistic: default model
            return Model.Optimistic();
        }

        /// <summary>
        /// Parses a model input string into its name and parameter array.
        /// </summary>
        /// <param name="input">Model string (e.g. "smax(0.2, 0.8)").</param>
        /// <returns>Tuple of model name and parsed double parameters.</returns>
        private static (string, double[]) Split(string input)
        {
            try
            {
                // Find '(' to detect arguments
                int lpar = input.IndexOf('(');

                // Extract the model name before '('
                string name = input[..lpar].Trim();

                // Extract the separated arguments by removing both '(' and ')'
                string args = input.Substring(lpar + 1, input.Length - lpar - 2);

                // Split arguments by commas and convert each one to a double
                var values = args.Split(',').Select(s => double.Parse(s.Trim()));

                // Return name and numeric arguments
                return (name, values.ToArray());
            }
            catch { return (input, []); }
        }

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

            // Check if engine is defined
            if (session.Engine == null)
            {
                // Must inform the user to set up an engine first
                return Output.Error("Engine is not ready yet! " +
                    "Run the 'engine' command to set it up.\n");
            }

            // Break the input into tokens for easier parsing
            var tokens = input.Trim().Split([' ', '\t', ':']);

            // Check that all expected arguments are present
            if (tokens.Length < 3) return this.PrintHelp();

            // Pull out any flags from tokens
            var flags = GetFlags(tokens);

            // Retrieve evaluation models from the supported flags
            string? opponent = Extract(flags, ["-o", "--opponent"]);
            string? partner  = Extract(flags, ["-p", "--partner"]);

            // Parse the opponent and partner model parameters
            var (opp_name, opp_args) = Split(opponent ?? "exp");
            var (par_name, par_args) = Split(partner ?? "opt");

            // Create model instances using parsed output
            session.Opponent = Parse(opp_name, opp_args);
            session.Partner  = Parse(par_name, par_args);

            // Check if search has made a progress
            if (session.Engine.Iterations > 0)
            {
                // Reset flags to print results
                session.IsFinishing = false;

                // Directly invoke this event to force it
                session.OnSearchCompleted(); return true;
            }

            // Inform the user that evaluation models are ready
            Output.Success("Evaluation models have been set up.\n");
            return true;
        }
    }
}
