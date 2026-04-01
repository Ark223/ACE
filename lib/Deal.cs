using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static Ace.Extensions;

namespace Ace
{
    /// <summary>
    /// Represents a fully specified bridge deal for simulation purposes.
    /// </summary>
    internal sealed partial class Deal
    {
        private string _pbn;
        private Trick _trick;
        private Player _leader;
        private List<Card> _moves;

        private readonly Suit _trump;
        private readonly Player _origin;
        private readonly byte[] _tricks;

        private readonly List<Card>[] _hands;
        private readonly List<string> _history;

        /// <summary>
        /// Gets the current trick in progress.
        /// </summary>
        internal Trick Trick => this._trick;

        /// <summary>
        /// Gets the player who is currently on lead.
        /// </summary>
        internal Player Leader => this._leader;

        /// <summary>
        /// Gets the current hands of all four players.
        /// </summary>
        internal List<Card>[] Hands => this._hands;

        /// <summary>
        /// Gets all legal moves in the current position.
        /// </summary>
        internal IReadOnlyList<Card> Moves
        {
            get => this._moves ?? this.GetMoves();
        }

        /// <summary>
        /// Gets or sets the PBN representation of the deal.
        /// </summary>
        internal string Pbn
        {
            get => this._pbn;
            set => this._pbn = value ?? string.Empty;
        }

        /// <summary>
        /// Initializes a new <see cref="Deal"/> from full hands and current trick state.
        /// </summary>
        /// <param name="deal">List of four player hands representing the full deal.</param>
        /// <param name="trick">Current trick state, containing played cards to the table.</param>
        /// <param name="trump">Trump suit for the deal (or <see cref="Suit.NoTrump"/>).</param>
        /// <param name="tricks">Array of trick counts to track tricks won so far.</param>
        internal Deal(in List<Card>[] deal, Trick trick, Suit trump, byte[] tricks)
        {
            this._hands = deal;
            this._trump = trump;

            this._trick = trick.Copy();
            this._leader = trick.Leader;
            this._origin = this._leader;

            this._tricks = tricks.ToArray();
            this._history = new List<string>();
        }
    }

    internal sealed partial class Deal
    {
        /// <summary>
        /// Returns the trick-taking priority of the specified card.
        /// </summary>
        /// <param name="card">Card to evaluate.</param>
        /// <param name="lead">The suit led for this trick.</param>
        /// <returns>A card's priority category for this trick.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Priority(in Card card, Suit lead)
        {
            return this._trump != Suit.NoTrump && card.Suit ==
                this._trump ? 2 : card.Suit == lead ? 1 : 0;
        }

        /// <summary>
        /// Determines the winner of a current trick and updates the world state.
        /// </summary>
        private void FinishTrick()
        {
            Player leader = this._trick.Leader;
            Suit lead = this._trick.Cards[0].Suit;

            // Find the index of the winning card for this trick
            var winner = Enumerable.Range(0, 4).OrderBy(index =>
            {
                ref var card = ref this._trick.Cards[index];
                int priority = -this.Priority(card, lead);
                return (Prio: priority, Rank: -card.Rank);
            });

            // Determine the player who won this trick
            var player = leader.Advance(winner.First());

            // Update trick counts for pairs
            this._tricks[((int)player) & 1]++;

            // Update and prepare next trick
            this._trick = new Trick(player);
            this._leader = player;
        }

        /// <summary>
        /// Returns a list of all legal moves available to the current player.
        /// </summary>
        /// <returns>
        /// A list of <see cref="Card"/> objects representing all available plays.
        /// </returns>
        private List<Card> GetMoves()
        {
            int leader = (int)this._leader;
            var hand = this._hands[leader];

            // On first lead, any card can be played
            if (this._trick.Count == 0) return hand;

            // Otherwise, must follow suit if able
            Suit suit = this._trick.Cards[0].Suit;

            // Filter all cards matching the lead suit
            var follow = hand.Where(c => c.Suit == suit);
            return follow.Any() ? follow.ToList() : hand;
        }
    }

    internal sealed partial class Deal
    {
        /// <summary>
        /// Removes all played cards from each player's hand according to bitmask.
        /// </summary>
        /// <param name="plays">Array of bitmasks indicating played cards.</param>
        internal void CutHands(ulong[] plays)
        {
            for (int seat = 0; seat < 4; seat++)
            {
                this._hands[seat].RemoveAll(card => 0ul !=
                    ((1ul << card.Index()) & plays[seat]));
            }
        }

        /// <summary>
        /// Indicates whether the deal is finished and no moves can be made.
        /// </summary>
        /// <returns>True if all hands are empty; otherwise, false.</returns>
        internal bool IsOver()
        {
            return this._hands.All(hand => hand.Count == 0);
        }

        /// <summary>
        /// Plays a specified card for the current leader.
        /// </summary>
        /// <param name="card">Card to be played.</param>
        /// <param name="replay">Add card to the trick.</param>
        internal void Play(in Card card, bool replay = false)
        {
            // Remove this card from the player's hand
            this._hands[(int)this._leader].Remove(card);
            string play = card.ToString();

            // Store this play only if required
            if (!replay) this._trick.Insert(card);
            this._history.Add($"{play[1]}{play[0]}");

            // Finish the trick if 4 cards have been played
            if (this._trick.Count == 4) this.FinishTrick();

            // Otherwise, pass lead to the next player
            else this._leader = this._leader.Next();

            // Refresh cached legal moves after a play
            if (!replay) this._moves = this.GetMoves();
        }

        /// <summary>
        /// Replays all cards from the current trick in order.
        /// </summary>
        /// <returns>Always true when the replay is done.</returns>
        internal bool Replay()
        {
            for (int idx = 0; idx < this._trick.Count; ++idx)
            {
                this.Play(this._trick.Cards[idx], true);
            }
            return true;
        }

        /// <summary>
        /// Computes the number of tricks available to the leader from the current state.
        /// </summary>
        /// <param name="deal">PBN string representation of all four player hands.</param>
        /// <returns>A number of tricks the leader can take at the current position.</returns>
        internal Dictionary<Card, int> Solve(string deal)
        {
            // Collect all cards played so far in a sequence
            string commands = string.Join(" ", this._history);

            // Initialize a double-dummy solver with the sample deal
            using (var dds = new DDS(deal, this._trump, this._origin))
            {
                // Replay moves to reach the current deal state
                if (!commands.Equals("")) dds.Execute(commands);

                // Evaluate all legal moves from current position with DDS
                return this.Moves.ToDictionary(c => c, c => dds.Tricks(c));
            }
        }

        /// <summary>
        /// Computes total tricks for the leader's pair (actual or projected).
        /// </summary>
        /// <returns>Tricks won or projected for the leader's side.</returns>
        internal Dictionary<Card, int> Tricks()
        {
            // Track tricks already won by pair
            int side = ((int)this._leader) & 1;
            int tricks = this._tricks[side];

            // Save the final trick count at terminal
            var results = new Dictionary<Card, int>();
            if (this.IsOver())
            {
                results[Card.None] = tricks;
                return results;
            }

            // Solve for the remaining tricks using DDS
            foreach (var entry in this.Solve(this._pbn))
            {
                results[entry.Key] = tricks + entry.Value;
            }
            return results;
        }

        /// <summary>
        /// Converts the deal to PBN (Portable Bridge Notation) string format.
        /// </summary>
        /// <returns>A PBN string representing all four player hands.</returns>
        internal string ToPBN()
        {
            var hands = new string[4];
            for (int seat = 0; seat < 4; seat++)
            {
                // Build lookup table for all cards
                bool[,] has_card = new bool[4, 15];

                // Mark each card present in this hand
                foreach (Card card in this._hands[seat])
                {
                    has_card[(int)card.Suit, card.Rank] = true;
                }

                // Prepare rank strings per suit
                string[] ranks = new string[4];
                for (int idx = 0; idx < 4; idx++)
                {
                    // Maintain PBN order (A-high)
                    Suit suit = PBN.PbnOrder[idx];
                    var builder = new StringBuilder(13);

                    // Go through ranks from Ace down to 2
                    for (int rank = 14; rank >= 2; rank--)
                    {
                        if (has_card[(int)suit, rank])
                        {
                            builder.Append(Card.RankToChar[rank]);
                        }
                    }
                    ranks[idx] = builder.ToString();
                }

                // Join suits as "S.H.D.C" for this seat
                hands[seat] = string.Join(".", ranks);
            }

            // Add extra prefix as required by DDS
            return "N:" + string.Join(" ", hands);
        }
    }
}
