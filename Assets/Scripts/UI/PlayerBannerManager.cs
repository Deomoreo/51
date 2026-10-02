using System.Linq;
using DG.Tweening;
using UnityEngine;
using Project51.Core;
using Project51.Unity;
using Project51.UIV2.Core;
using Project51.UIV2.Data;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Popola i 4 PlayerBanner (slot Locale/Sinistra/Alto/Destra) con dati reali da
    /// TurnController.GameState: nome, punteggio, turno attivo. Aggiornamento a polling
    /// (ogni 0,2 s), non a evento: TurnController non espone
    /// un OnTurnChanged, solo OnMoveExecuted/OnLocalPlayerMoveRequested.
    /// </summary>
    public class PlayerBannerManager : MonoBehaviour
    {
        [Tooltip("Indice 0=Locale, 1=Sinistra, 2=Alto, 3=Destra")]
        [SerializeField] private PlayerBanner[] banners = new PlayerBanner[4];

        [Tooltip("panel_fill_r24 di PanelsNeutral_v2, usato per il cerchio del conteggio prese. Assegnato da Tools/UIV2/Apply Table Layout V4.")]
        [SerializeField] private Sprite roundedFillSprite;

        [Tooltip("Ritratti per posto assoluto (uguali su tutti i client). Assegnati da Tools/UIV2/Apply Table Layout V4.")]
        [SerializeField] private Sprite[] seatAvatars = new Sprite[0];

        [Tooltip("UI51: stessi indici di banners. Dove c'e' un banner UI51 quello storico resta solo come ancora delle carte. Assegnati da Tools/UI51/Build Fase 5.")]
        [SerializeField] private Project51.UI51.PlayerBanner[] ui51Banners = new Project51.UI51.PlayerBanner[0];

        [Header("UI51 visore delle scope (S7). Assegnati da Tools/UI51/Build Fase 5.")]
        [SerializeField] private GameObject scopeViewer;
        [SerializeField] private TMPro.TMP_Text scopeTitle;
        [SerializeField] private TMPro.TMP_Text scopeCaptures;
        [SerializeField] private TMPro.TMP_Text scopeCount;
        [Tooltip("Card0/Face del ventaglio: modello, le altre carte sono sue copie.")]
        [SerializeField] private UnityEngine.UI.Image scopeFace;
        [Tooltip("Aree di tocco sulle scope, stessi indici di banners (vuoto dove il posto non ne ha).")]
        [SerializeField] private UnityEngine.UI.Button[] scopeHits = new UnityEngine.UI.Button[0];
        [Tooltip("Profilo rapido (UI51TableBuilder.BuildQuickProfile): si apre toccando un banner.")]
        [SerializeField] private QuickProfileCard profileCard;

        private readonly System.Collections.Generic.List<UnityEngine.UI.Image> scopeFaces = new System.Collections.Generic.List<UnityEngine.UI.Image>();
        private Vector2 scopeRest;
        private int scopeSlot = -1;
        private int scopeShown;
        private bool scopeAccused; // il visore mostra le carte accusate (in mano) invece delle scope
        private int mattaFlipped = -1; // carta del visore che mostra gia' la matta trasformata
        private bool accusoWas;

        private readonly Sprite[] ui51Avatars = new Sprite[4];
        private readonly int[] ui51Looks = { int.MinValue, int.MinValue, int.MinValue, int.MinValue }; // per slot; livello -1 da' chiavi negative

        private TurnController turnController;
        private GameSceneInitializer initializer;
        private CardViewManager cardViewManager;
        private Sprite matchCardBack;

        private void Start()
        {
            turnController = FindObjectOfType<TurnController>();
            cardViewManager = FindObjectOfType<CardViewManager>();
            InvokeRepeating(nameof(Refresh), 0.2f, 0.2f);
        }

        private void OnEnable() => CardViewManager.AccusedHandTapped += OpenAccused;
        private void OnDisable() => CardViewManager.AccusedHandTapped -= OpenAccused;

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
            // Visore delle scope: si chiude a fine smazzata e quando si apre la finestra dell'accuso (ACCUSA starebbe
            // sotto al velo), non al cambio di turno. Solo sul fronte: aprirlo durante la finestra resta possibile.
            bool accuso = turnController != null && turnController.IsAccusoWindowOpen;
            if (scopeSlot >= 0 && (state == null || state.RoundEnded || accuso && !accusoWas)) CloseScope();
            if (profileCard != null && profileCard.IsOpen && (state == null || state.RoundEnded || accuso && !accusoWas)) profileCard.Hide();
            accusoWas = accuso;
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
                var ui51 = UI51Banner(relative);
                if (ui51 != null)
                {
                    RefreshUI51(ui51, relative, p, player);
                    // Senza scope il tocco non fa nulla: niente click ne' vibrazione, e passa al profilo rapido sotto.
                    if (relative < scopeHits.Length && scopeHits[relative] != null)
                    {
                        bool hasScope = player.ScopaCards != null && player.ScopaCards.Count > 0;
                        scopeHits[relative].interactable = hasScope;
                        if (scopeHits[relative].targetGraphic != null) scopeHits[relative].targetGraphic.raycastTarget = hasScope;
                    }
                    if (relative == scopeSlot) ShowScope(relative, p, player);
                    continue;
                }
                banner.SetName(GetDisplayName(p));
                // ponytail: ritratto per posto, non per profilo; serve un AvatarId sincronizzato per sceglierlo.
                if (seatAvatars.Length > 0) banner.SetAvatar(seatAvatars[p % seatAvatars.Length]);
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
                foreach (var banner in ui51Banners)
                {
                    if (banner != null) banner.SetDealer(false);
                }
            }

            var state = turnController != null ? turnController.GameState : null;
            var ui51 = state != null
                ? UI51Banner(ResolveRelativeSlot(playerIndex, GameModeService.Current.LocalPlayerIndex, state.NumPlayers))
                : null;
            if (ui51 != null) ui51.SetDealer(active);
            else GetBannerForPlayer(playerIndex)?.SetDealerIndicator(active);
        }

        /// <summary>Tocco sulle scope del posto relativo slot (Button di UI51ScopeHit): apre il visore.</summary>
        public void OpenScope(int slot) => OpenViewer(slot, false);

        /// <summary>Tocco sulle carte accusate del giocatore assoluto p (CardViewManager): stesso visore, "Carte accusate da".</summary>
        private void OpenAccused(int p)
        {
            var state = turnController != null ? turnController.GameState : null;
            if (state != null) OpenViewer(ResolveRelativeSlot(p, GameModeService.Current.LocalPlayerIndex, state.NumPlayers), true);
        }

        private void OpenViewer(int slot, bool accused)
        {
            // Finestra dell'accuso aperta da meno di un giro: il fronte chiude solo un visore gia' aperto, non questo.
            accusoWas = turnController != null && turnController.IsAccusoWindowOpen;
            // Il vassoio della presa (ordine 550) coprirebbe il visore: si annulla, la carta si puo' scegliere di nuovo.
            var capture = FindObjectOfType<MoveSelectionUI>();
            if (capture != null && capture.IsVisible) capture.Cancel();
            if (profileCard != null) profileCard.Hide();
            scopeSlot = slot;
            scopeAccused = accused;
            Refresh();
        }

        /// <summary>
        /// Tocco sul banner del posto relativo slot: profilo rapido. Per me i dati del profilo cloud; per un altro giocatore
        /// quello che pubblica (AuthBootstrapper.LookProps); bot, ospiti e versioni vecchie: solo nome, ritratto e aspetto.
        /// </summary>
        public void OpenProfile(int slot)
        {
            var state = turnController != null ? turnController.GameState : null;
            if (state == null || profileCard == null || state.RoundEnded) return;
            int local = GameModeService.Current.LocalPlayerIndex, n = state.NumPlayers, p = -1;
            for (int i = 0; i < n; i++) if (ResolveRelativeSlot(i, local, n) == slot) p = i;
            if (p < 0) return;
            bool partner = state.TeamMode && p != local && MatchScore.EntryOf(state, p) == MatchScore.EntryOf(state, local);
            var view = new QuickProfileCard.View
            {
                Name = GetDisplayName(p),
                Avatar = SeatAvatar(p),
                Self = slot == 0,
                Team = slot == 0 ? "Tu" : partner ? PartnerLabel : "Avversario",
                Top = ProfileTop(slot, n),
            };
            if (!LookOf(slot, p, partner, out view.Frame, out view.Style, out view.Level)) return;
            var auth = Project51.Auth.AuthBootstrapper.Instance;
            if (slot == 0)
            {
                if (auth != null && auth.HasRealProfile)
                {
                    view.Stats = true;
                    view.Games = auth.Profile.TotalGames;
                    view.Wins = auth.Profile.Wins;
                    view.Scope = auth.Profile.TotalScope;
                    view.Xp = auth.Profile.XP;
                }
            }
            else
            {
                var owner = GameModeService.Current.IsHumanPlayer(p) ? GameSocialV2.PlayerAt(p) : null;
                if (owner != null)
                {
                    view.Stats = ProfileCosmetics.ReadStats(owner.CustomProperties, out view.Games, out view.Wins, out view.Scope, out view.PlayFabId);
                    // Un ospite si puo' silenziare e segnalare (2.55), non aggiungere ne' bloccare: il suo account dura una sessione.
                    if (view.PlayFabId == null && (view.PlayFabId = ProfileCosmetics.GuestId(owner.CustomProperties)) != null) view.Guest = true;
                    // Aggiungi amico / Segnala partono dal mio account: da ospite niente pulsanti.
                    if (auth == null || auth.PlayFabAuth == null || !auth.PlayFabAuth.HasRealLogin) view.PlayFabId = null;
                }
            }

            accusoWas = turnController.IsAccusoWindowOpen; // come il visore: si chiude solo sul prossimo fronte dell'accuso
            CloseScope();
            var capture = FindObjectOfType<MoveSelectionUI>();
            if (capture != null && capture.IsVisible) capture.Cancel();
            profileCard.Show(view);
        }

        /// <summary>Bordo alto della scheda nel mockup: io 300, in alto 140 (1v1) o 150 (a 4), ai lati 220.</summary>
        public static float ProfileTop(int slot, int numPlayers) => slot == 0 ? 300f : slot == 2 ? numPlayers == 2 ? 140f : 150f : 220f;

        /// <summary>Tocco sul velo o sulla X del profilo rapido.</summary>
        public void CloseProfile()
        {
            if (profileCard != null) profileCard.Hide();
        }

        /// <summary>Tocco sul velo del visore, o chiusura automatica da Refresh.</summary>
        public void CloseScope()
        {
            scopeSlot = -1;
            if (scopeViewer != null) scopeViewer.SetActive(false);
        }

        /// <summary>Titolo e chip del visore delle scope (mockup Partita).</summary>
        public static void ScopeTexts(bool mine, string name, int captures, int scope, out string title, out string chipA, out string chipB)
        {
            title = mine ? "Le tue scope" : "Le scope di " + name;
            chipA = captures == 1 ? "1 carta presa" : captures + " carte prese";
            chipB = scope == 1 ? "1 scopa" : scope + " scope";
        }

        /// <summary>Titolo e chip del visore delle carte accusate (mockup Partita: "Accuso", "+3 punti").</summary>
        public static void AccusedTexts(string name, int points, out string title, out string chipA, out string chipB)
        {
            title = "Carte accusate da " + name;
            chipA = "Accuso";
            chipB = "+" + points + (points == 1 ? " punto" : " punti");
        }

        private const float AccusedStep = 96f + 8f; // carta del visore larga 96, 8 di stacco

        /// <summary>Ventaglio del mockup fino a 6 carte (passo 40, 9 gradi, 6 in giu'); oltre, stessa sagoma delle 6.</summary>
        public static void FanPose(int count, out float step, out float degrees, out float drop)
        {
            float k = count > 6 ? 5f / (count - 1) : 1f;
            step = 40f * k;
            degrees = 9f * k;
            drop = 6f * k;
        }

        /// <summary>
        /// Visore aperto: testi a ogni giro, ventaglio solo all'apertura o quando cambia il numero di scope.
        /// Senza scope (smazzata nuova) si chiude.
        /// </summary>
        private void ShowScope(int slot, int p, PlayerState player)
        {
            var cards = scopeAccused ? Sprites(player.AccusiPoints > 0 ? player.Hand : null) : GetScopeSprites(player);
            int n = cards.Count;
            if (scopeViewer == null || scopeFace == null || n == 0)
            {
                CloseScope();
                return;
            }
            string title, chipA, chipB;
            if (scopeAccused) AccusedTexts(GetDisplayName(p), player.AccusiPoints, out title, out chipA, out chipB);
            else ScopeTexts(slot == 0, GetDisplayName(p), turnController.GetDisplayedCapturedCount(p), n, out title, out chipA, out chipB);
            scopeTitle.text = title;
            scopeCaptures.text = chipA;
            scopeCount.text = chipB;

            if (scopeFaces.Count == 0)
            {
                scopeFaces.Add(scopeFace);
                scopeRest = ((RectTransform)scopeFace.transform.parent).anchoredPosition;
            }
            while (scopeFaces.Count < n)
            {
                var model = scopeFace.transform.parent;
                scopeFaces.Add(Instantiate(model, model.parent).Find("Face").GetComponent<UnityEngine.UI.Image>());
            }
            bool opening = !scopeViewer.activeSelf;
            scopeViewer.SetActive(true);
            int matta = -1;
            Sprite mattaSprite = scopeAccused ? MattaTarget(player.Hand, out matta) : null;
            if (opening || n != scopeShown) mattaFlipped = -1;
            for (int i = 0; i < scopeFaces.Count; i++)
            {
                if (i < n) scopeFaces[i].sprite = i == mattaFlipped ? mattaSprite : cards[i];
                var card = scopeFaces[i].transform.parent.gameObject;
                if (card.activeSelf != i < n) card.SetActive(i < n);
                var look = card.transform.Find("Matta");
                if (look != null && look.gameObject.activeSelf != (i == mattaFlipped)) look.gameObject.SetActive(i == mattaFlipped);
            }
            if (!opening && n == scopeShown) return;

            scopeShown = n;
            float step, degrees, drop;
            FanPose(n, out step, out degrees, out drop);
            // Accusate: in fila senza coprirsi, servono a leggere le carte (utente 01/10). Al massimo 3: 3 x 104 sta nei 330 del ventaglio.
            if (scopeAccused) { step = AccusedStep; degrees = 0f; drop = 0f; }
            var fan = new System.Collections.Generic.List<RectTransform>(n);
            for (int i = 0; i < n; i++)
            {
                var rt = (RectTransform)scopeFaces[i].transform.parent;
                Project51.UI51.UIAnim.Stop(rt); // il kill riscrive la posa finale del ventaglio di prima
                rt.anchoredPosition = scopeRest + new Vector2(step * (i - (n - 1) * 0.5f), 0f);
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;
                rt.SetAsLastSibling(); // ordine naturale: la matta trasformata era passata davanti
                fan.Add(rt);
            }
            if (opening) Project51.UI51.UIAnim.FadeIn((RectTransform)scopeViewer.transform, 0.2f);
            Project51.UI51.UIAnim.Fan(fan, 0.06f, degrees, drop);
            if (mattaSprite != null && matta < n) FlipMatta(matta, mattaSprite, 0.45f + 0.06f * (n - 1));
        }

        /// <summary>Faccia che la matta prende per l'accuso (stessa regola del tavolo: solo con le 3 carte), null se resta un 7.</summary>
        private Sprite MattaTarget(System.Collections.Generic.List<Card> hand, out int index)
        {
            index = hand != null ? hand.FindIndex(c => c.IsMatta) : -1;
            int rank = AccusiChecker.MattaValueForAccuso(hand);
            return index >= 0 && rank > 0 && cardViewManager != null ? cardViewManager.GetSpriteForCard(new Card(hand[index].Suit, rank)) : null;
        }

        /// <summary>
        /// Visore delle accusate: finito il ventaglio la matta (7 di coppe) si gira e diventa la carta che vale, poi bordo d'oro
        /// che pulsa e cartellino "MATTA". Chiudere il visore uccide il giro: la faccia si raddrizza alla prossima apertura.
        /// </summary>
        private void FlipMatta(int i, Sprite target, float delay)
        {
            var face = (RectTransform)scopeFaces[i].transform;
            Project51.UI51.UIAnim.Stop(face);
            face.localRotation = Quaternion.identity;
            var half = new Project51.UI51.UIKeyframes(0.16f, Project51.UI51.UIEase.EaseIn).Track(Project51.UI51.AnimProp.RotationY, 0f, 0f, 1f, 90f);
            half.Play(face, delay).OnComplete(() =>
            {
                mattaFlipped = i;
                scopeFaces[i].sprite = target;
                face.localRotation = Quaternion.Euler(0f, 90f, 0f);
                Project51.UI51.UIAnim.Flip(face, 0f);
                face.parent.SetAsLastSibling(); // davanti alle altre: bordo e cartellino interi
                var look = face.parent.Find("Matta");
                if (look == null) return;
                look.gameObject.SetActive(true);
                var tag = look.Find("Tag") as RectTransform;
                if (tag != null) Project51.UI51.UIAnim.Pop(tag, 0.4f, 1.12f, 0.3f, 0.1f);
                var pulse = look.Find("Pulse");
                if (pulse != null) Project51.UI51.UIAnim.Pulse(pulse.GetComponent<Project51.UI51.UI51Shape>(), 8f);
            });
        }

        /// <summary>Ritratto del giocatore p, lo stesso del suo banner (ruota del sorteggio).</summary>
        internal Sprite SeatAvatar(int p) => seatAvatars.Length > 0 ? seatAvatars[p % seatAvatars.Length] : null;

        /// <summary>Banner UI51 del posto relativo (0 io, 1 sinistra, 2 alto, 3 destra); null se il posto non ne ha.</summary>
        public Project51.UI51.PlayerBanner UI51Banner(int slot)
        {
            return slot >= 0 && slot < ui51Banners.Length ? ui51Banners[slot] : null;
        }

        /// <summary>Banner UI51 del giocatore assoluto p (null se non pronto).</summary>
        public Project51.UI51.PlayerBanner UI51BannerForPlayer(int p)
        {
            var state = turnController != null ? turnController.GameState : null;
            return state != null ? UI51Banner(ResolveRelativeSlot(p, GameModeService.Current.LocalPlayerIndex, state.NumPlayers)) : null;
        }

        /// <summary>
        /// Stessi dati del banner storico, senza punteggio (in UI51 sta nella pillola in alto) e
        /// senza mazzetto prese (in UI51 e' il numero dentro al banner).
        /// </summary>
        private void RefreshUI51(Project51.UI51.PlayerBanner banner, int slot, int p, PlayerState player)
        {
            banner.SetName(GetDisplayName(p));
            // AvatarFrame.SetAvatar rifa' il ritaglio a ogni chiamata: solo quando il ritratto cambia.
            var avatar = seatAvatars.Length > 0 ? seatAvatars[p % seatAvatars.Length] : null;
            if (avatar != null && avatar != ui51Avatars[slot] && banner.avatar != null)
            {
                ui51Avatars[slot] = avatar;
                banner.avatar.SetAvatar(avatar);
            }
            int local = GameModeService.Current.LocalPlayerIndex;
            // Mockup MomentiPartita: anello blu quando tocca a un altro, oro al proprio turno.
            banner.SetTurn(p == turnController.CurrentPlayerIndex, p != local);
            if (initializer == null) initializer = FindObjectOfType<GameSceneInitializer>();
            banner.SetOffline(initializer != null && initializer.IsDisconnected(p));
            banner.SetCaptures(turnController.GetDisplayedCapturedCount(p));
            banner.SetCardBack(GetMatchCardBack());
            banner.SetScope(GetScopeSprites(player));
            var state = turnController.GameState;
            ApplyLook(banner, slot, p, state.TeamMode && p != local && MatchScore.EntryOf(state, p) == MatchScore.EntryOf(state, local));
        }

        /// <summary>
        /// Cornice, banner e livello. Il proprio: dal profilo (ospite, offline o profilo non arrivato: Oro, Notte;
        /// livello come in Home). Avversario umano: quello che pubblica lui (AuthBootstrapper.PublishLook).
        /// Bot, ospite o client che non pubblica: come lo costruisce UI51PrefabBuilder (cornice Blu, Notte, livello nascosto).
        /// A coppie il compagno (Partita4): "Compagno" al posto del livello e, se non pubblica un aspetto, cornice Oro.
        /// </summary>
        private void ApplyLook(Project51.UI51.PlayerBanner banner, int slot, int p, bool partner)
        {
            if (!LookOf(slot, p, partner, out int frame, out int style, out int level)) return;

            int look = frame + 10 * style + 100 * level + (partner ? 1000000 : 0);
            if (look == ui51Looks[slot]) return;
            ui51Looks[slot] = look;
            banner.SetStyle(ProfileCosmetics.Banner(style));
            ProfileCosmetics.ApplyFrame(banner.avatar, frame, 2f, 2f);
            if (partner) banner.SetInfo(PartnerLabel);
            else banner.SetLevel(level);
            // "Compagno" col chip non sta nei 120 del banner in alto (nel mockup il chip sborda di 22): il compagno lo ha
            // piu' largo di PartnerWiden per lato. Cresce a destra e il suo contenitore scorre a sinistra: resta centrato,
            // e gettone del mazziere e scope (fratelli nel contenitore) restano alla stessa distanza dal banner.
            var pill = (RectTransform)banner.transform;
            var holder = (RectTransform)pill.parent;
            float widen = partner ? PartnerWiden : 0f;
            pill.offsetMax = new Vector2(2f * widen, pill.offsetMax.y);
            holder.anchoredPosition = new Vector2(-widen * holder.localScale.x, holder.anchoredPosition.y);
        }

        /// <summary>Cornice, banner e livello del posto (vedi ApplyLook); false se il roster non e' ancora fissato.</summary>
        private bool LookOf(int slot, int p, bool partner, out int frame, out int style, out int level)
        {
            if (slot == 0)
            {
                var auth = Project51.Auth.AuthBootstrapper.Instance;
                bool real = auth != null && auth.HasRealProfile;
                frame = ProfileCosmetics.FrameIndex(real ? auth.Profile.FrameId : null);
                style = ProfileCosmetics.BannerIndex(real ? auth.Profile.BannerId : null);
                // Come Home: registrato col profilo cloud non arrivato = livello dai progressi locali.
                var local = Project51.Auth.PlayerProgressLocal.Instance;
                bool registered = auth != null && auth.PlayFabAuth != null && auth.PlayFabAuth.HasRealLogin;
                level = real ? PlayerXp.LevelOf(auth.Profile.XP) : registered && local != null ? local.Level : 1;
            }
            else
            {
                bool human = GameModeService.Current.IsHumanPlayer(p);
                var owner = human ? GameSocialV2.PlayerAt(p) : null;
                frame = partner ? 1 : 3; style = 0; level = -1;
                if (human && owner == null) return false; // roster non ancora fissato o rientro in corso: resta l'aspetto di prima
                if (owner == null || !ProfileCosmetics.ReadLook(owner.CustomProperties, out frame, out style, out level))
                {
                    frame = partner ? 1 : 3; style = 0; level = -1;
                }
            }

            return true;
        }

        /// <summary>Riga sotto al nome del compagno a coppie, al posto del livello (mockup Partita4).</summary>
        public const string PartnerLabel = "Compagno";
        public const float PartnerWiden = 8f;

        /// <summary>
        /// Stessa convenzione a indice relativo gia' usata in
        /// CapturedPileManager.MapToViewIndex e CardViewManager.RenderAIHandsDynamic
        /// (duplicata li' come qui: non
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
        private System.Collections.Generic.List<Sprite> GetScopeSprites(PlayerState player) => Sprites(player.ScopaCards);

        private System.Collections.Generic.List<Sprite> Sprites(System.Collections.Generic.IEnumerable<Card> cards)
        {
            if (cards == null || cardViewManager == null)
            {
                return new System.Collections.Generic.List<Sprite>();
            }

            return cards
                .Select(c => cardViewManager.GetSpriteForCard(c))
                .Where(s => s != null)
                .ToList();
        }

        internal Sprite MatchCardBack() => GetMatchCardBack();

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
                return GameSocialV2.PlayerName(playerIndex); // nickname vero dell'avversario online
            }
            return $"Bot {playerIndex + 1}";
        }
    }
}
