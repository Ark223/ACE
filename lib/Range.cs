namespace Ace
{
    /// <summary>
    /// Represents an interval [Min, Max] for bridge game constraints.
    /// </summary>
    public readonly struct Range
    {
        internal int Min { get; }
        internal int Max { get; }

        /// <summary>
        /// Initializes a new <see cref="Range"/> with specified minimum and maximum values.
        /// </summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        public Range(int min, int max)
        {
            this.Min = min;
            this.Max = max;
        }

        /// <summary>
        /// Returns a range representing "any value up to limit".
        /// </summary>
        /// <param name="limit">Maximum allowed value.</param>
        /// <returns>A new range from 0 to limit.</returns>
        internal static Range Any(int limit = 40)
        {
            return new Range(0, limit);
        }

        /// <summary>
        /// Determines if the range contains the specified value.
        /// </summary>
        /// <param name="value">Value to test.</param>
        /// <returns>
        /// True if value is in [Min, Max]; otherwise, false.
        /// </returns>
        internal bool Contains(int value)
        {
            return value >= this.Min && value <= this.Max;
        }

        /// <summary>
        /// Returns a string representation of the range (e.g., "[0,13]").
        /// </summary>
        /// <returns>
        /// A string displaying the minimum and maximum values.
        /// </returns>
        public override string ToString()
        {
            return $"[{this.Min},{this.Max}]";
        }
    }
}
