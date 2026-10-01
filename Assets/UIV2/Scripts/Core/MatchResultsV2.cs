using System;
using System.Linq;
using DG.Tweening;
using Project51.Auth;
using Project51.UIV2.Animations;
using Project51.Core;
using Project51.UIV2.Components;
using Project51.Unity;
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
        public TMP_Text RoundSubtitle;
        public ResultRowV2[] RoundRows;
        /// <summary>Vincitori di Carte, Denari, Settebello, Primiera (in quest'ordine).</summary>
        public TMP_Text[] AwardWinners;
        public Button RoundContinue;
        public TMP_Text RoundContinueLabel;
        public Button RoundExit;
        [Tooltip("Tavolo sfocato dietro al fine smazzata (G6). Costruito da Tools/UIV2/Build Round Results Blur.")]
        public BackdropBlur RoundBlur;

        [Header("Fine partita")]
        public GameObject MatchPanel;
        public TMP_Text MatchTitle;
        public TMP_Text MatchWinnerLine;
        public ResultRowV2[] MatchRows;
        public TMP_Text[] DetailLabels;
        public TMP_Text[] DetailValues;
        public TMP_Text DetailTotal;
        public Button Rematch;
        public TMP_Text RematchLabel;
        public Button MatchMenu;
        public RectTransform ConfettiRoot;
        /// <summary>Scoppio di luce dal trofeo quando vince il giocatore locale (C6).</summary>
        public UIV2MoteField TrophyBurst;

        [Header("Esperienza (E1)")]
        public GameObject XpRow;
        public TMP_Text XpGainLabel;
        public UIV2ProgressBar XpBar;
        public TMP_Text XpLevelLabel;
        public Image XpFlash;
        /// <summary>Scoppio di luce sulla barra quando si sale di livello.</summary>
        public UIV2MoteField XpBurst;

        [Header("UI51 Fase 7")]
        [Tooltip("Grafica dei mockup FineSmazzata/FinePartita (UI51TableBuilder.BuildResults). Vuoto = vecchi pannelli.")]
        public Project51.Unity.UI.UI51ResultsView View;

        private Action next, menu;
        private bool finished, clicked, wonMatch;
        private GameState shownState, celebratedState;
        private readonly RoundAdvanceCountdown autoAdvance = new RoundAdvanceCountdown();
        private InGameSettingsV2 settings;
        private TurnController turn;
        private bool applicationPaused;
        // ponytail: guardia per riferimento; un resync che rimanda la stessa fine smazzata come nuovo oggetto la conterebbe due volte.
        private GameState countedState, recordedState;
        private int matchScope, matchAccusi, matchSettebelli, xpFrom = -1, xpTo;
        private bool guestXp;
        private Tween xpTween;

        private void Awake()
        {
            settings = FindObjectOfType<InGameSettingsV2>(true);
            turn = FindObjectOfType<TurnController>();
            GamePresentation.RoundResults += Show;
            GamePresentation.HideResults += Hide;
            RoundContinue.onClick.AddListener(Next);
            Rematch.onClick.AddListener(Next);
            // Il fine smazzata del mockup 13 non ha un'uscita: si abbandona dalle Impostazioni in partita.
            if (RoundExit != null) RoundExit.onClick.AddListener(Exit);
            MatchMenu.onClick.AddListener(Exit);
            RoundPanel.SetActive(false);
            MatchPanel.SetActive(false);
        }

        /// <summary>Nome di un concorrente: il giocatore, oppure a coppie i due compagni.</summary>
        public static string EntryName(GameState state, int entry) =>
            string.Join(" e ", MatchScore.MembersOf(state, entry).Select(GameSocialV2.PlayerName));

        public void Show(GameState state, Action nextRound, Action mainMenu)
        {
            autoAdvance.Cancel();
            captionSeconds = int.MinValue;
            shownState = state;
            next = nextRound;
            menu = mainMenu;
            clicked = false;
            int target = (GameSceneInitializer.ActiveConfig ?? new MatchConfig()).TargetScore;
            var totals = MatchScore.Totals(state);
            var round = MatchScore.RoundScores(state);
            finished = MatchScore.IsFinished(state, target);
            int localEntry = MatchScore.EntryOf(state, GameModeService.Current.LocalPlayerIndex);
            var order = Enumerable.Range(0, totals.Length).OrderByDescending(e => totals[e]).ToArray();
            var rows = finished ? MatchRows : RoundRows;
            CountLocalBonuses(state);
            wonMatch = finished && order[0] == localEntry;
            if (finished && recordedState != state) RecordMatch(wonMatch);

            for (int row = 0; row < rows.Length; row++)
            {
                if (row >= order.Length) { rows[row].gameObject.SetActive(false); continue; }
                int entry = order[row];
                string delta = (MatchScore.IsCappotto(round[entry]) ? "Cappotto" : "+" + round[entry]) + " questa smazzata";
                rows[row].Bind(row + 1, EntryName(state, entry), delta, totals[entry], target,
                    MatchScore.IsCappotto(totals[entry]), winner: finished && row == 0);
            }

            if (View != null)
            {
                if (finished) View.BindMatch(state, localEntry, order[0], matchScope, matchAccusi, matchSettebelli);
                else View.BindRound(state, target, localEntry);
            }
            else
            {
                var breakdown = PunteggioManager.CalculateBreakdown(state);
                if (finished) BindMatchEnd(state, breakdown, totals, order[0], localEntry);
                else BindRoundEnd(state, breakdown);
            }

            RefreshContinue();
            (finished ? RoundPanel : MatchPanel).SetActive(false);
            int token = ++showToken;
            // Il fine partita e' a schermo intero con fondo pieno: la sfocatura servirebbe solo al fine smazzata.
            if (!finished && RoundBlur != null) StartCoroutine(RevealAfterBlur(token, localEntry, order[0]));
            else Reveal(localEntry, order[0]);
        }

        private int showToken;
        private int captionSeconds = int.MinValue; // ultimo conto scritto sotto PROSSIMA SMAZZATA

        /// <summary>La foto del tavolo va scattata prima che il pannello compaia, poi si mostra tutto insieme.</summary>
        private System.Collections.IEnumerator RevealAfterBlur(int token, int localEntry, int winner)
        {
            yield return RoundBlur.Capture();
            if (token == showToken) Reveal(localEntry, winner); // nel frattempo Hide() o un nuovo Show()
        }

        private void Reveal(int localEntry, int winner)
        {
            var panel = finished ? MatchPanel : RoundPanel;
            panel.SetActive(true);
            if (!finished) autoAdvance.Start();
            var group = panel.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.DOFade(1f, UIV2Motion.Enter).SetUpdate(true).SetLink(panel);
            foreach (var row in finished ? MatchRows : RoundRows)
                if (row != null && row.gameObject.activeInHierarchy) row.AnimateScore();
            if (View != null) View.Play(finished);
            if (finished) { if (wonMatch || View == null) PlayConfetti(); ShowXp(); } // UI51: coriandoli solo per chi vince (mockup)
            if (finished && winner == localEntry && celebratedState != shownState)
            {
                celebratedState = shownState;
                GameFeedback.Present(FeedbackKind.Victory, true, new Vector2(.5f, .72f));
            }
            if (finished && winner == localEntry && TrophyBurst != null)
                DOVirtual.DelayedCall(0.25f, TrophyBurst.Burst).SetUpdate(true).SetLink(panel);
            if (!finished) GameAudio.PlayUi(SoundId.PopupOpen);
            else GameAudio.Play(winner == localEntry ? SoundId.Victory : SoundId.Defeat);
        }

        /// <summary>Scope e accusi del giocatore locale sommati sulle smazzate della partita (per il bonus XP).</summary>
        private void CountLocalBonuses(GameState state)
        {
            if (countedState == state) return;
            countedState = state;
            if (state.RoundIndex <= 1) matchScope = matchAccusi = matchSettebelli = 0;
            int local = GameModeService.Current.LocalPlayerIndex;
            if (local < 0 || local >= state.NumPlayers) return;
            matchScope += state.Players[local].ScopaCount;
            matchAccusi += state.Players[local].RoundAccusiCount;
            if (state.Players[local].CapturedCards.Any(c => c.IsSetteBello)) matchSettebelli++;
        }

        /// <summary>E1: una registrazione per partita, sullo stesso progresso che mostra la Home (cloud se caricato, altrimenti locale).</summary>
        private void RecordMatch(bool won)
        {
            recordedState = shownState;
            bool training = GameSceneInitializer.ActiveConfig?.Intent == MatchIntent.Training;
            bool guest = AuthBootstrapper.Instance == null || AuthBootstrapper.Instance.PlayFabAuth == null || !AuthBootstrapper.Instance.PlayFabAuth.HasRealLogin;
            // Gli ospiti non guadagnano XP ne' ricompense: la riga invita a registrarsi.
            guestXp = guest;
            int xp = guest ? 0 : PlayerXp.MatchAward(won, matchScope, matchAccusi, training);
            var local = PlayerProgressLocal.Instance;
            var cloud = AuthBootstrapper.Instance != null ? AuthBootstrapper.Instance.Profile : null;
            bool cloudLoaded = cloud != null && cloud.IsLoaded;
            xpFrom = cloudLoaded && !guest ? cloud.XP : local != null ? local.Exp : -1;
            xpTo = xpFrom + xp;
            if (local != null) local.RecordGameResult(won, xp);
            if (cloudLoaded) cloud.RecordGameResult(won, xp, guest ? 0 : matchScope);
        }

        /// <summary>B5: uscire da una partita non ancora finita conta come sconfitta, senza XP.</summary>
        public static void RecordAbandon()
        {
            var turn = FindObjectOfType<TurnController>();
            var state = turn != null ? turn.GameState : null;
            if (state == null || MatchScore.IsFinished(state, (GameSceneInitializer.ActiveConfig ?? new MatchConfig()).TargetScore)) return;
            if (PlayerProgressLocal.Instance != null) PlayerProgressLocal.Instance.RecordGameResult(false);
            var cloud = AuthBootstrapper.Instance != null ? AuthBootstrapper.Instance.Profile : null;
            if (cloud != null && cloud.IsLoaded) cloud.RecordGameResult(false, 0);
        }

        /// <summary>Riga "+XP": la barra si riempie rallentando, lampeggia alla fine e scoppia a ogni livello.</summary>
        private void ShowXp()
        {
            xpTween?.Kill();
            if (View != null) { View.ShowXp(xpFrom, xpTo, guestXp); return; }
            if (XpRow == null) return;
            XpRow.SetActive(xpFrom >= 0);
            if (xpFrom < 0) return;
            XpBar.gameObject.SetActive(!guestXp);
            XpLevelLabel.gameObject.SetActive(!guestXp);
            XpGainLabel.enableWordWrapping = false;
            XpGainLabel.text = guestXp ? "Registrati per guadagnare XP" : "+" + (xpTo - xpFrom) + " XP";
            if (guestXp) return;
            XpFlash.DOKill();
            XpFlash.color = new Color(XpFlash.color.r, XpFlash.color.g, XpFlash.color.b, 0f);
            if (GamePreferences.ReducedGraphics) { SetXp(xpTo); return; }
            SetXp(xpFrom);
            int level = PlayerXp.LevelOf(xpFrom);
            xpTween = DOVirtual.Float(xpFrom, xpTo, 1.2f, v =>
                {
                    int total = Mathf.RoundToInt(v);
                    SetXp(total);
                    if (PlayerXp.LevelOf(total) == level) return;
                    level = PlayerXp.LevelOf(total);
                    XpLevelLabel.transform.DOKill(true);
                    XpLevelLabel.transform.DOPunchScale(Vector3.one * .35f, .45f, 6).SetUpdate(true).SetLink(XpRow);
                    if (XpBurst != null) XpBurst.Burst();
                })
                .SetDelay(.45f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(XpRow)
                .OnComplete(() => XpFlash.DOFade(.6f, .12f).SetLoops(2, LoopType.Yoyo).SetUpdate(true).SetLink(XpRow));
        }

        private void SetXp(int total)
        {
            int level = PlayerXp.LevelOf(total);
            XpBar.SetProgress(PlayerXp.XpInLevel(total), PlayerXp.XpToNext(level), animate: false);
            XpLevelLabel.text = "Liv. " + level;
        }

        private void BindRoundEnd(GameState state, SmazzataScore[] breakdown)
        {
            RoundSubtitle.text = "Smazzata " + state.RoundIndex + "  ·  mazzo esaurito";
            Func<Func<SmazzataScore, bool>, string> awarded = won =>
            {
                var names = Enumerable.Range(0, breakdown.Length).Where(e => won(breakdown[e])).Select(e => EntryName(state, e)).ToArray();
                return names.Length == 0 ? "Nessuno" : string.Join(", ", names);
            };
            AwardWinners[0].text = awarded(x => x.WonCards);
            AwardWinners[1].text = awarded(x => x.WonDenari);
            AwardWinners[2].text = awarded(x => x.HasSetteBello);
            AwardWinners[3].text = awarded(x => x.WonPrimiera);
        }

        private void BindMatchEnd(GameState state, SmazzataScore[] breakdown, int[] totals, int winner, int localEntry)
        {
            bool won = winner == localEntry;
            MatchTitle.text = won ? "HAI VINTO!" : "FINE PARTITA";
            MatchWinnerLine.text = EntryName(state, winner) + "  ·  "
                + (MatchScore.IsCappotto(totals[winner]) ? "Cappotto" : totals[winner] + " punti");

            // Dettaglio dell'ultima smazzata per il giocatore (o la coppia) locale.
            var d = breakdown[Mathf.Clamp(localEntry, 0, breakdown.Length - 1)];
            int bonus = (d.HasGrande ? 5 : 0) + (d.HasPiccola ? 3 + d.PiccolaExtras : 0);
            var lines = new[]
            {
                ("Carte (" + d.CardCount + " su 40)", d.WonCards ? 1 : 0),
                ("Denari (" + d.DenariCount + " su 10)", d.WonDenari ? 1 : 0),
                ("Settebello", d.HasSetteBello ? 1 : 0),
                ("Primiera", d.WonPrimiera ? 1 : 0),
                ("Scope", d.ScopaCount),
                ("Accusi", d.AccusiPoints),
                ("Grande e Piccola", bonus)
            };
            for (int i = 0; i < DetailLabels.Length; i++)
            {
                // La riga Grande/Piccola compare solo se ha dato punti, come nel mockup a 6 righe.
                bool show = i < lines.Length && (i < 6 || lines[i].Item2 > 0);
                DetailLabels[i].gameObject.SetActive(show);
                DetailValues[i].gameObject.SetActive(show);
                if (!show) continue;
                DetailLabels[i].text = lines[i].Item1;
                DetailValues[i].text = "+" + lines[i].Item2;
            }
            DetailTotal.text = "+" + (d.Points + d.AccusiPoints);
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
                     ConfettiRoot != null && !ConfettiRoot.gameObject.activeSelf && (wonMatch || View == null)) PlayConfetti();
        }

        // Solo l'host fa proseguire; se l'host esce, Photon passa il ruolo a un altro giocatore.
        private void RefreshContinue()
        {
            bool canAdvance = !GameModeService.Current.IsMultiplayer || GameModeService.Current.IsMasterClient;
            string label = canAdvance ? (finished ? "RIVINCITA" : View != null ? "PROSSIMA SMAZZATA" : autoAdvance.IsRunning
                ? "CONTINUA · " + Mathf.CeilToInt(autoAdvance.Remaining) + "s" : "CONTINUA") : "ATTENDI L'HOST";
            // UI51: il conto sta sotto il pulsante ("Si riparte da sola tra N secondi"), solo per chi fa proseguire.
            int seconds = canAdvance && autoAdvance.IsRunning ? Mathf.CeilToInt(autoAdvance.Remaining) : -1;
            if (View != null && !finished && seconds != captionSeconds)
            {
                captionSeconds = seconds;
                View.SetRoundCaption(seconds >= 0 ? "Si riparte da sola tra " + seconds + " secondi" : null);
            }
            var button = finished ? Rematch : RoundContinue;
            var text = finished ? RematchLabel : RoundContinueLabel;
            button.interactable = canAdvance;
            if (text.text != label) text.text = label;
        }

        private void Next()
        {
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
            bool visible = RoundPanel.activeInHierarchy || MatchPanel.activeInHierarchy;
            if (!visible) return;
            // Includes a just-promoted host: stale results must not start another round.
            if (turn == null) turn = FindObjectOfType<TurnController>();
            if (turn?.GameState != null && !turn.GameState.RoundEnded) { Hide(); return; }
            if (!finished && RoundPanel.activeInHierarchy)
            {
                bool blocked = applicationPaused || !Application.isFocused || AppLoading.IsCovering ||
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
            showToken++; // annulla un fine smazzata ancora in attesa della foto sfocata
            foreach (var panel in new[] { RoundPanel, MatchPanel })
            {
                panel.GetComponent<CanvasGroup>().DOKill();
                panel.SetActive(false);
            }
            StopConfetti();
            xpTween?.Kill();
        }

        private void OnDestroy()
        {
            GamePresentation.RoundResults -= Show;
            GamePresentation.HideResults -= Hide;
        }
    }
}
