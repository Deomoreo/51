using System;
using System.Linq;
using DG.Tweening;
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

        private Action next, menu;
        private bool finished, clicked;

        private void Awake()
        {
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
            string.Join(" + ", MatchScore.MembersOf(state, entry).Select(GameSocialV2.PlayerName));

        public void Show(GameState state, Action nextRound, Action mainMenu)
        {
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

            for (int row = 0; row < rows.Length; row++)
            {
                if (row >= order.Length) { rows[row].gameObject.SetActive(false); continue; }
                int entry = order[row];
                string delta = (MatchScore.IsCappotto(round[entry]) ? "Cappotto" : "+" + round[entry]) + " questa smazzata";
                rows[row].Bind(row + 1, EntryName(state, entry), delta, totals[entry], target,
                    MatchScore.IsCappotto(totals[entry]), winner: finished && row == 0);
            }

            var breakdown = PunteggioManager.CalculateBreakdown(state);
            if (finished) BindMatchEnd(state, breakdown, totals, order[0], localEntry);
            else BindRoundEnd(state, breakdown);

            RefreshContinue();
            (finished ? RoundPanel : MatchPanel).SetActive(false);
            int token = ++showToken;
            // Il fine partita e' a schermo intero con fondo pieno: la sfocatura servirebbe solo al fine smazzata.
            if (!finished && RoundBlur != null) StartCoroutine(RevealAfterBlur(token, localEntry, order[0]));
            else Reveal(localEntry, order[0]);
        }

        private int showToken;

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
            var group = panel.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.DOFade(1f, 0.3f).SetUpdate(true).SetLink(panel);
            if (finished) PlayConfetti();
            if (finished && winner == localEntry && TrophyBurst != null)
                DOVirtual.DelayedCall(0.25f, TrophyBurst.Burst).SetUpdate(true).SetLink(panel);
            if (!finished) GameAudio.PlayUi(SoundId.PopupOpen);
            else GameAudio.Play(winner == localEntry ? SoundId.Victory : SoundId.Defeat);
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

        // Solo l'host fa proseguire; se l'host esce, Photon passa il ruolo a un altro giocatore.
        private void RefreshContinue()
        {
            bool canAdvance = !GameModeService.Current.IsMultiplayer || GameModeService.Current.IsMasterClient;
            string label = canAdvance ? (finished ? "RIVINCITA" : "CONTINUA") : "ATTENDI L'HOST";
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
            menu?.Invoke();
        }

        private void Update()
        {
            bool visible = RoundPanel.activeSelf || MatchPanel.activeSelf;
            if (!visible) return;
            RefreshContinue();
            // Il nuovo stato dell'host chiude i risultati anche sui client remoti.
            if (GameModeService.Current.IsMultiplayer && !GameModeService.Current.IsMasterClient)
            {
                var turn = FindObjectOfType<TurnController>();
                if (turn?.GameState != null && !turn.GameState.RoundEnded) Hide();
            }
        }

        public void Hide()
        {
            showToken++; // annulla un fine smazzata ancora in attesa della foto sfocata
            foreach (var panel in new[] { RoundPanel, MatchPanel })
            {
                panel.GetComponent<CanvasGroup>().DOKill();
                panel.SetActive(false);
            }
            if (ConfettiRoot != null) foreach (RectTransform piece in ConfettiRoot) piece.DOKill();
        }

        private void OnDestroy()
        {
            GamePresentation.RoundResults -= Show;
            GamePresentation.HideResults -= Hide;
        }
    }
}
