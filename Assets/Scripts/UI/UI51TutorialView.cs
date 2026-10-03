using DG.Tweening;
using Project51.Core;
using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 12: partita guidata (mockup TutorialPartita) sopra al tavolo vero di un allenamento 1v1 col bot facile.
    /// La distribuzione la fissa GameSceneInitializer.TutorialSeed; finita la distribuzione e la finestra dell'accuso,
    /// Nonna Rosa spiega 6 passi con AVANTI illuminando le carte vere, il bottone ACCUSA e il punteggio. Durante la guida
    /// non si gioca (il velo prende i tocchi); "GIOCA LA PRIMA PARTITA" chiude tutto e lascia giocare questa stessa partita.
    /// Grafica: UI51RulesBuilder (Tools/UI51/Build Fase 12).
    /// </summary>
    public sealed class UI51TutorialView : MonoBehaviour
    {
        [SerializeField] private RectTransform safe, coach, skipDialog, skipCard, done, doneBurst, doneHead;
        [Tooltip("Velo col buco: bordo largo Hole intorno al rettangolo illuminato.")]
        [SerializeField] private UI51Shape spot, ring, halo;
        [SerializeField] private RectTransform finger, fingerGlyph, bubble;
        [SerializeField] private UI51Shape[] dots = new UI51Shape[0];
        [SerializeField] private TMP_Text cap, title, text, nextLabel;
        [SerializeField] private Button next, back, skip, skipContinue, skipScrim, skipHome, play, rules;
        [SerializeField] private RectTransform scorePill, accuso;

        /// <summary>Larghezza del velo intorno al buco (box-shadow 0 0 0 2000px del mockup).</summary>
        public const float Hole = 3000f;
        const float Gap = 24f, TopLimit = 104f;

        static readonly string[] Titles =
        {
            "Le tue carte", "Prendi con la carta uguale", "Somme e regola del 15", "Scopa!", "Accusa", "Arriva a 51",
        };

        static readonly string[] Texts =
        {
            "Queste sono le tue 3 carte. A ogni turno ne giochi una: basta toccarla.",
            "L’Asso di denari prende l’Asso di bastoni sul tavolo. Le carte prese vanno nel tuo mazzetto e valgono punti a fine smazzata.",
            "Sul tavolo non c’è un altro 7, quindi il 7 può prendere per somma (3 + 4) o con la regola del 15 (7 + 4 + 3 + Asso). " +
            "Se c’è la carta uguale invece devi prendere quella. Quando le prese sono più di una, scegli tu.",
            "Se con una presa svuoti il tavolo fai scopa: vale 1 punto, anche con l’asso che piglia tutto. Le scope che fai spuntano dietro al tuo banner.",
            "Con 3 carte che sommano 9 o meno hai la Cirulla (3 punti); con 3 carte uguali il Decino (10 punti). " +
            "A ogni distribuzione questo bottone si accende: se hai un accuso, premilo entro 5 secondi.",
            "A fine smazzata contano carte, denari, settebello, primiera, grande, piccola, scope e accusi. Chi arriva per primo a 51 vince.",
        };

        /// <summary>"Leggi tutte le regole": tornati in Home si apre la pagina Regole (StartScreenV2).</summary>
        public static bool OpenRulesOnReturn { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => OpenRulesOnReturn = false;

        /// <summary>Allenamento 1v1 col bot facile e le regole del 1v1 del selettore delle modalità, con la distribuzione del tutorial.</summary>
        public static void Launch()
        {
            var launcher = FindObjectOfType<GameLaunchController>();
            if (launcher == null) return;
            var config = new MatchConfig
            {
                Intent = MatchIntent.Training, Format = GameFormat.OneVsOne, BotDifficulty = BotDifficulty.Easy,
                Rules = MatchRules.ForFormat(GameFormat.OneVsOne),
            };
            GameSceneInitializer.Tutorial = true;
            launcher.Launch(config);
        }

        private TurnController turn;
        private CardViewManager cards;
        private bool sawDeal;
        private int step = -1;
        private Rect spotRect;
        private float spotRadius;

        private void Awake()
        {
            // Awake gira prima di GameSceneInitializer.Start, che consuma il flag.
            if (!GameSceneInitializer.Tutorial) { gameObject.SetActive(false); return; }
            coach.gameObject.SetActive(false);
            skipDialog.gameObject.SetActive(false);
            done.gameObject.SetActive(false);
            next.onClick.AddListener(() => Show(step + 1));
            back.onClick.AddListener(() => Show(step - 1));
            skip.onClick.AddListener(() => SetSkip(true));
            skipContinue.onClick.AddListener(() => SetSkip(false));
            skipScrim.onClick.AddListener(() => SetSkip(false));
            skipHome.onClick.AddListener(Home);
            play.onClick.AddListener(() => gameObject.SetActive(false));
            rules.onClick.AddListener(() => { OpenRulesOnReturn = true; Home(); });
        }

        private void Update()
        {
            if (step >= 0) return;
            if (turn == null) turn = FindObjectOfType<TurnController>();
            if (turn == null || turn.GameState == null) return;
            if (turn.IsDealInProgress) sawDeal = true;
            if (!sawDeal || turn.IsDealInProgress || turn.IsAccusoWindowOpen || !turn.IsHumanPlayerTurn) return;
            cards = FindObjectOfType<CardViewManager>();
            coach.gameObject.SetActive(true);
            UIAnim.FadeIn(coach, 0.3f);
            Show(0);
            PulseHalo();
            if (UIAnim.DecorativeLoops)
                DOTween.Sequence()
                    .Join(fingerGlyph.DOAnchorPos(new Vector2(4f, -6f), 0.55f))
                    .Join(fingerGlyph.DOScale(0.92f, 0.55f))
                    .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(fingerGlyph.gameObject);
        }

        private void Show(int i)
        {
            if (i < 0) return;
            if (i >= Titles.Length) { Finish(); return; }
            bool first = step < 0;
            step = i;

            var area = Target(i, out var tip, out float radius);
            MoveSpot(area, radius, first);
            finger.gameObject.SetActive(tip.HasValue);
            if (tip.HasValue) finger.anchoredPosition = tip.Value + new Vector2(-28f, 10f); // punta del dito a (18, 0) del glifo, 10 di margine

            cap.text = $"NONNA ROSA · {i + 1} DI {Titles.Length}";
            title.text = Titles[i];
            text.text = Texts[i];
            nextLabel.text = i == Titles.Length - 1 ? "FINE" : "AVANTI";
            back.gameObject.SetActive(i > 0);
            for (int k = 0; k < dots.Length; k++)
            {
                dots[k].rectTransform.sizeDelta = new Vector2(k == i ? 20f : 6f, 6f);
                dots[k].color = k <= i ? UI51Tokens.Gold : UI51Tokens.CreamA(0.3f);
            }

            // Fumetto sotto al buco se sta in alto, sopra se sta in basso; mai sopra alla barra coi pallini.
            LayoutRebuilder.ForceRebuildLayoutImmediate(bubble);
            float h = bubble.rect.height, top = safe.rect.yMax - TopLimit;
            float above = tip.HasValue ? Mathf.Max(area.yMax, tip.Value.y) : area.yMax; // il dito sopra al buco resta scoperto
            float y = area.center.y > 0f ? area.yMin - Gap : above + Gap + h;
            bubble.anchoredPosition = new Vector2(bubble.anchoredPosition.x, Mathf.Clamp(y, safe.rect.yMin + 16f + h, top));
            UIAnim.PopDialog(bubble);
        }

        /// <summary>Rettangolo da illuminare nelle coordinate di Safe (y in su), punta del dito e raggio.</summary>
        private Rect Target(int i, out Vector2? tip, out float radius)
        {
            var state = turn.GameState;
            var hand = state.Players[GameModeService.Current.LocalPlayerIndex].Hand;
            tip = null;
            radius = 22f;
            switch (i)
            {
                case 0:
                    radius = 18f;
                    tip = CardTip(hand.Find(c => c.IsAce) ?? hand[0]);
                    return Grow(Cards(hand), 12f);
                case 1:
                    tip = CardTip(state.Table.Find(c => c.IsAce));
                    return Grow(Union(Cards(state.Table), Cards(hand)), 14f);
                case 2:
                    tip = CardTip(hand.Find(c => c.Rank == 7));
                    return Grow(Union(Cards(state.Table), Cards(hand)), 14f);
                case 3:
                    return Grow(Cards(state.Table), 16f);
                case 4:
                {
                    var r = Grow(UiRect(accuso), 8f);
                    radius = r.height * 0.5f;
                    tip = new Vector2(r.xMin - 2f, r.yMax + 29f); // mockup: dito a (300, 740), bottone a (320, 769)
                    return r;
                }
                default:
                {
                    var r = Grow(UiRect(scorePill), 4f);
                    radius = r.height * 0.5f;
                    return r;
                }
            }
        }

        private Vector2? CardTip(Card card)
        {
            if (card == null || cards == null || !cards.TryGetCardView(card, out var view)) return null;
            var r = World(view.CardRenderer.bounds);
            return new Vector2(r.center.x, r.yMax - r.height * 0.3f);
        }

        private Rect Cards(System.Collections.Generic.List<Card> list)
        {
            Rect? all = null;
            foreach (var c in list)
                if (cards != null && cards.TryGetCardView(c, out var view))
                    all = all.HasValue ? Union(all.Value, World(view.CardRenderer.bounds)) : World(view.CardRenderer.bounds);
            return all ?? new Rect(-150f, -300f, 300f, 180f);
        }

        private Rect World(Bounds b)
        {
            var cam = Camera.main;
            Vector2 a = Local(cam.WorldToScreenPoint(b.min)), c = Local(cam.WorldToScreenPoint(b.max));
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
        }

        private Rect UiRect(RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners); // tela in overlay: angoli in pixel dello schermo
            Vector2 a = Local(corners[0]), c = Local(corners[2]);
            return Rect.MinMaxRect(a.x, a.y, c.x, c.y);
        }

        private Vector2 Local(Vector3 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, screen, null, out var p);
            return p;
        }

        private static Rect Union(Rect a, Rect b) =>
            Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));

        private static Rect Grow(Rect r, float pad) => new Rect(r.x - pad, r.y - pad, r.width + 2f * pad, r.height + 2f * pad);

        /// <summary>Il buco scivola al passo nuovo (.35 s, cubic-bezier .2 .8 .3 1 come il mockup).</summary>
        private void MoveSpot(Rect to, float radius, bool instant)
        {
            DOTween.Kill(spot);
            if (instant) { SetSpot(to, radius); return; }
            Rect from = spotRect;
            float fromRadius = spotRadius;
            DOVirtual.Float(0f, 1f, 0.35f, f => SetSpot(
                    Rect.MinMaxRect(Mathf.Lerp(from.xMin, to.xMin, f), Mathf.Lerp(from.yMin, to.yMin, f),
                        Mathf.Lerp(from.xMax, to.xMax, f), Mathf.Lerp(from.yMax, to.yMax, f)),
                    Mathf.Lerp(fromRadius, radius, f)))
                .SetEase(UIEase.Sheet.Ease).SetTarget(spot).SetLink(spot.gameObject);
        }

        private void SetSpot(Rect r, float radius)
        {
            spotRect = r;
            spotRadius = radius;
            var rt = spot.rectTransform;
            rt.anchoredPosition = r.center;
            rt.sizeDelta = r.size + Vector2.one * (2f * Hole);
            spot.radius = radius + Hole;
            ring.rectTransform.sizeDelta = r.size + Vector2.one * 6f; // .ring: inset -3, bordo 2
            ring.radius = radius + 3f;
        }

        /// <summary>ringP del mockup: alone d'oro che si allarga da 0 a 8 e svanisce, 1.6 s. Spento con la grafica ridotta.</summary>
        private void PulseHalo()
        {
            halo.gameObject.SetActive(UIAnim.DecorativeLoops);
            if (!UIAnim.DecorativeLoops) return;
            var rt = halo.rectTransform;
            DOVirtual.Float(0f, 1f, 0.8f, f =>
                {
                    float s = 8f * f;
                    rt.offsetMin = -Vector2.one * s;
                    rt.offsetMax = Vector2.one * s;
                    halo.borderWidth = s;
                    halo.radius = spotRadius + 3f + s;
                    halo.color = new Color(1f, 1f, 1f, 0.7f * (1f - f));
                })
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetTarget(halo).SetLink(halo.gameObject);
        }

        private void SetSkip(bool open)
        {
            skipDialog.gameObject.SetActive(open);
            if (!open) return;
            UIAnim.FadeIn(skipDialog, 0.2f);
            UIAnim.PopDialog(skipCard);
        }

        private void Finish()
        {
            coach.gameObject.SetActive(false);
            done.gameObject.SetActive(true);
            UIAnim.FadeIn(done, 0.25f);
            // @keyframes burst (1 s): scala .3 -> 1.2, alpha 0 -> .9 (40%) -> .6.
            new UIKeyframes(1f, UIEase.EaseOut).Track(AnimProp.Scale, 0f, 0.3f, 1f, 1.2f).Track(AnimProp.Alpha, 0f, 0f, 0.4f, 0.9f, 1f, 0.6f)
                .Play(doneBurst);
            UIAnim.PopDialog(doneHead);
        }

        private static void Home() => AppFlowManager.LeaveGameAndGoToMenu();
    }
}
