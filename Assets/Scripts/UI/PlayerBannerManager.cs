using System.Linq;
using UnityEngine;
using Project51.Core;
using Project51.Unity;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Popola i 4 PlayerBanner (slot Locale/Sinistra/Alto/Destra) con dati reali da
    /// TurnController.GameState: nome, punteggio, turno attivo. Aggiornamento a polling
    /// (stessa cadenza/pattern di TurnIndicator), non a evento: TurnController non espone
    /// un OnTurnChanged, solo OnMoveExecuted/OnLocalPlayerMoveRequested.
    /// </summary>
    public class PlayerBannerManager : MonoBehaviour
    {
        [Tooltip("Indice 0=Locale, 1=Sinistra, 2=Alto, 3=Destra")]
        [SerializeField] private PlayerBanner[] banners = new PlayerBanner[4];

        [Tooltip("panel_fill_r24 di PanelsNeutral_v2, usato per il cerchio del conteggio prese. Assegnato da Tools/UIV2/Apply Table Layout V4.")]
        [SerializeField] private Sprite roundedFillSprite;

        private TurnController turnController;
        private CardViewManager cardViewManager;
        private Sprite matchCardBack;

        private void Start()
        {
            turnController = FindObjectOfType<TurnController>();
            cardViewManager = FindObjectOfType<CardViewManager>();
            InvokeRepeating(nameof(Refresh), 0.2f, 0.2f);
        }

        private void Refresh()
        {
            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
            }
            if (cardViewManager == null)
            {
                cardViewManager = FindObjectOfType<CardViewManager>();
            }

            var state = turnController != null ? turnController.GameState : null;
            if (state == null) return;

            int numPlayers = state.NumPlayers;
            int localIndex = GameModeService.Current.LocalPlayerIndex;
            var usedSlots = new bool[banners.Length];

            for (int p = 0; p < numPlayers; p++)
            {
                int relative = ResolveRelativeSlot(p, localIndex, numPlayers);
                if (relative < 0 || relative >= banners.Length || banners[relative] == null) continue;

                usedSlots[relative] = true;

                var player = state.Players[p];
                var banner = banners[relative];
                banner.gameObject.SetActive(true);
                banner.SetName(GetDisplayName(p));
                // Punteggio di partita (a coppie quello della squadra), non solo della smazzata in corso.
                banner.SetScore(MatchScore.Totals(state)[MatchScore.EntryOf(state, p)]);
                banner.SetTurnActive(p == turnController.CurrentPlayerIndex);
                banner.SetScopeCards(GetScopeSprites(player));
                banner.SetCapturedPile(turnController.GetDisplayedCapturedCount(p), GetMatchCardBack(), CardViewManager.GetCapturedPileDesignOffset(relative), roundedFillSprite);
            }

            for (int slot = 0; slot < banners.Length; slot++)
            {
                if (banners[slot] != null && !usedSlots[slot])
                {
                    banners[slot].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Banner del giocatore assoluto playerIndex (o null se non mappato/non pronto). Usato
        /// da TurnController per l'animazione di dichiarazione del dealer a inizio smazzata,
        /// cosi' la conversione a indice relativo resta in un solo posto invece di duplicarla.
        /// </summary>
        public PlayerBanner GetBannerForPlayer(int playerIndex)
        {
            var state = turnController != null ? turnController.GameState : null;
            if (state == null) return null;

            int relative = ResolveRelativeSlot(playerIndex, GameModeService.Current.LocalPlayerIndex, state.NumPlayers);
            if (relative < 0 || relative >= banners.Length) return null;
            return banners[relative];
        }

        /// <summary>
        /// Mostra la chip "MAZZIERE" sul banner del giocatore assoluto playerIndex e la spegne su
        /// TUTTI gli altri banner - il dealer resta visualizzato per tutta la smazzata (richiesta
        /// esplicita dell'utente), quindi ad ogni chiamata bisogna anche ripulire l'eventuale
        /// dealer precedente (se il ruolo passa a un altro giocatore alla smazzata successiva),
        /// non solo accendere quello nuovo. Chiamato via reflection da TurnController
        /// (Project51.Gameplay e' un assembly separato che non puo' referenziare
        /// Project51.Unity.UI, che vive nell'assembly di default - stesso motivo/stesso pattern
        /// gia' usato per NetworkGameController): un solo metodo invocabile, cosi' non serve far
        /// viaggiare l'oggetto PlayerBanner attraverso la reflection.
        /// </summary>
        public void SetDealerIndicatorForPlayer(int playerIndex, bool active)
        {
            if (active)
            {
                foreach (var banner in banners)
                {
                    banner?.SetDealerIndicator(false);
                }
            }
            GetBannerForPlayer(playerIndex)?.SetDealerIndicator(active);
        }

        /// <summary>
        /// Stessa convenzione a indice relativo gia' usata in
        /// CapturedPileManager.MapToViewIndex, CardViewManager.RenderAIHandsDynamic e
        /// AccusoPanelController.GetAccusoCardSizeForPlayer (duplicata li' come qui: non
        /// esiste un helper condiviso nel progetto per questo calcolo).
        /// </summary>
        private static int ResolveRelativeSlot(int playerIndex, int localIndex, int numPlayers)
        {
            if (numPlayers <= 0) return 0;
            int relative = ((playerIndex - localIndex) % numPlayers + numPlayers) % numPlayers;
            if (numPlayers == 2 && relative == 1) relative = 2; // unico avversario, reso "in alto"
            return relative;
        }

        /// <summary>
        /// Sprite reali delle carte scope (PlayerState.ScopaCards) via CardViewManager,
        /// che gia' possiede la mappatura Card -> Sprite (Assets/UI_SPEC_Tavolo.md, sezione 4:
        /// "carte vere che spuntano da dietro il banner", non gettoni/icone generiche).
        /// </summary>
        private System.Collections.Generic.List<Sprite> GetScopeSprites(PlayerState player)
        {
            var scopaCards = player.ScopaCards;
            if (scopaCards == null || scopaCards.Count == 0 || cardViewManager == null)
            {
                return new System.Collections.Generic.List<Sprite>();
            }

            return scopaCards
                .Select(c => cardViewManager.GetSpriteForCard(c))
                .Where(s => s != null)
                .ToList();
        }

        private Sprite GetMatchCardBack()
        {
            if (matchCardBack == null)
            {
                var deck = CardDecks.LoadForMatch();
                matchCardBack = deck != null ? deck.Back : Resources.Load<Sprite>("Cards/CardBack");
            }
            return matchCardBack;
        }

        private static string GetDisplayName(int playerIndex)
        {
            var provider = GameModeService.Current;
            if (provider.IsHumanPlayer(playerIndex))
            {
                if (provider.IsLocalPlayer(playerIndex))
                {
                    var auth = Project51.Auth.AuthBootstrapper.Instance;
                    var name = auth != null && auth.PlayFabAuth != null ? auth.PlayFabAuth.GetBestDisplayName() : null;
                    return string.IsNullOrEmpty(name) ? "Tu" : name;
                }
                return $"Giocatore {playerIndex + 1}";
            }
            return $"Bot {playerIndex + 1}";
        }
    }
}
