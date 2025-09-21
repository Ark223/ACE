using static Ace.Extensions;

namespace Ace
{
    /// <summary>
    /// Represents a bridge contract (level and strain).
    /// </summary>
    public sealed class Contract
    {
        internal int Level { get; }
        internal Suit Strain { get; }

        /// <summary>
        /// Initializes a new <see cref="Contract"/> with the specified level and strain.
        /// </summary>
        /// <param name="level">Contract level.</param>
        /// <param name="strain">Contract strain.</param>
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
        /// <param name="contract">String contract (first character is level, second is strain).</param>
        /// <returns>A parsed <see cref="Contract"/>, or <see cref="None"/> if input is empty.</returns>
        public static Contract Parse(string contract)
        {
            if (string.IsNullOrEmpty(contract))
            {
                return Contract.None;
            }
            Suit strain = Suit.NoTrump;
            int level = contract[0] - '0';
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
        /// <param name="contract">String contract (first character is level, second is strain).</param>
        /// <param name="result">Parsed <see cref="Contract"/>, or <see cref="None"/> if failed.</param>
        /// <returns>True if parsing the contract succeeded; otherwise, false.</returns>
        public static bool TryParse(string contract, out Contract result)
        {
            result = Contract.None;
            if (string.IsNullOrEmpty(contract))
            {
                return false;
            }
            if (contract[0] < '1' || contract[0] > '7')
            {
                return false;
            }
            Suit strain = Suit.NoTrump;
            int level = contract[0] - '0';
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
