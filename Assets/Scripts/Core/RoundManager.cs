using System;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Core
{
    public enum AccusoType
    {
        None,
        Cirulla,
        Decino,
        Dealer15,
        Dealer30
    }

    public class RoundManager
    {
        private readonly GameState state;
        private readonly Random rng;

        /// <summary>
        /// Event raised when a player successfully declares an accuso.
        /// Parameters: playerIndex, accusoType, hand (List of cards)
        /// </summary>
        public event Action<int, AccusoType, List<Card>> OnAccusoDeclared;
        
        /// <summary>
        /// Event raised when new hands are dealt (after previous hands are empty).
        /// TurnController can subscribe to this to check for new accusi.
        /// </summary>
        public event Action OnNewHandsDealt;
        
        /// <summary>
        /// Event raised after initial deal but BEFORE dealer accuso is processed.
        /// This allows all players to declare Cirulla/Decino before dealer 15/30 is checked.
        /// </summary>
        public event Action OnInitialHandsDealt;

        /// <summary>
        /// Event raised quando il dealer fa Dealer15/Dealer30 sulle carte tavolo a inizio
        /// smazzata. Parametri: dealerIndex, AccusoType (Dealer15/Dealer30), le carte tavolo
        /// SPAZZATE VIA (copia presa PRIMA che TakeTableByPlayer svuoti state.Table - altrimenti
        /// chi ascolta l'evento le troverebbe gia' vuote). Prima non esisteva: le carte
        /// sparivano silenziosamente dal tavolo senza che nessuno potesse mostrarlo (nessun
        /// reveal), perche' ProcessDealerInitialAccuso gira PRIMA che qualunque render/UI esista.
        /// </summary>
        public event Action<int, AccusoType, List<Card>> OnDealerAccusoDeclared;

        /// <summary>
        /// Numero della mano corrente all'interno della smazzata (parte da 1).
        /// </summary>
        public int CurrentHandNumber { get; private set; } = 1;

        /// <summary>
        /// Numero totale di mani della smazzata corrente. Calcolato una volta in StartSmazzata.
        /// </summary>
        public int TotalHands { get; private set; }

        public RoundManager(GameState state, Random rng = null)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.rng = rng ?? new Random();
        }

        private MatchRules GetRules()
        {
            try
            {
                var t = System.Type.GetType("Project51.Unity.GameSceneInitializer, Assembly-CSharp");
                if (t != null)
                {
                    var prop = t.GetProperty("ActiveConfig", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var cfg = prop?.GetValue(null) as MatchConfig;
                    if (cfg?.Rules != null)
                        return cfg.Rules;
                }
            }
            catch
            {
                // ignore
            }

            return MatchRules.Default;
        }

        private int ApplyAccusiRulePoints(int basePoints)
        {
            var rules = GetRules();
            if (rules == null)
                return basePoints;

            if (!rules.EnableAccusi)
                return 0;

            float mul = rules.AccusiPointMultiplier;
            if (mul <= 0f)
                return 0;

            return (int)System.Math.Round(basePoints * mul, MidpointRounding.AwayFromZero);
        }

        public void StartSmazzata()
        {
            Rules51.DealInitialCards(state);

            const int deckSize = 40;
            const int cardsPerPlayerPerHand = 3;
            TotalHands = (deckSize - state.Table.Count) / (cardsPerPlayerPerHand * state.NumPlayers);

            foreach (var p in state.Players)
            { p.AccusiPoints = 0; p.RoundAccusiPoints = 0; }

            OnInitialHandsDealt?.Invoke();

            ProcessDealerInitialAccuso();
        }

        private void ProcessDealerInitialAccuso()
        {
            int dealer = state.DealerIndex;
            var table = state.Table;
            if (table == null || table.Count == 0) return;

            bool hasMatta = table.Any(c => c.IsMatta);

            int baseSum = table.Sum(c => c.IsMatta ? 0 : c.Value);

            // If no matta, straightforward checks
            if (!hasMatta)
            {
                if (baseSum == 15)
                {
                    DeclareDealerAccuso(dealer, AccusoType.Dealer15, 1);
                }
                else if (baseSum == 30)
                {
                    DeclareDealerAccuso(dealer, AccusoType.Dealer30, 2);
                }
                return;
            }

            // With matta(s), try to find an assignment (matta can be 1..10) that yields 15 or 30
            int mattaCount = table.Count(c => c.IsMatta);
            // brute force small search: mattaCount <= number of matta on table (normally 0 or 1)
            var values = Enumerable.Range(1, 10).ToArray();
            bool assigned = false;
            int bestType = 0; // 15 -> 1, 30 -> 2
            // Try to see if any combination yields 15 or 30
            int[] assign = new int[mattaCount];
            void Recurse(int idx)
            {
                if (assigned) return;
                if (idx == mattaCount)
                {
                    int s = baseSum + assign.Sum();
                    if (s == 15)
                    {
                        assigned = true; bestType = 15; return;
                    }
                    if (s == 30)
                    {
                        assigned = true; bestType = 30; return;
                    }
                    return;
                }
                for (int v = 1; v <= 10; v++)
                {
                    assign[idx] = v;
                    Recurse(idx + 1);
                    if (assigned) return;
                }
            }
            Recurse(0);

            if (assigned)
            {
                if (bestType == 15)
                {
                    DeclareDealerAccuso(dealer, AccusoType.Dealer15, 1);
                }
                else if (bestType == 30)
                {
                    DeclareDealerAccuso(dealer, AccusoType.Dealer30, 2);
                }
            }
        }

        private void DeclareDealerAccuso(int dealer, AccusoType type, int basePoints)
        {
            state.Players[dealer].AccusiPoints += ApplyAccusiRulePoints(basePoints);
            state.Players[dealer].RoundAccusiPoints += ApplyAccusiRulePoints(basePoints);
            // Copia PRIMA di svuotare il tavolo: TakeTableByPlayer chiama state.Table.Clear().
            var sweptCards = new List<Card>(state.Table);
            TakeTableByPlayer(dealer);
            OnDealerAccusoDeclared?.Invoke(dealer, type, sweptCards);
        }

        private void TakeTableByPlayer(int playerIndex)
        {
            var player = state.Players[playerIndex];
            foreach (var c in state.Table)
                player.CapturedCards.Add(c);
            state.Table.Clear();
            state.LastCapturePlayerIndex = playerIndex;
        }

        /// <summary>
        /// Player declares an accuso before playing (Cirulla or Decino)
        /// Returns true if accuso accepted and points awarded.
        /// </summary>
        public bool TryPlayerAccuso(int playerIndex, AccusoType accuso)
        {
            var rules = GetRules();
            if (rules != null && !rules.EnableAccusi)
                return false;

            if (accuso == AccusoType.Cirulla)
            {
                var hand = state.Players[playerIndex].Hand;
                if (AccusiChecker.IsCirulla(hand))
                {
                    state.Players[playerIndex].AccusiPoints += ApplyAccusiRulePoints(3);
                    state.Players[playerIndex].RoundAccusiPoints += ApplyAccusiRulePoints(3);
                    OnAccusoDeclared?.Invoke(playerIndex, AccusoType.Cirulla, new List<Card>(hand));
                    return true;
                }
                return false;
            }
            else if (accuso == AccusoType.Decino)
            {
                var hand = state.Players[playerIndex].Hand;
                if (AccusiChecker.IsDecino(hand))
                {
                    state.Players[playerIndex].AccusiPoints += ApplyAccusiRulePoints(10);
                    state.Players[playerIndex].RoundAccusiPoints += ApplyAccusiRulePoints(10);
                    OnAccusoDeclared?.Invoke(playerIndex, AccusoType.Decino, new List<Card>(hand));
                    return true;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// Apply move and handle end-of-hand or smazzata transitions.
        /// </summary>
        public void ApplyMove(Move move)
        {
            Rules51.ApplyMove(state, move);

            // Check cappotto: if any player captured all denari (10)
            for (int i = 0; i < state.NumPlayers; i++)
            {
                int denari = state.Players[i].CapturedCards.Count(c => c.Suit == Suit.Denari);
                if (denari == 10)
                {
                    var rules = GetRules();
                    if (rules != null && !rules.CappottoEndsGameImmediately)
                    {
                        if (rules.CappottoBonusPoints > 0)
                            state.Players[i].TotalScore += rules.CappottoBonusPoints;
                    }
                    else
                    {
                        // Immediate game-level win; for now mark RoundEnded and give large bonus
                        state.RoundEnded = true;
                        state.Players[i].TotalScore += 1000; // sentinel for immediate win
                    }
                }
            }

            // If all hands empty, deal new hands or end smazzata
            bool allHandsEmpty = state.Players.All(p => p.Hand.Count == 0);
            if (allHandsEmpty)
            {
                // RESET AccusiPoints when hands are empty (before dealing new cards)
                // Players keep their total score, but accusi are only for the current hand
                for (int i = 0; i < state.NumPlayers; i++)
                {
                    state.Players[i].AccusiPoints = 0;
                }
                
                if (state.Deck.Count > 0)
                {
                    // deal new hands (3 each)
                    int firstPlayerIndex = (state.DealerIndex - 1 + state.NumPlayers) % state.NumPlayers;
                    for (int round = 0; round < 3; round++)
                    {
                        for (int offset = 0; offset < state.NumPlayers; offset++)
                        {
                            int playerIndex = (firstPlayerIndex + offset) % state.NumPlayers;
                            if (state.Deck.Count > 0)
                            {
                                var card = state.Deck[0];
                                state.Deck.RemoveAt(0);
                                state.Players[playerIndex].Hand.Add(card);
                            }
                        }
                    }
                    
                    // NEW: After dealing new hands, trigger event so TurnController can check for accusi
                    CurrentHandNumber++;
                    OnNewHandsDealt?.Invoke();
                }
                else
                {
                    EndSmazzata();
                }
            }
        }

        public void EndSmazzata()
        {
            if(state.RoundEnded)return;
            // Assign remaining table cards to last capture player
            if (state.LastCapturePlayerIndex >= 0 && state.Table.Count > 0)
            {
                var p = state.Players[state.LastCapturePlayerIndex];
                foreach (var c in state.Table) p.CapturedCards.Add(c);
                state.Table.Clear();
            }

            // Check cappotto: if any player has captured all 10 denari -> immediate win
            for (int i = 0; i < state.NumPlayers; i++)
            {
                int denari = state.Players[i].CapturedCards.Count(c => c.Suit == Suit.Denari);
                if (denari == 10)
                {
                    var rules = GetRules();
                    if (rules != null && !rules.CappottoEndsGameImmediately)
                    {
                        if (rules.CappottoBonusPoints > 0)
                            state.Players[i].TotalScore += rules.CappottoBonusPoints;
                        // continue scoring normally
                        break;
                    }

                    state.RoundEnded = true;
                    // give sentinel big score to indicate immediate win
                    state.Players[i].TotalScore += 1000;
                    return;
                }
            }

            // Compute points
            var points = PunteggioManager.CalculateSmazzataScores(state);

            // Add accusi points and apply to totals
            for (int i = 0; i < state.NumPlayers; i++)
            {
                points[i] += System.Math.Max(state.Players[i].RoundAccusiPoints, state.Players[i].AccusiPoints);
                state.Players[i].TotalScore += points[i];
            }

            state.RoundEnded = true;
        }
    }
}
