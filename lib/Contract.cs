using static Ace.Extensions;

namespace Ace
{
    /// <summary>
    /// Represents a bridge contract (level and strain).
    /// </summary>
    public sealed class Contract
    {
        /// <summary>
        /// Gets the contract level (from 1 through 7).
        /// </summary>
        internal int Level { get; }

        /// <summary>
        /// Gets the contract strain (Spade, Heart, Diamond, Club, or NT).
        /// </summary>
        internal Suit Strain { get; }

        /// <summary>
        /// Initializes a new <see cref="Contract"/> with the specified input.
        /// </summary>
        /// <param name="level">Contract level (from 1 to 7).</param>
        /// <param name="strain">Contract strain (trump or NT).</param>
        public Contract(int level, Suit strain)
        {
            this.Level = level;
            this.Strain = strain;
        }

        /// <summary>
        /// Gets a special <see cref="Contract"/> representing no contract ("passed out").
        /// </summary>
        internal static Contract None
        {
            get { return new Contract(0, Suit.NoTrump); }
        }

        /// <summary>
        /// Parses a string (e.g., "4S", "3NT") into a <see cref="Contract"/> object.
        /// </summary>
        /// <param name="contract">String contract (first char is level, second is strain).</param>
        /// <returns>A parsed <see cref="Contract"/>, or default value if input is empty.</returns>
        public static Contract Parse(string contract)
        {
            // Empty input means no contract
            if (string.IsNullOrEmpty(contract))
            {
                return Contract.None;
            }

            Suit strain = Suit.NoTrump;
            int level = contract[0] - '0';

            // Parse strain from second char
            switch (char.ToUpper(contract[1]))
            {
                case 'C': strain = Suit.Clubs;    break;
                case 'D': strain = Suit.Diamonds; break;
                case 'H': strain = Suit.Hearts;   break;
                case 'S': strain = Suit.Spades;   break;
                case 'N': strain = Suit.NoTrump;  break;
            }
            return new Contract(level, strain);
        }

        /// <summary>
        /// Attempts to parse a string (e.g., "4S", "3NT") into a <see cref="Contract"/> object.
        /// </summary>
        /// <param name="contract">String contract (first char is level, second is strain).</param>
        /// <param name="result">Parsed <see cref="Contract"/>, or default value if failed.</param>
        /// <returns>True if parsing the contract succeeded; otherwise, false.</returns>
        public static bool TryParse(string contract, out Contract result)
        {
            // Default to no contract
            result = Contract.None;

            // Empty input means no contract
            if (string.IsNullOrEmpty(contract))
                return false;

            // Contract level must be between 1 and 7
            if (contract[0] < '1' || contract[0] > '7')
                return false;

            Suit strain = Suit.NoTrump;
            int level = contract[0] - '0';

            // Parse strain from second char
            switch (char.ToUpper(contract[1]))
            {
                case 'C': strain = Suit.Clubs;    break;
                case 'D': strain = Suit.Diamonds; break;
                case 'H': strain = Suit.Hearts;   break;
                case 'S': strain = Suit.Spades;   break;
                case 'N': strain = Suit.NoTrump;  break;
                default: return false;
            }
            result = new Contract(level, strain);
            return true;
        }

        /// <summary>
        /// Returns a string representation of the contract (e.g., "4S", "3NT").
        /// </summary>
        /// <returns>
        /// A string showing the contract level and strain, or "-" if passed out.
        /// </returns>
        public override string ToString()
        {
            if (this.Level == 0) return "-";
            return this.Strain == Suit.NoTrump
                ? $"{this.Level}NT"
                : $"{this.Level}{this.Strain.ToString()[0]}";
        }
    }
}
