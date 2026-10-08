using System;
using System.Linq;
using DG.Tweening;
using Project51.Auth;
using Project51.UIV2.Animations;
using Project51.Core;
using Project51.UIV2.Components;
using Project51.UIV2.Data;
using Project51.Unity;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Risultati al tavolo: fine smazzata (riquadro, mockup 13) e fine partita (schermo intero con
    /// coriandoli, mockup 12). Totali e fine partita arrivano dallo GameState (MatchScore), quindi
    /// sono identici su ogni client; solo l'host fa proseguire.
    /// </summary>
    public sealed class MatchResultsV2 : MonoBehaviour
    {
        [Header("Fine smazzata")]
        public GameObject RoundPanel;
        public Button RoundContinue;
        public TMP_Text RoundContinueLabel;

        [Header("Fine partita")]
        public GameObject MatchPanel;
        public Button Rematch;
        public TMP_Text RematchLabel;
        public Button MatchMenu;
        public RectTransform ConfettiRoot;

        [Header("UI51 Fase 7")]
        [Tooltip("Grafica dei mockup FineSmazzata/FinePartita (UI51ResultsBuilder).")]
        public Project51.Unity.UI.UI51ResultsView View;

        private Action next, menu;
        private bool finished, clicked, wonMatch;
        private GameState shownState, celebratedState;
        private readonly RoundAdvanceCountdown autoAdvance = new RoundAdvanceCountdown();
        private InGameSettingsV2 settings;
        private TurnController turn;
        private bool applicationPaused;
        // B7 (R2): un resync rimanda la stessa fine smazzata come oggetto nuovo, e a volte diversa (era divergente): si mostra di
        // nuovo solo se cambia (shownKey), ma bonus e premio si contano una volta per smazzata e per partita, mai per stato.
        private GameState recordedState;
        private string shownKey;
        private int countedRound;
        private bool recorded;
        private int matchScope, matchAccusi, matchSettebelli, xpFrom = -1, xpTo;
        private bool guestXp;
        // Vittoria per abbandono (scelta dell'utente 01/10): senza piu' avversari umani al tavolo vince chi resta. Statici per RecordAbandon.
        private static GameState forfeitState;
        private static string forfeitOpponent;
        private static int forfeitActor; // il server ricava da qui l'avversario vero (record della partita, Server/CloudScript/51.js)
        private bool Forfeit => forfeitState != null && forfeitState == shownState;
        // Un avversario e' uscito mentre questa partita era in corso (non a fine partita: la rivincita col bot non vale); controllo in attesa del tavolo fermo.
        private bool opponentLeftMidMatch, forfeitCheck;
        public const int MaxForfeitRewards = 3; // come "abbandoni" in premioPartita (Server/CloudScript/51.js)
        // #141: biglietto della partita in corso (id del server, inizioPartita) e id della partita sul telefono (allenamento: un
        // biglietto per partita; senza biglietto: l'id del risultato). Statici: RecordAbandon e' statico. Le partite finite le ricorda
        // RewardsService (mai lo stesso biglietto per due partite, anche dopo un riavvio).
        private static string matchTicket, matchLocal;
        private int verifyTries;
        private bool ticketRequest;
        private float nextTicketTry;

        private void Awake()
        {
            matchTicket = null; // scena nuova: partita nuova o rientro, il server restituisce lo stesso biglietto se e' la stessa partita
            forfeitState = null;
            forfeitOpponent = null;
            forfeitActor = 0;
            settings = FindObjectOfType<InGameSettingsV2>(true);
            turn = FindObjectOfType<TurnController>();
            if (turn != null) turn.InactiveTooLong += LeftForInactivity;
            GamePresentation.RoundResults += Show;
            GamePresentation.HideResults += Hide;
            RoundContinue.onClick.AddListener(Next);
            Rematch.onClick.AddListener(Next);
            // Il fine smazzata del mockup 13 non ha un'uscita: si abbandona dalle Impostazioni in partita.
            MatchMenu.onClick.AddListener(Exit);
            RoundPanel.SetActive(false);
            MatchPanel.SetActive(false);
        }

        /// <summary>Nome di un concorrente: il giocatore, oppure a coppie i due compagni.</summary>
        public static string EntryName(GameState state, int entry) =>
            string.Join(" e ", MatchScore.MembersOf(state, entry).Select(GameSocialV2.PlayerName));

        public void Show(GameState state, Action nextRound, Action mainMenu)
        {
            if (Forfeit) return; // partita gia' chiusa per abbandono: il tavolo e' fermo
            // Avversari usciti durante la smazzata: alla sua fine vince chi resta (OpponentLeft lo fa subito dopo la prima).
            bool becomesForfeit = forfeitState != state && CanForfeit(state);
            if (becomesForfeit) { forfeitState = state; turn.Halt(); }
            string key = GameStateSerializer.Serialize(state);
            if (!becomesForfeit && key == shownKey) return; // stessa fine smazzata rimandata da un resync: gia' mostrata (e contata)
            shownKey = key;
            bool forfeit = forfeitState == state;
            autoAdvance.Cancel();
            captionSeconds = int.MinValue;
            shownState = state;
            next = nextRound;
            menu = mainMenu;
            clicked = false;
            int target = (GameSceneInitializer.ActiveConfig ?? new MatchConfig()).TargetScore;
            var totals = MatchScore.Totals(state);
            finished = forfeit || MatchScore.IsFinished(state, target);
            int localEntry = MatchScore.EntryOf(state, GameModeService.Current.LocalPlayerIndex);
            var order = Enumerable.Range(0, totals.Length).OrderByDescending(e => totals[e]).ToArray();
            CountLocalBonuses(state);
            int winner = forfeit ? localEntry : order[0];
            wonMatch = finished && winner == localEntry;
            if (finished && !recorded) RecordMatch(wonMatch, winner);

            if (finished)
            {
                View.BindMatch(state, localEntry, winner, matchScope, matchAccusi, matchSettebelli);
                if (forfeit) View.SetModeCaption(state.TeamMode || MatchScore.EntryCount(state) > 2 ? "GLI AVVERSARI HANNO ABBANDONATO" : "L'AVVERSARIO HA ABBANDONATO");
            }
            else View.BindRound(state, target, localEntry);

            RefreshContinue();
            (finished ? RoundPanel : MatchPanel).SetActive(false);
            Reveal(localEntry, winner);
        }

        /// <summary>
        /// Un giocatore e' uscito davvero (NetworkGameController.OnPlayerLeftRoom, non una disconnessione): senza piu' avversari
        /// umani la partita e' vinta da chi resta. True se la partita e' chiusa per abbandono (niente avviso del bot).
        /// </summary>
        public bool OpponentLeft(Photon.Realtime.Player player)
        {
            if (turn == null) turn = FindObjectOfType<TurnController>();
            var state = turn != null ? turn.GameState : null;
            var seats = FindObjectOfType<GameSceneInitializer>();
            int local = GameModeService.Current.LocalPlayerIndex;
            if (state == null || seats == null || player == null || local < 0) return Forfeit;
            int seat = seats.GetPlayerIndexForActor(player.ActorNumber);
            if (seat < 0 || MatchScore.EntryOf(state, seat) == MatchScore.EntryOf(state, local) ||
                MatchScore.IsFinished(state, (GameSceneInitializer.ActiveConfig ?? new MatchConfig()).TargetScore)) return Forfeit;
            opponentLeftMidMatch = true;
            ProfileCosmetics.ReadStats(player.CustomProperties, out _, out _, out _, out string id);
            forfeitOpponent = id ?? ProfileCosmetics.GuestId(player.CustomProperties) ?? "";
            forfeitActor = player.ActorNumber;
            forfeitCheck = true;
            CheckForfeit();
            return Forfeit;
        }

        /// <summary>
        /// Rientro (caduta o riavvio): chi e' uscito o e' stato tolto mentre eravamo scollegati non ci ha mandato OnPlayerLeftRoom,
        /// ma per la partita e' un abbandono come gli altri. Il suo id viene da quelli ricordati a inizio partita (RememberOpponentIds),
        /// anche dopo un riavvio; "sconosciuto" solo se mancano (scelta dell'utente 02/10).
        /// </summary>
        public void CheckOpponentsAfterRejoin()
        {
            if (turn == null) turn = FindObjectOfType<TurnController>();
            var state = turn != null ? turn.GameState : null;
            var seats = FindObjectOfType<GameSceneInitializer>();
            int local = GameModeService.Current.LocalPlayerIndex;
            if (state == null || seats == null || local < 0 || !PhotonNetwork.InRoom || Forfeit ||
                MatchScore.IsFinished(state, (GameSceneInitializer.ActiveConfig ?? new MatchConfig()).TargetScore)) return;
            int mine = MatchScore.EntryOf(state, local);
            foreach (var (seat, actor) in seats.Roster())
            {
                if (seat < 0 || seat == local || MatchScore.EntryOf(state, seat) == mine) continue;
                if (PhotonNetwork.CurrentRoom.GetPlayer(actor) != null && !seats.IsRemovedForInactivity(seat)) continue;
                opponentLeftMidMatch = true;
                if (forfeitOpponent == null) { forfeitOpponent = KnownOpponentId(actor) ?? ""; forfeitActor = actor; }
            }
            if (!opponentLeftMidMatch) return;
            forfeitCheck = true;
            CheckForfeit();
        }

        private const string KnownIdsKey = "Partita.Avversari"; // "stanza|attore:id,attore:id"
        private float nextIdsCheck;

        /// <summary>Id di chi siede al tavolo, ricordati finche' sono presenti: servono se escono mentre siamo scollegati.</summary>
        private void RememberOpponentIds()
        {
            if (Time.unscaledTime < nextIdsCheck || !PhotonNetwork.InRoom || !GameModeService.Current.IsMultiplayer) return;
            nextIdsCheck = Time.unscaledTime + 2f;
            var seats = FindObjectOfType<GameSceneInitializer>();
            if (seats == null) return;
            string room = PhotonNetwork.CurrentRoom.Name, saved = PlayerPrefs.GetString(KnownIdsKey, string.Empty);
            var ids = new System.Collections.Generic.Dictionary<int, string>();
            var parts = saved.Split('|');
            if (parts.Length == 2 && parts[0] == room)
                foreach (var entry in parts[1].Split(','))
                    if (entry.IndexOf(':') > 0 && int.TryParse(entry.Substring(0, entry.IndexOf(':')), out int a)) ids[a] = entry.Substring(entry.IndexOf(':') + 1);
            foreach (var (_, actor) in seats.Roster())
            {
                var player = PhotonNetwork.CurrentRoom.GetPlayer(actor);
                if (player == null || player.IsLocal) continue; // chi e' uscito tiene l'id ricordato
                ProfileCosmetics.ReadStats(player.CustomProperties, out _, out _, out _, out string id);
                id = id ?? ProfileCosmetics.GuestId(player.CustomProperties);
                if (!string.IsNullOrEmpty(id)) ids[actor] = id.Replace(",", "").Replace("|", "").Replace(":", ""); // anche al posto di un id vecchio (codice riusato)
            }
            string text = room + "|" + string.Join(",", ids.Select(e => e.Key + ":" + e.Value));
            if (text != saved) { PlayerPrefs.SetString(KnownIdsKey, text); PlayerPrefs.Save(); } // serve proprio dopo un riavvio
        }

        private static string KnownOpponentId(int actor)
        {
            var parts = PlayerPrefs.GetString(KnownIdsKey, string.Empty).Split('|');
            if (parts.Length != 2 || !PhotonNetwork.InRoom || parts[0] != PhotonNetwork.CurrentRoom.Name) return null;
            foreach (var entry in parts[1].Split(','))
                if (entry.StartsWith(actor + ":", StringComparison.Ordinal)) return entry.Substring(entry.IndexOf(':') + 1);
            return null;
        }

        /// <summary>Si decide a tavolo fermo: la mossa ancora in coda puo' essere proprio quella che chiude la partita.</summary>
        private void CheckForfeit()
        {
            if (!forfeitCheck || turn == null || turn.IsBusy) return;
            forfeitCheck = false;
            if (!Forfeit && CanForfeit(turn.GameState)) Show(turn.GameState, null, AppFlowManager.GoToMainMenu);
        }

        /// <summary>
        /// Come il server (premioPartita): XP di una vittoria per abbandono al massimo MaxForfeitRewards volte al giorno e una per
        /// avversario ("" = sconosciuto, mai doppio). saved = "aaaa-mm-gg|id,id" (PlayerPrefs). Oltre la vittoria conta, senza XP.
        /// </summary>
        public static bool ForfeitRewarded(string opponent, string today, ref string saved)
        {
            var parts = (saved ?? string.Empty).Split('|');
            var ids = parts.Length == 2 && parts[0] == today && parts[1].Length > 0 ? parts[1].Split(',').ToList() : new System.Collections.Generic.List<string>();
            string who = string.IsNullOrEmpty(opponent) ? "?" : opponent.Replace(",", "").Replace("|", "");
            if (ids.Count >= MaxForfeitRewards || who != "?" && ids.Contains(who)) return false;
            ids.Add(who);
            saved = today + "|" + string.Join(",", ids);
            return true;
        }

        /// <summary>
        /// Online, partita non finita, almeno una smazzata giocata fino in fondo (prima resta il bot: contro gli abbandoni
        /// concordati) e nessuno degli avversari umani di inizio partita ancora nella stanza. Chi e' solo disconnesso conta
        /// finche' Photon gli tiene il posto (60 s): poi arriva l'uscita vera.
        /// </summary>
        private bool CanForfeit(GameState state)
        {
            if (state == null || !GameModeService.Current.IsMultiplayer || !PhotonNetwork.InRoom) return false;
            if (!opponentLeftMidMatch || state.RoundIndex <= 1 && !state.RoundEnded) return false;
            if (MatchScore.IsFinished(state, (GameSceneInitializer.ActiveConfig ?? new MatchConfig()).TargetScore)) return false;
            if (turn == null) turn = FindObjectOfType<TurnController>();
            var seats = FindObjectOfType<GameSceneInitializer>();
            int local = GameModeService.Current.LocalPlayerIndex;
            if (turn == null || seats == null || local < 0) return false;
            if (seats.IsRemovedForInactivity(local)) return false; // tolto per inattivita' lui stesso: ha gia' abbandonato
            int mine = MatchScore.EntryOf(state, local);
            bool hadOpponent = false;
            foreach (var (seat, actor) in seats.Roster())
            {
                if (seat < 0 || seat == local || MatchScore.EntryOf(state, seat) == mine) continue;
                hadOpponent = true;
                // Tolto per inattivita' (3 tempi scaduti): per la partita e' uscito anche se il suo telefono e' ancora nella stanza.
                if (PhotonNetwork.CurrentRoom.GetPlayer(actor) != null && !seats.IsRemovedForInactivity(seat)) return false;
            }
            return hadOpponent;
        }

        private int captionSeconds = int.MinValue; // ultimo conto scritto sotto PROSSIMA SMAZZATA

        /// <summary>Partita finita e mostrata (anche senza pannello, nel tutorial), finche' non si va avanti.</summary>
        public bool MatchOver => finished && shownState != null && !clicked;

        private void Reveal(int localEntry, int winner)
        {
            // Test 8 (terzo giro 08/10): a fine tutorial solo la sua schermata finale (GIOCA ORA / VAI ALLA HOME), niente risultati sotto.
            // La partita si chiude come sempre: MatchOver per il tutorial, Rematch (Next) per GIOCA ORA.
            if (finished && Project51.Unity.UI.UI51TutorialView.Running) { GameAudio.Play(winner == localEntry ? SoundId.Victory : SoundId.Defeat); return; }
            var panel = finished ? MatchPanel : RoundPanel;
            panel.SetActive(true);
            if (!finished) autoAdvance.Start();
            var group = panel.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.DOFade(1f, UIV2Motion.Enter).SetUpdate(true).SetLink(panel);
            // ShowXp prima delle entrate: fissa l'altezza delle ricompense e sposta le statistiche, che FadeUp poi prende come posa di riposo.
            if (finished) ShowXp();
            View.Play(finished);
            if (Confetti) PlayConfetti();
            if (finished && winner == localEntry && celebratedState != shownState)
            {
                celebratedState = shownState;
                GameFeedback.Present(FeedbackKind.Victory, true, new Vector2(.5f, .72f));
            }
            if (!finished) GameAudio.PlayUi(SoundId.PopupOpen);
            else GameAudio.Play(winner == localEntry ? SoundId.Victory : SoundId.Defeat);
        }

        /// <summary>Scope e accusi del giocatore locale sommati sulle smazzate della partita (per il bonus XP).</summary>
        private void CountLocalBonuses(GameState state)
        {
            if (state.RoundIndex == countedRound) return;
            countedRound = state.RoundIndex;
            if (state.RoundIndex <= 1) matchScope = matchAccusi = matchSettebelli = 0;
            int local = GameModeService.Current.LocalPlayerIndex;
            if (local < 0 || local >= state.NumPlayers) return;
            matchScope += state.Players[local].ScopaCount;
            matchAccusi += state.Players[local].RoundAccusiCount;
            if (state.Players[local].CapturedCards.Any(c => c.IsSetteBello)) matchSettebelli++;
        }

        /// <summary>E1: una registrazione per partita, sullo stesso progresso che mostra la Home (cloud se caricato, altrimenti locale).</summary>
        private ServerReward coinReward;
        private bool coinFailed;

        private void RecordMatch(bool won, int winner)
        {
            recordedState = shownState;
            recorded = true;
            // B33: la partita guidata non conta (statistiche, XP, monete): niente riga delle ricompense.
            if (Project51.Unity.UI.UI51TutorialView.Running) { xpFrom = -1; return; }
            opponentLeftMidMatch = forfeitCheck = false;
            ModerationService.MatchEnded();
            // La RIVINCITA e' una partita nuova: se nel frattempo e' arrivata una sospensione non si puo' iniziare (scelta dell'utente 01/10).
            if (GameModeService.Current.IsMultiplayer) ModerationService.CheckBeforeRematch();
            bool training = GameSceneInitializer.ActiveConfig?.Intent == MatchIntent.Training;
            bool guest = AuthBootstrapper.Instance == null || AuthBootstrapper.Instance.PlayFabAuth == null || !AuthBootstrapper.Instance.PlayFabAuth.HasRealLogin;
            // Gli ospiti non guadagnano XP ne' ricompense: la riga invita a registrarsi.
            guestXp = guest;
            int xp = guest ? 0 : PlayerXp.MatchAward(won, matchScope, matchAccusi, training);
            if (Forfeit && xp > 0)
            {
                string saved = PlayerPrefs.GetString("Moderazione.AbbandoniPremiati", string.Empty);
                if (ForfeitRewarded(forfeitOpponent, DateTime.Now.ToString("yyyy-MM-dd"), ref saved)) PlayerPrefs.SetString("Moderazione.AbbandoniPremiati", saved);
                else xp = 0;
            }
            var local = PlayerProgressLocal.Instance;
            var cloud = AuthBootstrapper.Instance != null ? AuthBootstrapper.Instance.Profile : null;
            bool cloudLoaded = cloud != null && cloud.IsLoaded;
            xpFrom = cloudLoaded && !guest ? cloud.XP : local != null ? local.Exp : -1;
            xpTo = xpFrom + xp;
            if (local != null) local.RecordGameResult(won, xp);
            // Monete, XP e statistiche dal server (scelta dell'utente 01/10: 40/20, meta' coi bot, tetto giornaliero), solo per chi ha
            // un account: il telefono manda l'esito, non i numeri.
            coinReward = null;
            coinFailed = false;
            if (!guest)
            {
                // #141/#142: biglietto della partita e dichiarazione del risultato (punti di tutti, vincitore, posto); se il biglietto non
                // e' ancora arrivato si chiede adesso (risposta persa: il server da' lo stesso), senza (rete) vale l'id del telefono. Il
                // risultato resta sul telefono finche' il server non da' una risposta definitiva (RewardsService.RetryMatches in Home);
                // "in verifica" (si aspettano le altre persone) si ritenta qui dopo 10 e 30 s.
                var claim = Declaration(shownState, won, winner);
                verifyTries = 0;
                Action<ServerReward> done = null;
                done = r =>
                {
                    if (r.occupato || (r.partitaNonValida && !RewardsService.KeepsPending(r))) { Failed(cloud); return; }
                    if (cloud != null) cloud.ApplyServerStats(r);
                    coinReward = r;
                    if (this == null) return;
                    if (RewardsService.KeepsPending(r) && r.stato != "incompleta") { View.ShowCoins(r); RetryLater(claim, done); }
                    else ServerXp(r);
                };
                Action<string> failed = _ => Failed(cloud);
                if (matchTicket != null) { claim.partita = matchTicket; RewardsService.MatchReward(claim, done, failed); }
                else RewardsService.OpenMatch(RoomName, matchLocal, t =>
                {
                    if (t.ok && !string.IsNullOrEmpty(t.partita)) claim.partita = t.partita;
                    RewardsService.MatchReward(claim, done, failed);
                }, _ => RewardsService.MatchReward(claim, done, failed));
                matchTicket = matchLocal = null; // la rivincita chiede il suo
            }
            forfeitOpponent = null; // la rivincita parte pulita
            forfeitActor = 0;
        }

        /// <summary>
        /// Risultato dichiarato al server (#142 Fase A): punti finali di ogni concorrente, smazzate, concorrente vincente, posto del
        /// giocatore, squadre, giocatori, rientri. Gli altri telefoni della partita mandano gli stessi numeri: il server confronta.
        /// </summary>
        private PendingMatch Declaration(GameState state, bool won, int winner)
        {
            matchLocal = matchLocal ?? NewLocalId();
            var claim = new PendingMatch
            {
                locale = matchLocal, stanza = RoomName, vinta = won, abbandono = Forfeit, attore = forfeitActor, scope = matchScope,
                accusi = matchAccusi, punti = state != null ? MatchScore.Totals(state) : new int[0], smazzate = state != null ? state.RoundIndex : 0,
                vincitore = winner, posto = GameModeService.Current.LocalPlayerIndex, squadre = state != null && state.TeamMode,
                giocatori = state != null ? state.NumPlayers : 0, rientri = Project51.Networking.NetworkGameController.Rejoins
            };
            Project51.Networking.NetworkGameController.Rejoins = 0;
            return claim;
        }

        private static string NewLocalId() => Guid.NewGuid().ToString("N").Substring(0, 16);

        /// <summary>"In verifica": nuovo tentativo dopo 10 s e dopo altri 30 s, poi ci pensa la Home.</summary>
        private void RetryLater(PendingMatch claim, Action<ServerReward> done)
        {
            if (verifyTries >= 2) return;
            StartCoroutine(RetryAfter(verifyTries++ == 0 ? 10f : 30f, claim, done));
        }

        private System.Collections.IEnumerator RetryAfter(float seconds, PendingMatch claim, Action<ServerReward> done)
        {
            yield return new WaitForSecondsRealtime(seconds);
            RewardsService.RetryMatch(claim.partita, claim.locale, done);
        }

        /// <summary>Premio non confermato (rete, server occupato, biglietto mancante): la riga lo dice, le statistiche si rileggono.</summary>
        private void Failed(ProfileService cloud)
        {
            coinFailed = true;
            cloud?.RefreshStatistics();
            if (this != null) View.ShowCoins(null, true);
        }

        /// <summary>
        /// XP decisi dal server (limite dei risultati, abbandono non valido o oltre il limite, sospensione): se diversi dalla stima del
        /// telefono la riga si corregge, anche a pannello gia' aperto. Senza statistiche (limite, sospeso) sono +0.
        /// </summary>
        private void ServerXp(ServerReward r)
        {
            int from = r.statistiche ? r.esperienza - r.xp : xpFrom, to = r.statistiche ? r.esperienza : xpFrom;
            bool changed = xpFrom >= 0 && recordedState == shownState && (from != xpFrom || to != xpTo);
            if (changed) { xpFrom = from; xpTo = to; }
            if (changed && MatchPanel != null && MatchPanel.activeInHierarchy) View.SetXp(xpFrom, xpTo);
            View.ShowCoins(r);
        }

        /// <summary>
        /// B5: uscire da una partita non ancora finita conta come sconfitta, senza XP. disciplinary = conta anche per le sospensioni
        /// (no quando il posto e' stato tolto dal tavolo ma questo telefono non ha visto da se' i tempi scaduti: scelta dell'utente 02/10).
        /// </summary>
        public static void RecordAbandon(bool disciplinary = true)
        {
            var turn = FindObjectOfType<TurnController>();
            var state = turn != null ? turn.GameState : null;
            if (Project51.Unity.UI.UI51TutorialView.Running) return; // B33: lasciare il tutorial non e' un abbandono
            if (state == null || state == forfeitState || MatchScore.IsFinished(state, (GameSceneInitializer.ActiveConfig ?? new MatchConfig()).TargetScore)) return;
            if (disciplinary) ModerationService.Abandoned(); // conta solo online con altre persone e con un account
            else ModerationService.MatchEnded();
            if (PlayerProgressLocal.Instance != null) PlayerProgressLocal.Instance.RecordGameResult(false);
            var cloud = AuthBootstrapper.Instance != null ? AuthBootstrapper.Instance.Profile : null;
            // Anche col profilo non ancora caricato (ST1): la partita persa la conta il server, la cache non serve.
            if (cloud != null && HasAccount) RewardsService.MatchQuit(matchTicket, cloud.ApplyServerStats);
            matchTicket = matchLocal = null;
        }

        private static bool HasAccount => AuthBootstrapper.Instance != null && AuthBootstrapper.Instance.PlayFabAuth != null &&
            AuthBootstrapper.Instance.PlayFabAuth.HasRealLogin;

        private static string RoomName => GameModeService.Current.IsMultiplayer && PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : null;

        /// <summary>
        /// #141: a partita in corso (anche dopo un rientro) si chiede il biglietto al server, finche' non arriva (ogni 5 s). Niente per
        /// ospiti e tutorial, che non sono premiati.
        /// </summary>
        private void EnsureTicket()
        {
            if (matchTicket != null || ticketRequest || recorded || Time.unscaledTime < nextTicketTry) return;
            if (turn?.GameState == null || turn.GameState.RoundEnded || Project51.Unity.UI.UI51TutorialView.Running || !HasAccount) return;
            ticketRequest = true;
            matchLocal = matchLocal ?? NewLocalId();
            RewardsService.OpenMatch(RoomName, matchLocal, r =>
            {
                ticketRequest = false;
                if (this == null) return; // scena chiusa nel frattempo: la prossima lo richiede
                if (r.ok && !string.IsNullOrEmpty(r.partita) && !recorded) matchTicket = r.partita;
                else nextTicketTry = Time.unscaledTime + Mathf.Max(5f, r.attendi); // attendi: un biglietto nuovo al minuto
            }, _ => { ticketRequest = false; nextTicketTry = Time.unscaledTime + 5f; });
        }

        /// <summary>Riga "+XP" e monete del fine partita.</summary>
        private void ShowXp()
        {
            View.ShowXp(xpFrom, xpTo, guestXp);
            if (!guestXp) View.ShowCoins(coinReward, coinFailed);
        }

        private void PlayConfetti()
        {
            if (ConfettiRoot == null) return;
            StopConfetti();
            if (GamePreferences.ReducedGraphics) return;
            ConfettiRoot.gameObject.SetActive(true);
            float height = ConfettiRoot.rect.height;
            foreach (RectTransform piece in ConfettiRoot)
            {
                piece.DOKill();
                var start = piece.anchoredPosition;
                float duration = UnityEngine.Random.Range(4f, 7f);
                piece.anchoredPosition = new Vector2(start.x, UnityEngine.Random.Range(0f, height * 0.4f));
                piece.DOAnchorPosY(-height - 60f, duration).SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart)
                    .SetDelay(UnityEngine.Random.Range(0f, 2f)).SetUpdate(true).SetLink(piece.gameObject);
                piece.DORotate(new Vector3(0f, 0f, UnityEngine.Random.Range(-360f, 360f)), duration, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear).SetLoops(-1, LoopType.Incremental).SetUpdate(true).SetLink(piece.gameObject);
            }
        }

        private void StopConfetti()
        {
            if (ConfettiRoot == null) return;
            foreach (RectTransform piece in ConfettiRoot) piece.DOKill();
            ConfettiRoot.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GamePreferences.Changed += RefreshConfetti;
            RefreshConfetti();
        }

        private void OnDisable()
        {
            autoAdvance.Cancel();
            GamePreferences.Changed -= RefreshConfetti;
            StopConfetti();
        }

        private void RefreshConfetti()
        {
            if (GamePreferences.ReducedGraphics) StopConfetti();
            else if (finished && MatchPanel != null && MatchPanel.activeInHierarchy &&
                     ConfettiRoot != null && !ConfettiRoot.gameObject.activeSelf && Confetti) PlayConfetti();
        }

        // UI51: coriandoli solo per chi vince (mockup), e non sulla vittoria immediata, dove piovono monete.
        private bool Confetti => finished && (View == null || wonMatch && !View.Instant);

        // Solo l'host fa proseguire; se l'host esce, Photon passa il ruolo a un altro giocatore.
        private void RefreshContinue()
        {
            bool canAdvance = !GameModeService.Current.IsMultiplayer || GameModeService.Current.IsMasterClient;
            string label = NoRematch ? "TORNA ALLA HOME" : canAdvance ? (finished ? "RIVINCITA" : "PROSSIMA SMAZZATA") : "ATTENDI GLI ALTRI";
            // UI51: il conto sta sotto il pulsante ("Prossima smazzata tra N secondi"), solo per chi fa proseguire.
            int seconds = canAdvance && autoAdvance.IsRunning ? Mathf.CeilToInt(autoAdvance.Remaining) : -1;
            if (!finished && seconds != captionSeconds)
            {
                captionSeconds = seconds;
                View.SetRoundCaption(seconds >= 0 ? "Prossima smazzata tra " + seconds + (seconds == 1 ? " secondo" : " secondi") : null);
            }
            // UI51: senza rivincita resta solo "Torna alla Home" in fondo, non due pulsanti uguali.
            if (finished && Rematch.gameObject.activeSelf == NoRematch) Rematch.gameObject.SetActive(!NoRematch);
            var button = finished ? Rematch : RoundContinue;
            var text = finished ? RematchLabel : RoundContinueLabel;
            button.interactable = canAdvance || NoRematch;
            if (text.text != label) text.text = label;
        }

        /// <summary>Partita online finita e gioco online sospeso, o vinta per abbandono: niente rivincita, il pulsante riporta alla Home.</summary>
        private bool NoRematch => finished && GameModeService.Current.IsMultiplayer && (ModerationService.IsSuspended || Forfeit);

        private void Next()
        {
            if (NoRematch) { Exit(); return; }
            if (clicked || GameModeService.Current.IsMultiplayer && !GameModeService.Current.IsMasterClient) return;
            // TurnController.StartNewGame riparte da zero da solo se la partita e' finita (MatchScore.ContinueMatch).
            clicked = true;
            var callback = next;
            Hide();
            callback?.Invoke();
        }

        private void Exit()
        {
            if (clicked) return;
            clicked = true;
            RecordAbandon();
            menu?.Invoke();
        }

        private void Update()
        {
            if (forfeitCheck) CheckForfeit();
            RememberOpponentIds();
            // Si gioca di nuovo dopo una partita registrata: e' la rivincita, si conta da capo.
            if (recorded && turn?.GameState != null && !turn.GameState.RoundEnded) { recorded = false; countedRound = 0; }
            EnsureTicket();
            bool visible = RoundPanel.activeInHierarchy || MatchPanel.activeInHierarchy;
            if (!visible) return;
            // Includes a just-promoted host: stale results must not start another round.
            if (turn == null) turn = FindObjectOfType<TurnController>();
            if (turn?.GameState != null && !turn.GameState.RoundEnded && !Forfeit)
            {
                // L'host ha avviato la rivincita ma io sono sospeso: esco prima della prima mossa (non conta come abbandono).
                bool leave = NoRematch;
                Hide();
                if (leave) AppFlowManager.LeaveGameAndGoToMenu();
                return;
            }
            if (!finished && RoundPanel.activeInHierarchy)
            {
                bool blocked = applicationPaused || !Application.isFocused || AppLoading.IsCovering || Project51.Unity.UI.UI51TutorialView.Holding ||
                    (settings != null && (settings.IsOpen || settings.IsLeaveOpen)) || RoundPanel.GetComponent<CanvasGroup>().alpha < .99f;
                bool authority = !GameModeService.Current.IsMultiplayer || GameModeService.Current.IsMasterClient;
                // Background/resume must not consume the entire reading interval in one frame.
                if (autoAdvance.Advance(Mathf.Min(Time.unscaledDeltaTime, .25f), authority, blocked)) { Next(); return; }
            }
            RefreshContinue();
        }

        private void OnApplicationPause(bool paused) => applicationPaused = paused;

        public void Hide()
        {
            autoAdvance.Cancel();
            foreach (var panel in new[] { RoundPanel, MatchPanel })
            {
                panel.GetComponent<CanvasGroup>().DOKill();
                panel.SetActive(false);
            }
            StopConfetti();
        }

        // 3 tempi scaduti di fila (TurnController.InactiveTooLong): si esce come con Abbandona, conta come abbandono e il posto passa al bot.
        private void LeftForInactivity()
        {
            Project51.Unity.UI.UI51Toast.Show("Sei uscito dalla partita: 3 turni senza giocare", Project51.Unity.UI.UI51Toast.Kind.Error);
            // Per le sospensioni solo se questo telefono ha visto scadere da se' i 3 tempi: un master modificato non puo' farlo contare.
            if (turn != null && !turn.InactivityConfirmed) { RecordAbandon(false); AppFlowManager.LeaveGameAndGoToMenu(); }
            else if (settings != null) settings.ConfirmLeave();
            else { RecordAbandon(); AppFlowManager.LeaveGameAndGoToMenu(); }
        }

        private void OnDestroy()
        {
            if (turn != null) turn.InactiveTooLong -= LeftForInactivity;
            GamePresentation.RoundResults -= Show;
            GamePresentation.HideResults -= Hide;
        }
    }
}
