using System;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Core
{
    public static class Rules51
    {
        private static Random Rng = new Random();

        /// <summary>Rimescolate ripetibili da qui in poi (il tutorial parte da una distribuzione nota).</summary>
        public static void Reseed(int seed) => Rng = new Random(seed);

        #region Game Creation & Deck Initialization

        /// <summary>
        public static GameState CreateNewGame()
        {
            var state = new GameState(4);
            state.DealerIndex = Rng.Next(4); // Random initial dealer
            InitializeDeck(state);
            ShuffleDeck(state);
            return state;
        }

        /// <summary>
        public static GameState CreateNewGame(int numPlayers)
        {
            var state = new GameState(numPlayers);
            state.DealerIndex = Rng.Next(numPlayers);
            // Deal initial cards (this will initialize and shuffle internally)
            DealInitialCards(state);
            return state;
        }

        /// <summary>
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

        #region Initial Deal & Two-Aces Check

        /// <summary>
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
        public static List<Move> GetValidMoves(GameState state, int playerIndex)
        {
            var player = state.Players[playerIndex];
            var validMoves = new List<Move>();
            var cardsWithCaptures = new HashSet<Card>(); // Track which cards have captures

            foreach (var card in player.Hand)
            {
                // Matta (7 di Coppe) is played as a normal 7 - the special visual is only for accuso hints
                // It does NOT act as a wildcard during play
                if (card.IsAce)
                {
                    // Check for Ace special rules
                    var aceMoves = GetAceCaptureMoves(state, playerIndex, card);
                    validMoves.AddRange(aceMoves);
                    // Track if this ace has capture moves (not just PlayOnly)
                    if (aceMoves.Any(m => m.Type != MoveType.PlayOnly))
                    {
                        cardsWithCaptures.Add(card);
                    }
                }
                else
                {
                    // Normal card logic (including Matta which plays as a 7)
                    // Equal value captures
                    var equalMoves = GetEqualValueCaptures(state, playerIndex, card);
                    validMoves.AddRange(equalMoves);

                    // Precedenza della carta uguale (SPEC §10.1): se in tavola c'e' la carta uguale si prende
                    // quella, niente somme ne' 15.
                    var sumMoves = equalMoves.Count > 0 ? new List<Move>() : GetSumToValueCaptures(state, playerIndex, card);
                    validMoves.AddRange(sumMoves);

                    var sum15Moves = equalMoves.Count > 0 ? new List<Move>() : GetSumTo15Captures(state, playerIndex, card);
                    validMoves.AddRange(sum15Moves);
                    
                    // Track if this card has any captures
                    if (equalMoves.Count > 0 || sumMoves.Count > 0 || sum15Moves.Count > 0)
                    {
                        cardsWithCaptures.Add(card);
                    }
                }
            }

            // If no captures are possible for ANY card, allow PlayOnly for each card
            if (cardsWithCaptures.Count == 0)
            {
                foreach (var card in player.Hand)
                {
                    validMoves.Add(new Move(playerIndex, card, MoveType.PlayOnly));
                }
            }
            else
            {
                // Some cards can capture - add PlayOnly only for cards that CAN'T capture
                // This allows the player to choose to play non-capturing cards
                foreach (var card in player.Hand)
                {
                    if (!cardsWithCaptures.Contains(card))
                    {
                        validMoves.Add(new Move(playerIndex, card, MoveType.PlayOnly));
                    }
                }
            }

            return validMoves;
        }

        /// <summary>
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
