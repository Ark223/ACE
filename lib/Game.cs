using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static Ace.Extensions;

namespace Ace
{
    /// <summary>
    /// Options for initializing a <see cref="Game"/> instance.
    /// </summary>
    public sealed class GameOptions
    {
        /// <summary>
        /// Deal in PBN format (use "..." for unknown hands).
        /// </summary>
        public string Deal { get; set; } = string.Empty;

        /// <summary>
        /// The player who declared the current contract.
        /// </summary>
        public Player Declarer { get; set; } = Player.North;

        /// <summary>
        /// The contract to be played (e.g., 3NT, 4H, ...).
        /// </summary>
        public Contract Contract { get; set; } = Contract.None;

        /// <summary>
        /// Optional per-player hand constraints (for filtering playouts).
        /// </summary>
        public ConstraintSet Constraints { get; set; } = ConstraintSet.Empty;
    }

    /// <summary>
    /// A public interface for a bridge game, separating API from implementation.
    /// </summary>
    internal interface IGame
    {
        /// <summary>
        /// Attempts to play a move specified as a string (e.g., "SA" for Ace of Spades).
        /// </summary>
        /// <param name="move">String representation of the card played.</param>
        /// <param name="check">If true, validates the move before playing.</param>
        /// <returns>True if move is legal and accepted; otherwise, false.</returns>
        bool Play(string move, bool check = true);

        /// <summary>
        /// Attempts to play a move specified as a <see cref="Card"/> object.
        /// </summary>
        /// <param name="card">The card played.</param>
        /// <param name="check">If true, validates the move before playing.</param>
        /// <returns>True if move is legal and accepted; otherwise, false.</returns>
        bool Play(in Card card, bool check = true);

        /// <summary>
        /// Returns a list of all pseudo-legal moves available to the current player.
        /// </summary>
        /// <returns>
        /// A list of <see cref="Card"/> objects representing all available plays.
        /// </returns>
        List<Card> GetMoves();

        /// <summary>
        /// Returns the number of tricks won by the partnership of the given player.
        /// </summary>
        /// <param name="player">Player whose side is being checked.</param>
        /// <returns>Tricks taken by the player's partnership.</returns>
        int GetTricks(Player player);

        /// <summary>
        /// Checks whether the move is pseudo-legal in the current game state.
        /// </summary>
        /// <param name="move">String representation of the card.</param>
        /// <returns>True if the move is legal; otherwise, false.</returns>
        bool IsLegal(string move);

        /// <summary>
        /// Checks whether the move is pseudo-legal in the current game state.
        /// </summary>
        /// <param name="card">Card to be played in this state.</param>
        /// <returns>True if the move is legal; otherwise, false.</returns>
        bool IsLegal(in Card card);

        /// <summary>
        /// Determines whether the game has ended (all tricks played).
        /// </summary>
        /// <returns>True if the game is finished; otherwise, false.</returns>
        bool IsOver();

        /// <summary>
        /// Reveals and sets the dummy's hand after the opening lead.
        /// </summary>
        /// <param name="hand">Dummy's revealed hand in PBN format.</param>
        /// <returns>True if the dummy hand is set; otherwise, false.</returns>
        bool SetDummy(string hand);

        /// <summary>
        /// Undoes the last move, returning the game to its previous state.
        /// </summary>
        /// <returns>True if an action was performed; otherwise, false.</returns>
        bool Undo();

        /// <summary>
        /// Redoes the next move, advancing the game to its next state.
        /// </summary>
        /// <returns>True if an action was performed; otherwise, false.</returns>
        bool Redo();

        /// <summary>
        /// Creates a deep, independent copy of the current game state.
        /// </summary>
        /// <returns>A new <see cref="Game"/> instance with identical state.</returns>
        Game Clone();
    }

    /// <summary>
    /// Provides the state and core functions for managing and playing a bridge game.
    /// </summary>
    public sealed partial class Game : IGame, IDisposable
    {
        /// <summary>
        /// Gets the bitmasks representing all cards held by each player.
        /// </summary>
        internal ulong[] Hands { get; private set; }

        /// <summary>
        /// Gets the bitmasks representing all cards played by each player.
        /// </summary>
        internal ulong[] Plays { get; private set; }

        /// <summary>
        /// Gets the count of unknown cards remaining in each player's hand.
        /// </summary>
        internal byte[] Lefts { get; private set; }

        /// <summary>
        /// Gets the number of tricks taken so far by each partnership.
        /// </summary>
        internal byte[] Tricks { get; private set; }

        /// <summary>
        /// Gets the bitmask representing all cards not known to be in any hand.
        /// </summary>
        internal ulong Hidden { get; private set; }

        /// <summary>
        /// Gets the bitmask indicating, for each player, which suits are void.
        /// </summary>
        internal ushort Voids { get; private set; }

        /// <summary>
        /// Gets the current trick state, including played cards.
        /// </summary>
        public Trick Trick { get; private set; }

        /// <summary>
        /// Gets the player currently on lead and next to act.
        /// </summary>
        public Player Leader { get; private set; }

        /// <summary>
        /// Gets the declarer for the current contract.
        /// </summary>
        public Player Declarer { get; }

        /// <summary>
        /// Gets the declarer's partner, who acts as dummy.
        /// </summary>
        public Player Dummy { get; }

        /// <summary>
        /// Gets the contract being played in the current game.
        /// </summary>
        public Contract Contract { get; }

        /// <summary>
        /// Gets the per-player constraint set used for filtering deals.
        /// </summary>
        public ConstraintSet Constraints { get; }

        /// <summary>
        /// Event fired when a move has been successfully played.
        /// </summary>
        public event Action<Card> MovePlayed;

        /// <summary>
        /// Stores previous game states for undo operations.
        /// </summary>
        private Stack<History> UndoStack { get; set; }

        /// <summary>
        /// Stores reverted game states for redo operations.
        /// </summary>
        private Stack<History> RedoStack { get; set; }

        /// <summary>
        /// Represents a snapshot of game state for undo/redo.
        /// </summary>
        private struct History
        {
            internal Trick Trick;
            internal Player Player;
            internal ulong[] Hands;
            internal ulong[] Plays;
            internal byte[] Lefts;
            internal byte[] Taken;
            internal ulong Hidden;
            internal ushort Voids;

            /// <summary>
            /// Captures a full snapshot of the given <see cref="Game"/> state.
            /// </summary>
            /// <param name="game">Game instance to capture state from.</param>
            internal History(in Game game)
            {
                this.Player = game.Leader;
                this.Trick = game.Trick.Copy();
                this.Hands = game.Hands.ToArray();
                this.Plays = game.Plays.ToArray();
                this.Lefts = game.Lefts.ToArray();
                this.Taken = game.Tricks.ToArray();
                this.Hidden = game.Hidden;
                this.Voids = game.Voids;
            }

            /// <summary>
            /// Applies this stored snapshot to a <see cref="Game"/> instance.
            /// </summary>
            /// <param name="game">Game instance to restore state to.</param>
            /// <returns>Always true after applying the stored state.</returns>
            internal bool ApplyTo(in Game game)
            {
                game.Leader = this.Player;
                game.Trick = this.Trick.Copy();
                game.Hands = this.Hands.ToArray();
                game.Plays = this.Plays.ToArray();
                game.Lefts = this.Lefts.ToArray();
                game.Tricks = this.Taken.ToArray();
                game.Hidden = this.Hidden;
                game.Voids = this.Voids;
                return true;
            }
        }
    }

    public sealed partial class Game : IGame, IDisposable
    {
        /// <summary>
        /// A precomputed array containing all 52 cards in the deck.
        /// </summary>
        public static readonly Card[] Deck = Array.ConvertAll
        (
            Enumerable.Range(0, 52).ToArray(), Card.Create
        );

        /// <summary>
        /// Returns a bitmask of all cards currently held by any player.
        /// </summary>
        /// <returns>
        /// A 52-bit mask where each set bit indicates a card held in some hand.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong AllHeld()
        {
            return this.Hands[0] | this.Hands[1] | this.Hands[2] | this.Hands[3];
        }

        /// <summary>
        /// Returns a bitmask of all cards played by any player.
        /// </summary>
        /// <returns>
        /// A 52-bit mask with bits set for every card that has been played so far.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong AllPlayed()
        {
            return this.Plays[0] | this.Plays[1] | this.Plays[2] | this.Plays[3];
        }

        /// <summary>
        /// Returns a bitmask of all hidden or unknown cards (not held and not played).
        /// </summary>
        /// <returns>
        /// A 52-bit mask with bits set for the cards not in any hand or played.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong HiddenSet()
        {
            return (~this.AllHeld()) & ((1ul << 52) - 1ul);
        }

        /// <summary>
        /// Checks if the input player is void in the specified suit.
        /// </summary>
        /// <param name="suit">Suit to check for void status.</param>
        /// <param name="player">Player to check (or leader).</param>
        /// <returns>
        /// True if the player is void in the suit; otherwise, false.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasVoid(Suit suit, Player player)
        {
            return ((this.Voids >> (((int)player << 2) | (int)suit)) & 1) != 0;
        }

        /// <summary>
        /// Checks if the leading player is void in the specified suit.
        /// </summary>
        /// <param name="suit">Suit to check for void status.</param>
        /// <returns>
        /// True if the leader is void in the suit; otherwise, false.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasVoid(Suit suit)
        {
            return this.HasVoid(suit, this.Leader);
        }

        /// <summary>
        /// Marks the specified suit as void for the leading player.
        /// </summary>
        /// <param name="suit">Suit to be marked as void.</param>
        /// <param name="player">Player to check (or leader).</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetVoid(Suit suit)
        {
            this.Voids |= (ushort)(1 << (((int)this.Leader << 2) | (int)suit));
        }

        /// <summary>
        /// Returns a bitmask covering all 13 cards of specified suit within deck.
        /// </summary>
        /// <param name="suit">Suit for which to get the mask.</param>
        /// <returns>
        /// A 52-bit mask with 13 bits corresponding to the specified suit.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong SuitMask(Suit suit)
        {
            return 0x1ffful << ((int)suit * 13); // 0x1fff = 13 bits set to 1
        }

        /// <summary>
        /// Determines the suit led for the current trick.
        /// </summary>
        /// <returns>The suit led in this trick.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Suit FirstLead()
        {
            return this.Trick.Any() ? this.Trick.Cards[0].Suit : Suit.NoTrump;
        }

        /// <summary>
        /// Determines the suit led for the current trick.
        /// </summary>
        /// <param name="card">Card being played.</param>
        /// <returns>The suit led in this trick.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Suit FirstLead(in Card card)
        {
            return this.Trick.Any() ? this.Trick.Cards[0].Suit : card.Suit;
        }

        /// <summary>
        /// Returns the number of unknown cards remaining in each player's hand.
        /// </summary>
        /// <returns>
        /// A sequence where each value is the count of unknown cards for that player.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] FindLefts()
        {
            return Array.ConvertAll(new int[] { 0, 1, 2, 3 }, seat =>
                (byte)(13 - Utilities.PopCount(this.Hands[seat])));
        }

        /// <summary>
        /// Returns the trick-taking priority of a card: trump (2), led suit (1), or other (0).
        /// </summary>
        /// <param name="card">Card to evaluate.</param>
        /// <param name="trump">Trump suit for this contract.</param>
        /// <param name="lead">The suit led for this trick.</param>
        /// <returns>2 for trump, 1 for led suit, 0 otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Priority(in Card card, Suit trump, Suit lead)
        {
            return trump != Suit.NoTrump && card.Suit ==
                trump ? 2 : card.Suit == lead ? 1 : 0;
        }
    }

    public sealed partial class Game : IGame, IDisposable
    {
        /// <summary>
        /// Marks the leader's hand as void in the given suit, then assigns all hidden
        /// <br></br>cards of that suit to the only other hand that can still hold them.
        /// </summary>
        /// <param name="suit">Suit in which the leader is void.</param>
        private void ApplyVoid(Suit suit)
        {
            // Mark the void
            this.SetVoid(suit);

            // Compute bitmask of hidden cards of that suit
            ulong hidden = this.Hidden & this.SuitMask(suit);
            if (hidden == 0ul) return;

            // Find the non-leading player with any unknown cards
            var targets = Enumerable.Range(0, 4).Where<int>(seat =>
                seat != (int)this.Leader && this.Lefts[seat] > 0);

            // Only proceed if exactly one candidate remains
            int target = targets.DefaultIfEmpty(-1).First();
            if (target == -1 || targets.Count() > 1) return;

            // Count the number of the hidden cards
            byte count = Utilities.PopCount(hidden);
            byte current = this.Lefts[target];
            count = Math.Min(count, current);

            // Assign hidden cards to target
            this.Hands[target] |= hidden;
            this.Lefts[target] -= count;
            this.Hidden &= ~hidden;
        }

        /// <summary>
        /// Determines the winner of a current trick and updates the game state.
        /// </summary>
        private void FinishTrick()
        {
            Player leader = this.Trick.Leader;
            Suit trump = this.Contract.Strain;
            Suit lead = this.Trick.Cards[0].Suit;

            // Find the index of the winning card for this trick
            var winner = Enumerable.Range(0, 4).OrderBy(index =>
            {
                ref Card card = ref this.Trick.Cards[index];
                int priority = -this.Priority(card, trump, lead);
                return (Priority: priority, CardRank: -card.Rank);
            });

            // Determine the player who won this trick
            var player = leader.Advance(winner.First());

            // Update trick counts for pairs
            this.Tricks[((int)player) & 1]++;

            // Update and prepare next trick
            this.Trick = new Trick(player);
            this.Leader = player;
        }

        /// <summary>
        /// Formats a player's hand as an array of strings, one per suit.
        /// </summary>
        /// <param name="player">Player whose hand to format.</param>
        /// <returns>Array of suit strings (e.g. "S AKQ").</returns>
        public IEnumerable<string> FormatHand(Player player)
        {
            // Get the player's known hand mask
            ulong hand = this.Hands[(int)player];

            // Check if player has still hidden cards
            bool lefts = this.Lefts[(int)player] > 0;

            // Loop through each suit in PBN order
            char[] suits = { 'C', 'D', 'H', 'S' };
            for (int suit = 3; suit >= 0; suit--)
            {
                // Start building a new suit line
                var builder = new StringBuilder(15);
                builder.Append(suits[suit]).Append(' ');

                // Extract cards of this suit from hand
                ulong mask = this.SuitMask((Suit)suit);
                ulong cards = (hand & mask) >> (suit * 13);

                // Append each known card in this suit
                for (int rank = 14; rank >= 2; --rank)
                {
                    if ((cards & (1ul << (rank - 2))) != 0)
                        builder.Append(Card.RankToChar[rank]);
                }

                // Check hidden cards and player's void status
                bool voids = this.HasVoid((Suit)suit, player);
                bool hidden = (this.Hidden & mask) != 0;

                // Add "?" if the player may still hold hidden cards
                if (hidden && lefts && !voids) builder.Append('?');

                // Add "-" if the player is void in this suit
                if (builder.Length == 2) builder.Append('-');

                // Return the formatted suit line
                yield return builder.ToString();
            }
        }
    }

    public sealed partial class Game : IGame, IDisposable
    {
        /// <summary>
        /// Initializes a new bridge game using the specified game options.
        /// </summary>
        /// <param name="options">Game options, including deal and contract.</param>
        public Game(GameOptions options)
        {
            this.Declarer = options.Declarer;
            this.Leader = this.Declarer.Next();
            this.Dummy = this.Leader.Next();

            this.Contract = options.Contract;
            this.Constraints = options.Constraints;
            this.Hands = PBN.ParseDeal(options.Deal);
            this.Trick = new Trick(this.Leader);

            this.UndoStack = new Stack<History>();
            this.RedoStack = new Stack<History>();

            this.Hidden = this.HiddenSet();
            this.Lefts = this.FindLefts();
            this.Plays = new ulong[4];
            this.Tricks = new byte[2];
        }

        /// <summary>
        /// Factory method for creating a new <see cref="Game"/> instance.
        /// </summary>
        /// <param name="options">Game options, including deal and contract.</param>
        /// <returns>A newly initialized <see cref="Game"/> instance.</returns>
        public static Game New(GameOptions options)
        {
            return new Game(options);
        }

        /// <summary>
        /// Attempts to play a move specified as a string (e.g., "SA" for Ace of Spades).
        /// </summary>
        /// <param name="move">String representation of the card played.</param>
        /// <param name="check">If true, validates the move before playing.</param>
        /// <returns>True if move is legal and accepted; otherwise, false.</returns>
        public bool Play(string move, bool check = true)
        {
            bool parsed = Card.TryParse(move, out Card card);
            return parsed && this.Play(card, check);
        }

        /// <summary>
        /// Attempts to play a move specified as a <see cref="Card"/> object.
        /// </summary>
        /// <param name="card">The card played.</param>
        /// <param name="check">If true, validates the move before playing.</param>
        /// <returns>True if move is legal and accepted; otherwise, false.</returns>
        public bool Play(in Card card, bool check = true)
        {
            // Check if the move is legal according to rules
            if (check && !this.IsLegal(card)) return false;

            // Determine lead suit and index
            Suit lead = this.FirstLead(card);
            ulong bit = 1ul << card.Index();

            // Save the snapshot of the game state
            this.UndoStack.Push(new History(this));
            this.RedoStack.Clear();

            // Record a void if did not follow the suit
            if (card.Suit != lead) this.ApplyVoid(lead);

            // Remove hidden card and consume unknown slot
            if ((this.Hands[(int)this.Leader] & bit) == 0)
            {
                this.Hidden &= ~bit;
                this.Lefts[(int)this.Leader]--;
            }

            // Remove card from the player's hand
            this.Hands[(int)this.Leader] &= ~bit;

            // Record new play and update trick
            this.Plays[(int)this.Leader] |= bit;
            this.Trick.Insert(card);

            // Finish the trick if 4 cards have been played
            if (this.Trick.Count == 4) this.FinishTrick();

            // Otherwise, pass lead to the next player
            else this.Leader = this.Leader.Next();

            // Notify that a move was played
            this.MovePlayed?.Invoke(card);
            return true;
        }

        /// <summary>
        /// Returns a list of all pseudo-legal moves available to the current player.
        /// </summary>
        /// <returns>
        /// A list of <see cref="Card"/> objects representing all available plays.
        /// </returns>
        public List<Card> GetMoves()
        {
            var moves = new List<Card>();
            Suit lead = this.FirstLead();

            // Get the current leader's known hand mask
            ulong hand = this.Hands[(int)this.Leader];

            // Collect candidates for this player
            ulong unplayed = ~this.AllPlayed();
            ulong available = unplayed & hand;

            // Add possible cards from hidden pool
            if (this.Lefts[(int)this.Leader] > 0)
                available |= unplayed & this.Hidden;

            // Does the leader have any cards of the lead suit?
            bool has_lead = (hand & this.SuitMask(lead)) != 0;

            // Restrict cards to the led suit if must follow
            bool must_follow = this.Trick.Any() && has_lead;
            if (must_follow) available &= this.SuitMask(lead);

            while (available != 0)
            {
                // Get the card and remove it from candidate set
                ulong bit = available & (ulong)-(long)available;
                available ^= bit;

                // Calculate the card's index and retrieve it
                byte index = Utilities.TrailingZeroCount(bit);
                ref readonly Card card = ref Game.Deck[index];

                // Can't play a suit that's already void
                if (this.HasVoid(card.Suit)) continue;
                moves.Add(card);
            }
            return moves;
        }

        /// <summary>
        /// Returns the number of tricks won by the partnership of the given player.
        /// </summary>
        /// <param name="player">Player whose side is being checked.</param>
        /// <returns>Tricks taken by the player's partnership.</returns>
        public int GetTricks(Player player)
        {
            return this.Tricks[((int)player) & 1];
        }

        /// <summary>
        /// Checks whether the move is pseudo-legal in the current game state.
        /// </summary>
        /// <param name="move">String representation of the card.</param>
        /// <returns>True if the move is legal; otherwise, false.</returns>
        public bool IsLegal(string move)
        {
            bool parsed = Card.TryParse(move, out Card card);
            return parsed && this.IsLegal(card);
        }

        /// <summary>
        /// Checks whether the move is pseudo-legal in the current game state.
        /// </summary>
        /// <param name="card">Card to be played in this state.</param>
        /// <returns>True if the move is legal; otherwise, false.</returns>
        public bool IsLegal(in Card card)
        {
            ulong bit = 1ul << card.Index();
            Suit lead = this.FirstLead(card);

            // Get the current leader's known hand mask
            ulong hand = this.Hands[(int)this.Leader];

            // Can't discard off-suit if still has the suit led
            bool has_lead = (hand & this.SuitMask(lead)) != 0;
            if (has_lead && card.Suit != lead) return false;

            // Check whether card remains unseen
            bool hidden = (this.Hidden & bit) != 0;
            bool has_card = (hand & bit) != 0;

            // Card must be held or come from hidden pool
            bool lefts = this.Lefts[(int)this.Leader] > 0;
            if (!has_card && (!hidden || !lefts)) return false;

            // Return if the exact card was already played
            if ((this.AllPlayed() & bit) != 0) return false;

            // Can't play a suit that is void
            return !this.HasVoid(card.Suit);
        }

        /// <summary>
        /// Determines whether the game has ended (all tricks played).
        /// </summary>
        /// <returns>True if the game is finished; otherwise, false.</returns>
        public bool IsOver()
        {
            return (this.Tricks[0] + this.Tricks[1]) >= 13;
        }

        /// <summary>
        /// Reveals and sets the dummy's hand after the opening lead.
        /// </summary>
        /// <param name="hand">Dummy's revealed hand in PBN format.</param>
        /// <returns>True if the dummy hand is set; otherwise, false.</returns>
        public bool SetDummy(string hand)
        {
            // Must be right after opening lead
            if (this.Tricks[0] != 0) return false;
            if (this.Tricks[1] != 0) return false;
            if (this.Trick.Count != 1) return false;

            // Must contain exactly 13 cards
            ulong dummy = PBN.ParseHand(hand);
            int count = Utilities.PopCount(dummy);
            if (count != 13) return false;

            // Assign cards by updating bitmasks
            this.Hands[(int)this.Dummy] = dummy;
            this.Lefts[(int)this.Dummy] = 0;
            this.Hidden &= ~dummy;
            return true;
        }

        /// <summary>
        /// Undoes the last move, returning the game to its previous state.
        /// </summary>
        /// <returns>True if an action was performed; otherwise, false.</returns>
        public bool Undo()
        {
            // Cannot undo as missing previous moves
            if (!this.UndoStack.Any()) return false;

            // Signal a state change for engine
            this.MovePlayed?.Invoke(Card.None);

            // Save state and restore previous one
            this.RedoStack.Push(new History(this));
            return this.UndoStack.Pop().ApplyTo(this);
        }

        /// <summary>
        /// Redoes the next move, advancing the game to its next state.
        /// </summary>
        /// <returns>True if an action was performed; otherwise, false.</returns>
        public bool Redo()
        {
            // Cannot redo as missing further moves
            if (!this.RedoStack.Any()) return false;

            // Signal a state change for engine
            this.MovePlayed?.Invoke(Card.None);

            // Save state and restore the next one
            this.UndoStack.Push(new History(this));
            return this.RedoStack.Pop().ApplyTo(this);
        }

        /// <summary>
        /// Creates a deep, independent copy of the current game state.
        /// </summary>
        /// <returns>A new <see cref="Game"/> instance with identical state.</returns>
        public Game Clone()
        {
            // Shallow copy the game state instance
            var copy = (Game)this.MemberwiseClone();

            // Deep copy the core properties
            copy.Trick = this.Trick.Copy();
            copy.Hands = this.Hands.ToArray();
            copy.Plays = this.Plays.ToArray();
            copy.Lefts = this.Lefts.ToArray();
            copy.Tricks = this.Tricks.ToArray();

            var undo = this.UndoStack.Reverse();
            var redo = this.RedoStack.Reverse();

            // Clone stacks while preserving LIFO order
            copy.UndoStack = new Stack<History>(undo);
            copy.RedoStack = new Stack<History>(redo);
            return copy;
        }

        /// <summary>
        /// Returns a string representation of the current game state.
        /// </summary>
        /// <returns>A formatted view of the bridge game table.</returns>
        public override string ToString()
        {
            // Format each player's hand as an array of strings
            var hands = Array.ConvertAll(new[] { 0, 1, 2, 3 },
                seat => this.FormatHand((Player)seat).ToArray());

            // Compute display width to align West's hand properly
            int width = Math.Max(hands[3].Max(x => x.Length), 7);

            // Padding to align top and bottom hands
            string indent = new string(' ', width + 2);

            // Shift South's hand to the centered bottom
            var south = hands[2].Select(x => indent + x);

            // Shift North's hand lines as well to the centered top
            var north = hands[0].Select(x => indent + x).ToArray();

            // Format West's hand lines with padding so they align nicely
            var west = hands[3].Select(x => x.PadRight(width)).ToArray();

            // Prepare prefixes for each pair's trick counts
            string ns_prefix = $" NS: {this.Tricks[0],-3} ";
            string ew_prefix = $" EW: {this.Tricks[1],-3} ";

            // Insert trick counts into North's hand block
            string[] block = new string[4]
            {
                north[0], ns_prefix + north[1].Substring(9),
                ew_prefix + north[2].Substring(9), north[3]
            };

            // West lead affects placeholder alignment
            bool west_side = this.Leader == Player.West;

            // Prepare card display from the current trick
            var trick = Enumerable.Repeat("  ", 4).ToArray();
            trick[(int)this.Leader] = west_side ? "? " : " ?";

            // Fill seats with played cards and leading side
            for (int idx = 0; idx < this.Trick.Count; ++idx)
            {
                Player player = this.Trick.Leader.Advance(idx);
                trick[(int)player] = $"{this.Trick.Cards[idx]}";
            }

            // Build the full table view line by line
            return string.Join(Environment.NewLine, new[]
            {
                // Display North's hand with trick counter
                string.Join(Environment.NewLine, block), "",

                // Draw center with the current trick
                $"{west[0]}  +-----+  {hands[1][0]}",
                $"{west[1]}  | {trick[0]}  |  {hands[1][1]}",
                $"{west[2]}  |{trick[3]} {trick[1]}|  {hands[1][2]}",
                $"{west[3]}  | {trick[2]}  |  {hands[1][3]}",

                // Add the lower border of the trick area
                new string(' ', width) + "  +-----+", "",

                // Display South's hand at the bottom
                string.Join(Environment.NewLine, south)
            });
        }
    }

    public sealed partial class Game : IGame, IDisposable
    {
        /// <summary>
        /// Creates a <see cref="Sampler"/> instance for the current game state.
        /// </summary>
        /// <returns>
        /// A new <see cref="Sampler"/> object ready to generate and evaluate deals.
        /// </returns>
        internal Sampler Sampling()
        {
            return new Sampler(this);
        }

        /// <summary>
        /// Finalizer for <see cref="Game"/> instance.
        /// </summary>
        ~Game() => this.Release();

        /// <summary>
        /// Clears internal undo and redo stacks.
        /// </summary>
        private void Release()
        {
            this.UndoStack.Clear();
            this.RedoStack.Clear();
        }

        /// <summary>
        /// Releases all resources used by the <see cref="Game"/> instance.
        /// </summary>
        public void Dispose()
        {
            this.Release();
            GC.SuppressFinalize(this);
        }
    }
}
