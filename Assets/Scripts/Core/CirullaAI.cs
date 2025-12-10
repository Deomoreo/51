using System;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Core
{
    // Forward declarations for types used by CirullaAI
    // These are defined in other files in Project51.Core
    /// <summary>
    /// Difficulty levels for AI players.
    /// </summary>
    public enum AIDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>
    /// Strategic AI for Cirulla/51 that makes intelligent decisions.
    /// Prioritizes high-value cards and avoids giving opponents easy Scope.
    /// </summary>
    public class CirullaAI
    {
        private readonly System.Random rng;
        private readonly AIDifficulty difficulty;

        public CirullaAI(AIDifficulty difficulty = AIDifficulty.Medium)
        {
            this.difficulty = difficulty;
            this.rng = new System.Random();
        }

        /// <summary>
        /// Choose the best move for the given player from valid moves.
        /// </summary>
        public Move ChooseMove(GameState state, int playerIndex, List<Move> validMoves)
        {
            if (validMoves == null || validMoves.Count == 0)
                return null;

            if (validMoves.Count == 1)
                return validMoves[0];

            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    return ChooseMoveEasy(validMoves);
                case AIDifficulty.Medium:
                    return ChooseMoveMedium(state, validMoves);
                case AIDifficulty.Hard:
                    return ChooseMoveHard(state, playerIndex, validMoves);
                default:
                    return validMoves[0];
            }
        }

        /// <summary>
        /// Easy AI: Random selection with slight preference for captures.
        /// </summary>
        private Move ChooseMoveEasy(List<Move> validMoves)
        {
            var captures = validMoves.Where(m => m.Type != MoveType.PlayOnly).ToList();
            
            if (captures.Count > 0 && rng.NextDouble() > 0.3)
            {
                return captures[rng.Next(captures.Count)];
            }
            
            return validMoves[rng.Next(validMoves.Count)];
        }

        /// <summary>
        /// Medium AI: Prioritizes valuable captures.
        /// </summary>
        private Move ChooseMoveMedium(GameState state, List<Move> validMoves)
        {
            var captures = validMoves.Where(m => m.Type != MoveType.PlayOnly).ToList();

            if (captures.Count > 0)
            {
                // Score each capture move
                var scoredMoves = captures.Select(m => new
                {
                    Move = m,
                    Score = ScoreCaptureMove(m)
                }).OrderByDescending(x => x.Score).ToList();

                return scoredMoves[0].Move;
            }

            // For PlayOnly, choose the card that's least likely to help opponents
            return ChooseBestPlayOnly(state, validMoves);
        }

        /// <summary>
        /// Hard AI: Full strategic analysis.
        /// </summary>
        private Move ChooseMoveHard(GameState state, int playerIndex, List<Move> validMoves)
        {
            var captures = validMoves.Where(m => m.Type != MoveType.PlayOnly).ToList();

            if (captures.Count > 0)
            {
                // Score each capture with full analysis
                var scoredMoves = captures.Select(m => new
                {
                    Move = m,
                    Score = ScoreCaptureMoveAdvanced(state, playerIndex, m)
                }).OrderByDescending(x => x.Score).ToList();

                return scoredMoves[0].Move;
            }

            // Strategic PlayOnly
            return ChooseBestPlayOnlyAdvanced(state, playerIndex, validMoves);
        }

        /// <summary>
        /// Basic scoring for capture moves.
        /// </summary>
        private int ScoreCaptureMove(Move move)
        {
            int score = 0;

            // Base score for number of cards captured
            score += (move.CapturedCards?.Count ?? 0) * 10;

            // Bonus for capturing Sette Bello
            if (move.CapturedCards?.Any(c => c.IsSetteBello) == true)
                score += 100;

            // Bonus for capturing Denari
            score += (move.CapturedCards?.Count(c => c.Suit == Suit.Denari) ?? 0) * 15;

            // Bonus for capturing 7s (good for Primiera)
            score += (move.CapturedCards?.Count(c => c.Rank == 7) ?? 0) * 20;

            // Bonus for capturing 6s (second best for Primiera)
            score += (move.CapturedCards?.Count(c => c.Rank == 6) ?? 0) * 10;

            // Bonus for Aces (useful cards)
            score += (move.CapturedCards?.Count(c => c.IsAce) ?? 0) * 12;

            // Bonus for potential Scopa (would need to check if table would be cleared)
            // This is handled by checking if CapturedCards == all table cards
            
            return score;
        }

        /// <summary>
        /// Advanced scoring considering game context.
        /// </summary>
        private int ScoreCaptureMoveAdvanced(GameState state, int playerIndex, Move move)
        {
            int score = ScoreCaptureMove(move);

            var player = state.Players[playerIndex];
            var capturedDenari = player.CapturedCards.Count(c => c.Suit == Suit.Denari);
            var moveDenari = move.CapturedCards?.Count(c => c.Suit == Suit.Denari) ?? 0;

            // Bonus if this helps reach denari majority
            if (capturedDenari + moveDenari >= 6)
                score += 25;

            // Bonus for capturing cards needed for Grande
            var denariRanks = player.CapturedCards.Where(c => c.Suit == Suit.Denari).Select(c => c.Rank).ToHashSet();
            foreach (var card in move.CapturedCards ?? new List<Card>())
            {
                if (card.Suit == Suit.Denari)
                {
                    if (card.Rank == 10 || card.Rank == 9 || card.Rank == 8)
                    {
                        // Check if this helps complete Grande
                        if (!denariRanks.Contains(card.Rank))
                            score += 15;
                    }
                    if (card.Rank == 1 || card.Rank == 2 || card.Rank == 3)
                    {
                        // Check if this helps complete Piccola
                        if (!denariRanks.Contains(card.Rank))
                            score += 10;
                    }
                }
            }

            // Scopa detection: if this capture clears the table, big bonus
            if (state.Table.Count == (move.CapturedCards?.Count ?? 0))
            {
                // Check it's not the last play
                bool isLastPlay = state.Deck.Count == 0 && state.Players.All(p => p.Hand.Count <= 1);
                if (!isLastPlay)
                    score += 50;
            }

            return score;
        }

        /// <summary>
        /// Choose the best card to play when no captures are available.
        /// </summary>
        private Move ChooseBestPlayOnly(GameState state, List<Move> playOnlyMoves)
        {
            // Avoid playing 7s (valuable for Primiera)
            // Avoid playing Denari (valuable for majority)
            // Prefer playing cards that are hard to capture (face cards)

            var scored = playOnlyMoves.Select(m => new
            {
                Move = m,
                Score = ScorePlayOnlyCard(m.PlayedCard)
            }).OrderByDescending(x => x.Score).ToList();

            return scored[0].Move;
        }

        /// <summary>
        /// Advanced PlayOnly selection.
        /// </summary>
        private Move ChooseBestPlayOnlyAdvanced(GameState state, int playerIndex, List<Move> playOnlyMoves)
        {
            // Analyze what cards opponents might be able to capture
            var scored = playOnlyMoves.Select(m => new
            {
                Move = m,
                Score = ScorePlayOnlyAdvanced(state, m.PlayedCard)
            }).OrderByDescending(x => x.Score).ToList();

            return scored[0].Move;
        }

        /// <summary>
        /// Score a card for PlayOnly (higher = better to discard).
        /// </summary>
        private int ScorePlayOnlyCard(Card card)
        {
            int score = 0;

            // Prefer discarding face cards (hard for opponents to use)
            if (card.Rank >= 8)
                score += 20;

            // Avoid discarding 7s (valuable for Primiera)
            if (card.Rank == 7)
                score -= 30;

            // Avoid discarding 6s (second best Primiera)
            if (card.Rank == 6)
                score -= 15;

            // Avoid discarding Denari
            if (card.Suit == Suit.Denari)
                score -= 25;

            // Avoid discarding Aces
            if (card.IsAce)
                score -= 20;

            // Avoid discarding Sette Bello especially
            if (card.IsSetteBello)
                score -= 100;

            // Avoid discarding Matta
            if (card.IsMatta)
                score -= 50;

            return score;
        }

        /// <summary>
        /// Advanced PlayOnly scoring considering table state.
        /// </summary>
        private int ScorePlayOnlyAdvanced(GameState state, Card card)
        {
            int score = ScorePlayOnlyCard(card);

            // Avoid creating a sum that equals 15 with table cards
            int tableSum = state.Table.Sum(c => c.Value);
            if (tableSum + card.Value == 15)
                score -= 30; // Opponent could capture with any card

            // Avoid matching a single table card value
            if (state.Table.Any(c => c.Value == card.Value))
                score -= 20;

            // Prefer creating awkward sums (hard to capture)
            if (tableSum + card.Value > 15 && tableSum + card.Value < 21)
                score += 10;

            return score;
        }
    }
}
