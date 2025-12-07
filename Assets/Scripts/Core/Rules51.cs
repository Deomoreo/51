using System;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Core
{
    /// <summary>
    /// Core rules engine for Cirulla/51. Contains all game logic, no Unity dependencies.
    /// </summary>
    public static class Rules51
    {
        private static readonly Random Rng = new Random();

        #region Game Creation & Deck Initialization

        /// <summary>
        /// Creates a new 4-player Cirulla game with a shuffled deck.
        /// </summary>
        public static GameState CreateNewGame()
        {
            var state = new GameState(4);
            state.DealerIndex = Rng.Next(4); // Random initial dealer
            InitializeDeck(state);
            ShuffleDeck(state);
            return state;
        }

        /// <summary>
        /// Create a new game with a specified number of players and perform the initial deal.
        /// Useful for tests to create deterministic player counts.
        /// </summary>
        public static GameState CreateNewGame(int numPlayers)
        {
            var state = new GameState(numPlayers);
            state.DealerIndex = Rng.Next(numPlayers);
            // Deal initial cards (this will initialize and shuffle internally)
            DealInitialCards(state);
            return state;
        }

        /// <summary>
        /// Create a new game using a provided deck ordering (useful for tests). The provided deck
        /// will be copied and shuffled each attempt to avoid invalid initial table (e.g., two aces).
        /// </summary>
        public static GameState CreateNewGame(int numPlayers, List<Card> providedDeck)
        {
            var state = new GameState(numPlayers);
            state.DealerIndex = Rng.Next(numPlayers);

            bool validDeal = false;
            int maxAttempts = 100;
            int attempts = 0;

            while (!validDeal && attempts < maxAttempts)
            {
                attempts++;
                // copy provided deck into a temporary local list and use that as the draw source
                var tempDeck = new List<Card>(providedDeck);
                // assign to state's existing Deck list by clearing and adding
                state.Deck.Clear();
                foreach (var c in tempDeck) state.Deck.Add(c);
                ShuffleDeck(state);

                // clear hands and table
                foreach (var p in state.Players) p.Hand.Clear();
                state.Table.Clear();

                // deal 3 cards to each player starting from player to left of dealer
                int firstPlayerIndex = (state.DealerIndex - 1 + state.NumPlayers) % state.NumPlayers;
                for (int round = 0; round < 3; round++)
                {
                    for (int offset = 0; offset < state.NumPlayers; offset++)
                    {
                        int playerIndex = (firstPlayerIndex + offset) % state.NumPlayers;
                        state.Players[playerIndex].Hand.Add(DrawCard(state));
                    }
                }

                // deal 4 to table
                for (int i = 0; i < 4; i++) state.Table.Add(DrawCard(state));

                int aceCount = state.Table.Count(c => c.IsAce);
                if (aceCount < 2) validDeal = true;
            }

            state.CurrentPlayerIndex = (state.DealerIndex - 1 + state.NumPlayers) % state.NumPlayers;
            return state;
        }

        /// <summary>
        /// Initializes a standard 40-card Italian deck.
        /// Ranks: 1 (Ace), 2-7, 8 (Jack), 9 (Horse), 10 (King) for each suit.
        /// </summary>
        private static void InitializeDeck(GameState state)
        {
            state.Deck.Clear();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                for (int rank = 1; rank <= 10; rank++)
                {
                    state.Deck.Add(new Card(suit, rank));
                }
            }
        }

        /// <summary>
        /// Shuffles the deck using Fisher–Yates algorithm.
        /// </summary>
        private static void ShuffleDeck(GameState state)
        {
            var deck = state.Deck;
            int n = deck.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = Rng.Next(i + 1);
                var temp = deck[i];
                deck[i] = deck[j];
                deck[j] = temp;
            }
        }

        #endregion

        #region Player Selection Helper

        /// <summary>
        /// Given a played card and a (possibly empty) selection of table cards, determine whether this
        /// constitutes a valid move for the specified player. If valid, returns true and outputs the
        /// corresponding Move object. This is intended to be used by UI/player logic to validate
        /// manual selections rather than reimplementing rules logic.
        /// </summary>
        public static bool TryGetMoveFromSelection(GameState state, int playerIndex, Card playedCard, List<Card> selectedTableCards, out Move move)
        {
            move = null;
            if (state == null) return false;

            // Get all valid moves for player. This already enforces forced-capture rules and matta logic.
            var validMoves = GetValidMoves(state, playerIndex);

            // Normalize selection
            var selection = selectedTableCards == null ? new List<Card>() : new List<Card>(selectedTableCards);

            foreach (var m in validMoves)
            {
                if (!m.PlayedCard.Equals(playedCard)) continue;

                // PlayOnly: selection must be empty
                if (m.Type == MoveType.PlayOnly)
                {
                    if (selection.Count == 0)
                    {
                        // return the existing Move instance from validMoves so identity checks work
                        move = m;
                        return true;
                    }
                    continue;
                }

                // For capture moves: captured cards must match selection as sets (order independent)
                var moveCaptured = m.CapturedCards ?? new List<Card>();
                if (moveCaptured.Count != selection.Count) continue;

                bool same = !moveCaptured.Except(selection).Any() && !selection.Except(moveCaptured).Any();
                if (same)
                {
                    // return the existing Move instance from validMoves so identity checks work
                    move = m;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns all valid moves that match the provided played card and the selected table cards.
        /// Useful when multiple equivalent captures exist (e.g., matta assignments) so the UI
        /// can present alternatives to the player.
        /// </summary>
        public static List<Move> GetMatchingMovesFromSelection(GameState state, int playerIndex, Card playedCard, List<Card> selectedTableCards)
        {
            var matches = new List<Move>();
            if (state == null) return matches;

            var validMoves = GetValidMoves(state, playerIndex);
            var selection = selectedTableCards == null ? new List<Card>() : new List<Card>(selectedTableCards);

            foreach (var m in validMoves)
            {
                if (!m.PlayedCard.Equals(playedCard)) continue;

                if (m.Type == MoveType.PlayOnly)
                {
                    if (selection.Count == 0) matches.Add(new Move(playerIndex, playedCard, MoveType.PlayOnly));
                    continue;
                }

                var moveCaptured = m.CapturedCards ?? new List<Card>();
                // If selection is empty, we will later decide whether to present alternatives.
                // For now collect non-PlayOnly moves; actual filtering happens after the loop.
                if (selection.Count == 0)
                {
                    matches.Add(m);
                    continue;
                }

                if (moveCaptured.Count != selection.Count) continue;

                bool same = !moveCaptured.Except(selection).Any() && !selection.Except(moveCaptured).Any();
                if (same)
                {
                    matches.Add(m);
                }
            }

            // If selection was empty, apply heuristic to avoid showing alternatives when an "obvious"
            // single capture exists. Priority order: CaptureEqual -> CaptureSum -> Capture15 -> AceCapture.
            if (selectedTableCards == null || selectedTableCards.Count == 0)
            {
                if (matches.Count > 1)
                {
                    var byType = matches.GroupBy(x => x.Type).ToDictionary(g => g.Key, g => g.ToList());
                    // Refined priority: prefer exact captures, then captures-to-15, then sum-to-value, then ace-specific
                    MoveType[] priority = new[] { MoveType.CaptureEqual, MoveType.Capture15, MoveType.CaptureSum, MoveType.AceCapture };
                    foreach (var t in priority)
                    {
                        if (byType.TryGetValue(t, out var list))
                        {
                            if (list.Count == 1)
                            {
                                // only one obvious move of this type -> return it alone
                                return new List<Move> { list[0] };
                            }
                            else
                            {
                                // multiple moves of the same priority type -> present those only
                                return list;
                            }
                        }
                    }
                    // fallback: return all matches
                }
            }

            return matches;
        }

        #endregion
        #region Initial Deal & Two-Aces Check

        /// <summary>
        /// Deals the initial 3 cards to each player and 4 cards to the table.
        /// If the table contains two or more Aces, redeal from scratch.
        /// </summary>
        public static void DealInitialCards(GameState state)
        {
            bool validDeal = false;
            int maxAttempts = 100; // Prevent infinite loops in edge cases
            int attempts = 0;

            while (!validDeal && attempts < maxAttempts)
            {
                attempts++;

                // Reset hands, table, and deck
                foreach (var player in state.Players)
                {
                    player.Hand.Clear();
                }
                state.Table.Clear();

                InitializeDeck(state);
                ShuffleDeck(state);

                // Deal 3 cards to each player, starting from the player to the left of the dealer (clockwise)
                int firstPlayerIndex = (state.DealerIndex - 1 + state.NumPlayers) % state.NumPlayers;
                for (int round = 0; round < 3; round++)
                {
                    for (int offset = 0; offset < state.NumPlayers; offset++)
                    {
                        int playerIndex = (firstPlayerIndex + offset) % state.NumPlayers;
                        state.Players[playerIndex].Hand.Add(DrawCard(state));
                    }
                }

                // Deal 4 cards face-up to the table
                for (int i = 0; i < 4; i++)
                {
                    state.Table.Add(DrawCard(state));
                }

                // Check for two or more Aces on the table
                int aceCount = state.Table.Count(c => c.IsAce);
                if (aceCount < 2)
                {
                    validDeal = true;
                }
            }

            // Set the first player to act (giocatore di mano = player to the left of dealer, clockwise)
            state.CurrentPlayerIndex = (state.DealerIndex - 1 + state.NumPlayers) % state.NumPlayers;
        }

        /// <summary>
        /// Draws a card from the top of the deck.
        /// </summary>
        private static Card DrawCard(GameState state)
        {
            if (state.Deck.Count == 0)
                throw new InvalidOperationException("Cannot draw from an empty deck.");

            var card = state.Deck[0];
            state.Deck.RemoveAt(0);
            return card;
        }

        #endregion

        #region Valid Moves Generation

        /// <summary>
        /// Returns all valid moves for the specified player.
        /// Enforces the forced-capture rule: if any capture is possible, PlayOnly is not allowed.
        /// </summary>
        public static List<Move> GetValidMoves(GameState state, int playerIndex)
        {
            var player = state.Players[playerIndex];
            var validMoves = new List<Move>();

            foreach (var card in player.Hand)
            {
                // Matta (7 di Coppe) can act as a wildcard for captures.
                // Generate captures for all possible values when Matta is played.
                if (card.IsMatta)
                {
                    // Matta can capture any single card on the table (equal-value capture for each table card)
                    foreach (var tableCard in state.Table)
                    {
                        validMoves.Add(new Move(playerIndex, card, MoveType.CaptureEqual, new List<Card> { tableCard }));
                    }

                    // Matta as Ace (1) for Ace capture rules
                    var aceMoves = GetAceCaptureMoves(state, playerIndex, card);
                    validMoves.AddRange(aceMoves);

                    // Matta as Ace (1) for sum-to-15 captures
                    var sum15AsAce = GetSumTo15CapturesWithEffectiveValue(state, playerIndex, card, effectivePlayedValue: 1);
                    validMoves.AddRange(sum15AsAce);
                }
                // Check for Ace special rules
                else if (card.IsAce)
                {
                    var aceMoves = GetAceCaptureMoves(state, playerIndex, card);
                    validMoves.AddRange(aceMoves);
                }
                else
                {
                    // Equal value captures
                    var equalMoves = GetEqualValueCaptures(state, playerIndex, card);
                    validMoves.AddRange(equalMoves);

                    // Sum to card value captures
                    var sumMoves = GetSumToValueCaptures(state, playerIndex, card);
                    validMoves.AddRange(sumMoves);

                    // Sum to 15 with card captures
                    var sum15Moves = GetSumTo15Captures(state, playerIndex, card);
                    validMoves.AddRange(sum15Moves);
                }
            }

            // If no captures are possible, allow PlayOnly for each card
            if (validMoves.Count == 0)
            {
                foreach (var card in player.Hand)
                {
                    validMoves.Add(new Move(playerIndex, card, MoveType.PlayOnly));
                }
            }

            return validMoves;
        }

        /// <summary>
        /// Returns moves for capturing exactly one card of equal value.
        /// </summary>
        private static List<Move> GetEqualValueCaptures(GameState state, int playerIndex, Card playedCard)
        {
            var moves = new List<Move>();
            foreach (var tableCard in state.Table)
            {
                if (tableCard.Value == playedCard.Value)
                {
                    moves.Add(new Move(playerIndex, playedCard, MoveType.CaptureEqual, new List<Card> { tableCard }));
                }
            }
            return moves;
        }

        /// <summary>
        /// Sum-to-15 variant that uses an effective played card value (e.g., Matta counting as 1).
        /// </summary>
        private static List<Move> GetSumTo15CapturesWithEffectiveValue(GameState state, int playerIndex, Card playedCard, int effectivePlayedValue)
        {
            var moves = new List<Move>();
            int targetSum = 15 - effectivePlayedValue;

            if (targetSum <= 0) return moves;

            var subsets = GetAllSubsets(state.Table);
            foreach (var subset in subsets)
            {
                if (subset.Count == 0) continue;

                int sum = subset.Sum(c => c.Value);
                if (sum == targetSum)
                {
                    moves.Add(new Move(playerIndex, playedCard, MoveType.Capture15, new List<Card>(subset)));
                }
            }

            return moves;
        }

        /// <summary>
        /// Returns moves for capturing cards whose sum equals the played card's value (not including played card).
        /// </summary>
        private static List<Move> GetSumToValueCaptures(GameState state, int playerIndex, Card playedCard)
        {
            var moves = new List<Move>();
            int targetSum = playedCard.Value;

            // Generate all non-empty subsets of table cards (exclude single cards, already covered by equal-value)
            var subsets = GetAllSubsets(state.Table);
            foreach (var subset in subsets)
            {
                if (subset.Count < 2) continue; // Skip single-card subsets (equal-value handles those)

                int sum = subset.Sum(c => c.Value);
                if (sum == targetSum)
                {
                    moves.Add(new Move(playerIndex, playedCard, MoveType.CaptureSum, new List<Card>(subset)));
                }
            }

            return moves;
        }

        /// <summary>
        /// Returns moves for capturing cards whose sum + played card value = 15.
        /// </summary>
        private static List<Move> GetSumTo15Captures(GameState state, int playerIndex, Card playedCard)
        {
            var moves = new List<Move>();
            int targetSum = 15 - playedCard.Value;

            if (targetSum <= 0) return moves; // Can't sum to 15 if played card >= 15

            var subsets = GetAllSubsets(state.Table);
            foreach (var subset in subsets)
            {
                if (subset.Count == 0) continue;

                int sum = subset.Sum(c => c.Value);
                if (sum == targetSum)
                {
                    moves.Add(new Move(playerIndex, playedCard, MoveType.Capture15, new List<Card>(subset)));
                }
            }

            return moves;
        }

        /// <summary>
        /// Returns moves for Ace capture special rules:
        /// - If there's at least one Ace on the table, capture ONE Ace.
        /// - If no Aces on table but there are other cards, capture ALL cards.
        /// - If table is empty, PlayOnly (no capture).
        /// </summary>
        private static List<Move> GetAceCaptureMoves(GameState state, int playerIndex, Card playedCard)
        {
            var moves = new List<Move>();

            var tableAces = state.Table.Where(c => c.IsAce).ToList();
            if (tableAces.Count > 0)
            {
                // Capture one Ace (add a move for each Ace on table)
                foreach (var ace in tableAces)
                {
                    moves.Add(new Move(playerIndex, playedCard, MoveType.AceCapture, new List<Card> { ace }));
                }
            }
            else if (state.Table.Count > 0)
            {
                // Capture all cards on the table
                moves.Add(new Move(playerIndex, playedCard, MoveType.AceCapture, new List<Card>(state.Table)));
            }
            else
            {
                // Table is empty, no capture possible
                moves.Add(new Move(playerIndex, playedCard, MoveType.PlayOnly));
            }

            return moves;
        }

        /// <summary>
        /// Generates all non-empty subsets of a list.
        /// </summary>
        private static List<List<Card>> GetAllSubsets(List<Card> cards)
        {
            var subsets = new List<List<Card>>();
            int n = cards.Count;
            int totalSubsets = (1 << n); // 2^n

            for (int i = 1; i < totalSubsets; i++) // Start at 1 to exclude empty set
            {
                var subset = new List<Card>();
                for (int j = 0; j < n; j++)
                {
                    if ((i & (1 << j)) != 0)
                    {
                        subset.Add(cards[j]);
                    }
                }
                subsets.Add(subset);
            }

            return subsets;
        }

        #endregion

        #region Apply Move

        /// <summary>
        /// Applies the chosen move to the game state, updating hands, table, captured cards, and Scopa count.
        /// </summary>
        public static void ApplyMove(GameState state, Move move)
        {
            var player = state.Players[move.PlayerIndex];

            // Remove played card from hand
            if (!player.Hand.Remove(move.PlayedCard))
                throw new InvalidOperationException($"Player {move.PlayerIndex} does not have {move.PlayedCard} in hand.");

            if (move.Type == MoveType.PlayOnly)
            {
                // No capture, add played card to the table
                state.Table.Add(move.PlayedCard);
            }
            else
            {
                // Matta capture: no logs
                // Capture: add played card and captured cards to player's captured pile
                player.CapturedCards.Add(move.PlayedCard);
                foreach (var capturedCard in move.CapturedCards)
                {
                    if (!state.Table.Remove(capturedCard))
                        throw new InvalidOperationException($"Card {capturedCard} is not on the table.");

                    player.CapturedCards.Add(capturedCard);
                }

                // Update last capture player
                state.LastCapturePlayerIndex = move.PlayerIndex;

                // Check for Scopa: all cards cleared from table
                if (state.Table.Count == 0)
                {
                    bool isLastPlay = state.Deck.Count == 0 && state.Players.All(p => p.Hand.Count == 0);
                    if (!isLastPlay)
                    {
                        player.ScopaCount++;
                        player.ScopaCards.Add(move.PlayedCard);
                    }
                }
            }

            // Advance to next player (clockwise)
            state.CurrentPlayerIndex = (state.CurrentPlayerIndex - 1 + state.NumPlayers) % state.NumPlayers;
        }

        #endregion
    }
}
