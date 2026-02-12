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
        private int _shift = -8;
        private uint _key = 0;

        private readonly Suit _trump;
        private readonly Player _origin;
        private readonly byte[] _tricks;
        private readonly List<Card>[] _hands;
        private readonly List<string> _history;

        /// <summary>
        /// Gets the current hands of all four players.
        /// </summary>
        internal List<Card>[] Hands => this._hands;

        /// <summary>
        /// Gets the player who is currently on lead.
        /// </summary>
        internal Player Leader => this._leader;

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
        /// Returns a bitmask for the current leader's hand.
        /// </summary>
        /// <returns>A 52-bit mask of the leader's hand.</returns>
        private ulong HandMask()
        {
            ulong mask = 0ul;
            var hand = this._hands[(int)this._leader];
            for (int idx = 0; idx < hand.Count; idx++)
            {
                mask |= 1ul << hand[idx].Index();
            }
            return mask;
        }

        /// <summary>
        /// Computes an info-set key for the current player to act.
        /// </summary>
        /// <returns>A key identifying the current infoset.</returns>
        private Key GetStateKey()
        {
            ulong leader = (ulong)this._leader << 52;
            return new Key(this._key, this.HandMask() | leader);
        }

        /// <summary>
        /// Updates the public state key after a card is played.
        /// </summary>
        /// <param name="card">Card that was played.</param>
        /// <param name="player">Player who just acted.</param>
        private void UpdateKey(in Card card, Player player)
        {
            uint played = (uint)card.Index() | ((uint)player << 6);
            this._key |= played << (this._shift += 8);
        }

        /// <summary>
        /// Returns the trick-taking priority of a card: trump (2), led suit (1), or other (0).
        /// </summary>
        /// <param name="card">Card to evaluate.</param>
        /// <param name="lead">The suit led for this trick.</param>
        /// <returns>2 for trump, 1 for led suit, 0 otherwise.</returns>
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
        /// Returns a list of all legal moves available to the current player.
        /// </summary>
        /// <returns>
        /// A list of <see cref="Card"/> objects representing all available plays.
        /// </returns>
        internal List<Card> GetMoves()
        {
            int leader = (int)this._leader;
            ref var hand = ref this._hands[leader];

            // On first lead, any card can be played
            if (this._trick.Count == 0) return hand;

            // Otherwise, must follow suit if able
            Suit suit = this._trick.Cards[0].Suit;

            // Filter all cards matching the lead suit
            var follow = hand.Where(c => c.Suit == suit);
            return follow.Any() ? follow.ToList() : hand;
        }

        /// <summary>
        /// Returns true if the deal is finished and no further moves can be made.
        /// </summary>
        /// <returns>True if all player hands are empty; otherwise, false.</returns>
        internal bool IsOver()
        {
            return this._hands.All(hand => hand.Count == 0);
        }

        /// <summary>
        /// Plays a specified card for the current leader.
        /// </summary>
        /// <param name="card">Card to be played.</param>
        /// <param name="replay">Add card to the trick.</param>
        /// <returns>An info-state key after this play.</returns>
        internal Key Play(in Card card, bool replay = false)
        {
            // Remove the card from the player’s hand
            this._hands[(int)this._leader].Remove(card);

            // Add this card to the current trick
            if (!replay) this._trick.Insert(card);

            // Update public key with this play
            this.UpdateKey(card, this._leader);

            // Get the card representation
            string play = card.ToString();

            // Record this card play in a move history
            this._history.Add($"{play[1]}{play[0]}");

            // Finish the trick if 4 cards have been played
            if (this._trick.Count == 4) this.FinishTrick();

            // Otherwise, pass lead to the next player
            else this._leader = this._leader.Next();

            // Return an info-state key for the next player
            return !replay ? this.GetStateKey() : Key.Zero;
        }

        /// <summary>
        /// Replays all cards from the current trick in order.
        /// </summary>
        /// <returns>Always true when the replay is done.</returns>
        internal bool Replay()
        {
            // Replay cards without adding to current trick
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
        internal int Solve(string deal)
        {
            // Collect all cards played so far in a sequence
            string commands = string.Join(" ", this._history);

            // Initialize a double-dummy solver with the sample deal
            using (var dds = new DDS(deal, this._trump, this._origin))
            {
                // Replay moves to reach the current deal state
                if (!commands.Equals("")) dds.Execute(commands);

                // Evaluate position
                return dds.Tricks();
            }
        }

        /// <summary>
        /// Computes total tricks for the leader’s pair (actual or projected).
        /// </summary>
        /// <returns>Tricks won or projected for the leader's side.</returns>
        internal int Tricks()
        {
            // Determine sides (0 = NS, 1 = EW)
            int side = ((int)this._leader) & 1;

            // Return tricks won if game has finished
            if (this.IsOver()) return this._tricks[side];

            // Solve for remaining tricks using DDS
            int remaining = this.Solve(this._pbn);

            // Calculate total tricks for the side
            return this._tricks[side] + remaining;
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
                // Build lookup table for card checking
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
                        // Check if player has this card
                        if (has_card[(int)suit, rank])
                        {
                            // Add rank character to the string
                            builder.Append(Card.RankToChar[rank]);
                        }
                    }

                    // Store string for this hand
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
