using Project51.Auth;
using Project51.Core;
using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 15: pagina Trofei (mockup Trofei) dal "Vedi tutti" del Profilo. Trofei di Trophies.All dalle statistiche del server:
    /// conto in alto, categorie (Tutti + Trophies.Categories), griglia a 3 e dettaglio dal basso. Niente date ne' premi (scelta 02/10).
    /// Ogni scheda Trophy{i} ha Medal, Name, Done e Progress (Track/Fill, Value). Grafica: UI51ProgressBuilder.
    /// </summary>
    public sealed class UI51TrophiesView : MonoBehaviour
    {
        [SerializeField] private Button back;
        [SerializeField] private TMP_Text count;
        [SerializeField] private RectTransform progressFill;
        [SerializeField] private Button[] chips = new Button[0];
        [SerializeField] private RectTransform[] cards = new RectTransform[0];
        [SerializeField] private Sprite[] medals = new Sprite[0];
        [SerializeField] private ScrollRect scroll;

        [Header("Dettaglio")]
        [SerializeField] private GameObject detail;
        [SerializeField] private RectTransform sheet;
        [SerializeField] private Button scrim, close;
        [SerializeField] private Image detailMedal;
        [SerializeField] private TMP_Text detailName, detailText, detailStatus;

        /// <summary>Medaglia spenta: il grayscale(1) brightness(.55) del mockup non c'e' in uGUI, grigio scuro a occhio.</summary>
        public static readonly Color Locked = new Color(0.42f, 0.42f, 0.45f, 0.85f);

        private int games, wins, scope, level;

        private void Awake()
        {
            back.onClick.AddListener(() => gameObject.SetActive(false));
            for (int i = 0; i < chips.Length; i++) { int c = i; chips[i].onClick.AddListener(() => Filter(c)); }
            for (int i = 0; i < cards.Length; i++) { int t = i; cards[i].GetComponent<Button>().onClick.AddListener(() => ShowDetail(t)); }
            scrim.onClick.AddListener(HideDetail);
            close.onClick.AddListener(HideDetail);
        }

        public void Open()
        {
            var p = AuthBootstrapper.Instance != null ? AuthBootstrapper.Instance.Profile : null;
            games = p != null ? p.TotalGames : 0; wins = p != null ? p.Wins : 0;
            scope = p != null ? p.TotalScope : 0; level = p != null ? p.Level : 1;
            gameObject.SetActive(true);
            detail.SetActive(false);
            int earned = Trophies.CountEarned(games, wins, scope, level), total = Trophies.All.Length;
            count.text = earned + " / " + total;
            var max = progressFill.anchorMax; max.x = (float)earned / total; progressFill.anchorMax = max;
            for (int i = 0; i < cards.Length && i < Trophies.All.Length; i++) BindCard(cards[i], Trophies.All[i]);
            Filter(0);
            UIAnim.FadeIn((RectTransform)transform, 0.2f);
        }

        /// <summary>
        /// Tre colonne su tutta la larghezza vera (390 nel mockup, 431 su iPhone 12), allineate a titolo e categorie. In LateUpdate perche'
        /// DesignCanvasFit allarga l'area sicura dopo l'apertura.
        /// </summary>
        private void LateUpdate()
        {
            var grid = scroll.content.GetComponent<GridLayoutGroup>();
            float w = (((RectTransform)scroll.viewport).rect.width - 2f * grid.spacing.x) / 3f;
            if (Mathf.Abs(grid.cellSize.x - w) > 0.5f) grid.cellSize = new Vector2(w, grid.cellSize.y);
        }

        private void BindCard(RectTransform card, Trophy t)
        {
            bool done = Trophies.Earned(t, games, wins, scope, level);
            int value = Trophies.Progress(t, games, wins, scope, level);
            var medal = card.Find("Medal").GetComponent<Image>();
            medal.sprite = Medal(t);
            medal.color = done ? Color.white : Locked;
            card.Find("Name").GetComponent<TMP_Text>().color = done ? UI51Tokens.Cream : UI51Tokens.CreamA(0.6f);
            card.GetComponent<UI51Shape>().borderColor = UI51Tokens.GoldA(done ? 0.5f : 0.18f);
            card.Find("Done").gameObject.SetActive(done);
            var progress = card.Find("Progress");
            progress.gameObject.SetActive(!done);
            var fill = (RectTransform)progress.Find("Track/Fill");
            var max = fill.anchorMax; max.x = (float)value / t.Target; fill.anchorMax = max;
            progress.Find("Value").GetComponent<TMP_Text>().text = value + " / " + t.Target;
        }

        /// <summary>0 = Tutti, poi le categorie di Trophies.</summary>
        private void Filter(int chip)
        {
            for (int i = 0; i < chips.Length; i++)
            {
                bool on = i == chip;
                var shape = chips[i].GetComponent<UI51Shape>();
                shape.fill = UI51Shape.Solid(on ? UI51Tokens.GoldA(0.16f) : UI51Tokens.Rgba(6, 13, 27, 0.6f));
                shape.borderColor = UI51Tokens.GoldA(on ? 0.7f : 0.2f);
                var label = chips[i].GetComponentInChildren<TMP_Text>();
                label.color = on ? UI51Tokens.Gold : UI51Tokens.CreamA(0.75f);
            }
            for (int i = 0; i < cards.Length && i < Trophies.All.Length; i++)
                cards[i].gameObject.SetActive(chip == 0 || Trophies.All[i].Category == chip - 1);
            scroll.verticalNormalizedPosition = 1f;
        }

        private void ShowDetail(int i)
        {
            var t = Trophies.All[i];
            bool done = Trophies.Earned(t, games, wins, scope, level);
            detailMedal.sprite = Medal(t);
            detailMedal.color = done ? Color.white : Locked;
            detailName.text = t.Name;
            detailText.text = t.Description;
            detailStatus.text = done ? "Ottenuto" : "Progresso: " + Trophies.Progress(t, games, wins, scope, level) + " / " + t.Target;
            detail.SetActive(true);
            UIAnim.FadeIn((RectTransform)scrim.transform, 0.25f);
            UIAnim.SheetUp(sheet);
        }

        private void HideDetail() => detail.SetActive(false);

        private Sprite Medal(Trophy t) => t.Medal < medals.Length ? medals[t.Medal] : null;
    }
}
