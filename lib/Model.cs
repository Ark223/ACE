using System;
using System.Collections.Generic;

namespace Ace
{
    /// <summary>
    /// Abstract base for opponent models used in tree search algorithms.
    /// </summary>
    public abstract class Model
    {
        /// <summary>
        /// Scoring function delegate for evaluating nodes.
        /// </summary>
        /// <param name="node">Node to evaluate.</param>
        /// <returns>Computed score for this node.</returns>
        internal delegate double Score(in Node node);

        /// <summary>
        /// Aggregates child scores into a single value for this node.
        /// </summary>
        /// <param name="node">Child node to evaluate.</param>
        /// <param name="score">Function to score child.</param>
        /// <returns>Evaluated score for the child node.</returns>
        internal abstract double Backup(in Node node, in Score score);

        /// <summary>
        /// This model aims for the most favorable outcome for the player.
        /// </summary>
        /// <returns>An own-side model using the specified strategy.</returns>
        public static Model Optimistic()
        {
            return new OptimisticModel();
        }

        /// <summary>
        /// This model always assumes the worst-case outcome for the player.
        /// </summary>
        /// <returns>An opponent model using the specified strategy.</returns>
        public static Model Adversarial()
        {
            return new AdversarialModel();
        }

        /// <summary>
        /// This model uses the weighted average of all possible outcomes.
        /// </summary>
        /// <param name="prior">Smoothing for rarely visited moves.</param>
        /// <returns>An opponent model using the specified strategy.</returns>
        public static Model Expectation(double prior = 0d)
        {
            return new ExpectationModel(prior);
        }

        /// <summary>
        /// This model linearly blends the best-case and expectation models.
        /// </summary>
        /// <param name="lambda">Blend between best-case and average.</param>
        /// <param name="prior">Smoothing for rarely visited moves.</param>
        /// <returns>Evaluation model using the specified strategy.</returns>
        public static Model LinearBlend(double lambda, double prior = 0d)
        {
            return new LinearBlendModel(lambda, prior);
        }

        /// <summary>
        /// This model uses a soft maximum (risk-seeking average) over scores.
        /// </summary>
        /// <param name="tau">Adjusts how much to soften the maximum.</param>
        /// <param name="prior">Smoothing for rarely visited moves.</param>
        /// <returns>An own-side model using the specified strategy.</returns>
        public static Model SoftMax(double tau, double prior = 0d)
        {
            return new SoftMaxModel(tau, prior);
        }

        /// <summary>
        /// This model uses a soft minimum (risk-averse average) over scores.
        /// </summary>
        /// <param name="tau">Adjusts how much to soften the minimum.</param>
        /// <param name="prior">Smoothing for rarely visited moves.</param>
        /// <returns>An opponent model using the specified strategy.</returns>
        public static Model SoftMin(double tau, double prior = 0d)
        {
            return new SoftMinModel(tau, prior);
        }
    }

    /// <summary>
    /// This model aims for the most favorable outcome for the player.
    /// </summary>
    internal sealed class OptimisticModel : Model
    {
        /// <summary>
        /// Picks the maximum score among all children (best case).
        /// </summary>
        internal override double Backup(in Node node, in Score score)
        {
            double best = double.NegativeInfinity;
            foreach (Node child in node.Children.Values)
            {
                double value = score(child);
                if (value > best) best = value;
            }
            return best;
        }
    }

    /// <summary>
    /// This model always assumes the worst-case outcome for the player.
    /// </summary>
    internal sealed class AdversarialModel : Model
    {
        /// <summary>
        /// Picks the minimum score among all children (worst case).
        /// </summary>
        internal override double Backup(in Node node, in Score score)
        {
            double worst = double.PositiveInfinity;
            foreach (Node child in node.Children.Values)
            {
                double value = score(child);
                if (value < worst) worst = value;
            }
            return worst;
        }
    }

    /// <summary>
    /// This model uses the weighted average of all possible outcomes.
    /// </summary>
    internal sealed class ExpectationModel : Model
    {
        private readonly double _prior;

        /// <summary>
        /// Creates an expectation model with the given smoothing prior.
        /// </summary>
        /// <param name="prior">Smoothing for rarely visited moves.</param>
        internal ExpectationModel(double prior = 0d)
        {
            this._prior = Math.Max(0d, prior);
        }

        /// <summary>
        /// Computes a probability-weighted average over all possible moves.
        /// </summary>
        internal override double Backup(in Node node, in Score score)
        {
            double result = 0d;
            foreach (var pair in node.Policy(this._prior))
            {
                result += pair.probability * score(pair.child);
            }
            return result;
        }
    }

    /// <summary>
    /// This model linearly blends the best-case and expectation models.
    /// </summary>
    internal sealed class LinearBlendModel : Model
    {
        private readonly double _lambda, _prior;

        /// <summary>
        /// Creates a linear blend model with the given mixing parameters.
        /// </summary>
        /// <param name="lambda">Blend between best-case and average.</param>
        /// <param name="prior">Smoothing for rarely visited moves.</param>
        internal LinearBlendModel(double lambda, double prior = 0d)
        {
            this._lambda = Math.Max(0d, Math.Min(1d, lambda));
            this._prior = Math.Max(0d, prior);
        }

        /// <summary>
        /// Balances the best-case and average scores, weighted by lambda.
        /// </summary>
        internal override double Backup(in Node node, in Score score)
        {
            double result;
            if (node.Role == Role.Partner)
            {
                // Find the best outcome for our side
                double best = double.NegativeInfinity;
                foreach (Node child in node.Children.Values)
                {
                    double value = score(child);
                    if (value > best) best = value;
                }
                result = best;
            }
            else
            {
                // Find the worst outcome for our side
                double worst = double.PositiveInfinity;
                foreach (Node child in node.Children.Values)
                {
                    double value = score(child);
                    if (value < worst) worst = value;
                }
                result = worst;
            }

            // Compute expected score weighted by policy
            double lambda = this._lambda, expected = 0d;
            foreach (var pair in node.Policy(this._prior))
            {
                expected += pair.probability * score(pair.child);
            }

            // Blend both models according to the lambda factor
            return (1d - lambda) * result + lambda * expected;
        }
    }

    internal sealed class SoftMaxModel : Model
    {
        private readonly double _tau, _prior;

        /// <summary>
        /// Creates a soft maximum model with the given mixing parameters.
        /// </summary>
        /// <param name="tau">Adjusts how much to soften the maximum.</param>
        /// <param name="prior">Smoothing for rarely visited moves.</param>
        internal SoftMaxModel(double tau, double prior = 0d)
        {
            this._tau = Math.Max(1e-6d, tau);
            this._prior = Math.Max(0d, prior);
        }

        /// <summary>
        /// Computes the soft maximum (risk-seeking) value over all possible moves.
        /// </summary>
        internal override double Backup(in Node node, in Score score)
        {
            double result = 0d;
            int count = node.Children.Count;
            var comps = new List<(double, double)>(count);

            // Gather all child scores and their probabilities
            foreach (var pair in node.Policy(this._prior))
            {
                comps.Add((pair.probability, score(pair.child)));
            }

            // Find the largest logit for stability
            double scaled = double.NegativeInfinity;
            foreach (var (probability, value) in comps)
            {
                double logit = value / this._tau;
                if (logit > scaled) scaled = logit;
            }

            // Aggregate probability-weighted exponentials
            foreach (var (probability, value) in comps)
            {
                double logit = value / this._tau;
                double exp = Math.Exp(logit - scaled);
                result += probability * exp;
            }

            // Compute the final softmax formula and return
            return this._tau * (Math.Log(result) + scaled);
        }
    }

    /// <summary>
    /// This model uses a soft minimum (risk-averse average) over scores.
    /// </summary>
    internal sealed class SoftMinModel : Model
    {
        private readonly double _tau, _prior;

        /// <summary>
        /// Creates a soft minimum model with the given mixing parameters.
        /// </summary>
        /// <param name="tau">Adjusts how much to soften the minimum.</param>
        /// <param name="prior">Smoothing for rarely visited moves.</param>
        internal SoftMinModel(double tau, double prior = 0d)
        {
            this._tau = Math.Max(1e-6d, tau);
            this._prior = Math.Max(0d, prior);
        }

        /// <summary>
        /// Computes the soft minimum (risk-aware) value over all possible moves.
        /// </summary>
        internal override double Backup(in Node node, in Score score)
        {
            double result = 0d;
            int count = node.Children.Count;
            var comps = new List<(double, double)>(count);

            // Gather all child scores and their probabilities
            foreach (var pair in node.Policy(this._prior))
            {
                comps.Add((pair.probability, score(pair.child)));
            }

            // Find the largest logit for stability
            double scaled = double.NegativeInfinity;
            foreach (var (probability, value) in comps)
            {
                double logit = -value / this._tau;
                if (logit > scaled) scaled = logit;
            }

            // Aggregate probability-weighted exponentials
            foreach (var (probability, value) in comps)
            {
                double logit = -value / this._tau;
                double exp = Math.Exp(logit - scaled);
                result += probability * exp;
            }

            // Compute the final softmin formula and return
            return -this._tau * (Math.Log(result) + scaled);
        }
    }
}
