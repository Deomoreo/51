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
        /// Numero totale di mani della smazzata corrente.
        /// </summary>
        public int TotalHands { get; private set; }

        public RoundManager(GameState state, Random rng = null)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.rng = rng ?? new Random();
            // Stato gia' distribuito (client in rete che lo riceve dall'host): il contatore delle mani
            // si ricava dalle carte rimaste nel mazzo, altrimenti si leggeva "Mano 1 di 0".
            RecountHands();
        }

        /// <summary>Mani totali e mano corrente ricavate dallo stato (mazzo rimasto).</summary>
        private void RecountHands()
        {
            int perHand = CardsPerPlayerPerHand * state.NumPlayers;
            if (perHand <= 0) return;
            TotalHands = (DeckSize - InitialTableCards) / perHand;
            if (state.Deck.Count >= DeckSize) return; // mazzo non ancora distribuito
            CurrentHandNumber = Math.Max(1, TotalHands - state.Deck.Count / perHand);
        }

        // Le regole viaggiano con lo stato (impostate da TurnController e sincronizzate in rete).
        // Prima venivano cercate via reflection in "Assembly-CSharp", ma GameSceneInitializer vive in
        // Project51.Gameplay: la ricerca falliva sempre e si giocava con MatchRules.Default.
        private MatchRules GetRules()
        {
            return state.Rules ?? MatchRules.Default;
        }

        private int ApplyAccusiRulePoints(int basePoints)
        {
            return GetRules().AccusoPoints(basePoints);
        }

        public void StartSmazzata()
        {
            Rules51.DealInitialCards(state);

            CurrentHandNumber = 1;
            TotalHands = (DeckSize - state.Table.Count) / (CardsPerPlayerPerHand * state.NumPlayers);

            foreach (var p in state.Players)
            { p.AccusiPoints = 0; p.RoundAccusiPoints = 0; }

            OnInitialHandsDealt?.Invoke();

            ProcessDealerInitialAccuso();
        }

        private void ProcessDealerInitialAccuso()
        {
            var type = DealerAccusoFor(state.Table);
            if (type == AccusoType.Dealer15)
            {
                DeclareDealerAccuso(state.DealerIndex, AccusoType.Dealer15, 1);
            }
            else if (type == AccusoType.Dealer30)
            {
                DeclareDealerAccuso(state.DealerIndex, AccusoType.Dealer30, 2);
            }
        }

        /// <summary>Carte messe in tavolo a inizio smazzata.</summary>
        public const int InitialTableCards = 4;

        private const int DeckSize = 40;
        private const int CardsPerPlayerPerHand = 3;

        /// <summary>
        /// Accuso del mazziere sulle carte in tavolo a inizio smazzata: somma 15 o 30, con la matta che
        /// vale da 1 a 10 quanto serve (prima possibilita' trovata, come sempre). Null se non c'e'.
        /// </summary>
        public static AccusoType? DealerAccusoFor(IReadOnlyList<Card> table)
        {
            if (table == null || table.Count == 0) return null;

            int baseSum = table.Where(c => !c.IsMatta).Sum(c => c.Value);
            int mattaCount = table.Count(c => c.IsMatta);
            if (mattaCount == 0)
            {
                if (baseSum == 15) return AccusoType.Dealer15;
                if (baseSum == 30) return AccusoType.Dealer30;
                return null;
            }

            // Ricerca su tutte le assegnazioni 1..10 delle matte (in pratica 0 o 1 matta).
            AccusoType? found = null;
            var assign = new int[mattaCount];
            void Recurse(int idx)
            {
                if (found != null) return;
                if (idx == mattaCount)
                {
                    int sum = baseSum + assign.Sum();
                    if (sum == 15) found = AccusoType.Dealer15;
                    else if (sum == 30) found = AccusoType.Dealer30;
                    return;
                }
                for (int v = 1; v <= 10 && found == null; v++)
                {
                    assign[idx] = v;
                    Recurse(idx + 1);
                }
            }
            Recurse(0);
            return found;
        }

        /// <summary>
        /// Smazzata appena distribuita: tutti con 3 carte, 4 carte in tavolo (o gia' prese dal mazziere
        /// con il suo accuso) e nessuna carta giocata. Serve ai client in rete per mostrare roulette e
        /// distribuzione quando ricevono lo stato dal Master.
        /// </summary>
        public static bool IsFreshSmazzata(GameState state)
        {
            if (state == null || state.RoundEnded || state.Players == null || state.Players.Count != state.NumPlayers) return false;
            if (state.Players.Any(p => p.Hand.Count != 3)) return false;
            if (state.DealerIndex < 0 || state.DealerIndex >= state.NumPlayers) return false;

            int captured = state.Players.Sum(p => p.CapturedCards.Count);
            if (state.Table.Count + captured != InitialTableCards) return false;
            if (captured > 0 && (state.Table.Count > 0 || state.Players[state.DealerIndex].CapturedCards.Count != captured)) return false;
            return state.Deck.Count == 40 - 3 * state.NumPlayers - InitialTableCards;
        }

        /// <summary>
        /// In uno stato appena distribuito, l'accuso del mazziere gia' avvenuto: le carte che ha preso
        /// dal tavolo e il tipo (15 o 30). False se non c'e' stato.
        /// </summary>
        public static bool TryGetDealerAccusoAtStart(GameState state, out AccusoType type, out List<Card> sweptCards)
        {
            type = default(AccusoType);
            sweptCards = null;
            if (!IsFreshSmazzata(state) || state.Table.Count > 0) return false;
            var taken = state.Players[state.DealerIndex].CapturedCards;
            var found = DealerAccusoFor(taken);
            if (found == null) return false;
            type = found.Value;
            sweptCards = new List<Card>(taken);
            return true;
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

            // Cappotto immediato: scatta solo sulla presa che completa i 10 denari del giocatore (o
            // della coppia), non a ogni mossa successiva. La variante con bonus la gestisce
            // EndSmazzata, una volta sola.
            bool tookDenari = move.Type != MoveType.PlayOnly
                && (move.PlayedCard.Suit == Suit.Denari || (move.CapturedCards != null && move.CapturedCards.Any(c => c.Suit == Suit.Denari)));
            if (GetRules().CappottoEndsGameImmediately && tookDenari)
            {
                int entry = MatchScore.EntryOf(state, move.PlayerIndex);
                var members = MatchScore.MembersOf(state, entry);
                if (members.Sum(i => state.Players[i].CapturedCards.Count(c => c.Suit == Suit.Denari)) == 10)
                {
                    foreach (int member in members)
                        state.Players[member].TotalScore += MatchScore.CappottoScore;
                    state.RoundEnded = true;
                    return;
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

            var rules = GetRules();
            var entries = PunteggioManager.CalculateBreakdown(state);

            // Cappotto: un giocatore (o una coppia) con tutti e 10 i denari
            for (int e = 0; e < entries.Length; e++)
            {
                if (entries[e].DenariCount != 10) continue;
                var members = MatchScore.MembersOf(state, e);
                if (rules.CappottoEndsGameImmediately)
                {
                    foreach (int member in members)
                        state.Players[member].TotalScore += MatchScore.CappottoScore;
                    state.RoundEnded = true;
                    return;
                }
                if (rules.CappottoBonusPoints > 0)
                {
                    foreach (int member in members)
                        state.Players[member].TotalScore += rules.CappottoBonusPoints;
                }
            }

            // Punti della smazzata piu' accusi; a coppie ogni compagno riceve il punteggio della squadra.
            for (int i = 0; i < state.NumPlayers; i++)
            {
                var entry = entries[MatchScore.EntryOf(state, i)];
                state.Players[i].TotalScore += entry.Points + entry.AccusiPoints;
            }

            state.RoundEnded = true;
        }
    }
}
