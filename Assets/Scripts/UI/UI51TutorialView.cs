using DG.Tweening;
using Project51.Core;
using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 12, B33 (scelte dell'utente 07/10): partita guidata corta sopra al tavolo vero di un allenamento 1v1, col copione
    /// di TutorialScript (mazzo ridotto, bot scriptato, totali di partenza). Nonna Rosa accompagna ogni passo illuminando carte,
    /// bottone ACCUSA, punteggio o risultati: nei passi da leggere si va avanti con AVANTI e il gioco aspetta (TutorialHold); nelle
    /// mosse guidate si tocca la carta attraverso il buco del velo e passa solo la mossa del copione (TutorialGate). Le ultime due
    /// carte sono libere. A fine partita la schermata finale (premio B32) con GIOCA ORA e VAI ALLA HOME. Statistiche e ricompense di partita non contano.
    /// Grafica: UI51RulesBuilder (Tools/UI51/Build Fase 12).
    /// </summary>
    public sealed class UI51TutorialView : MonoBehaviour, ICanvasRaycastFilter
    {
        [SerializeField] private RectTransform safe, coach, skipDialog, skipCard, done, doneBurst, doneHead, reward;
        [Tooltip("Velo col buco: bordo largo Hole intorno al rettangolo illuminato.")]
        [SerializeField] private UI51Shape spot, ring, halo;
        [SerializeField] private RectTransform finger, fingerGlyph, bubble;
        [SerializeField] private UI51Shape[] dots = new UI51Shape[0];
        [SerializeField] private TMP_Text cap, title, text, nextLabel;
        [SerializeField] private Button next, back, skip, skipContinue, skipScrim, skipHome, play, home;
        [SerializeField] private RectTransform scorePill, accuso;

        /// <summary>Larghezza del velo intorno al buco (box-shadow 0 0 0 2000px del mockup).</summary>
        public const float Hole = 3000f;
        const float Gap = 24f, TopLimit = 104f;

        enum Kind { Read, Move, Accuso, Free }
        enum When { Turn, AccusoWindow, Results }
        enum Area { Score, Hand, Board, Opponent, Deck, Accuso, Results }

        sealed class Beat
        {
            public string Title, Text;
            public Kind Kind;
            public When When;
            public Area Area;
            public int Round = 1;
            public TutorialScript.Step Move;
        }

        static Beat Read(string t, string x, Area a, When w = When.Turn, int round = 1) => new Beat { Title = t, Text = x, Kind = Kind.Read, Area = a, When = w, Round = round };
        static Beat Play(string t, string x, TutorialScript.Step m, int round = 1) => new Beat { Title = t, Text = x, Kind = Kind.Move, Area = Area.Board, Move = m, Round = round };

        static readonly Beat[] Beats =
        {
            Read("Partita veloce", "Due smazzate corte con poche carte. Si gioca a 51: tu parti da 45, il bot da 49. Vince chi supera 51.", Area.Score),
            // Build 3 #46: avversario, mazziere, mazzo e carte rimaste, timer e matta (tre passi in piu', il resto nei testi che c'erano).
            Read("Avversario e mazziere", "In alto c’è il tuo avversario. La M indica il mazziere: distribuisce e gioca per ultimo. Il primo si sorteggia, poi si cambia a ogni smazzata.",
                Area.Opponent),
            Read("Il mazzo", "Tocca il mazzo quando vuoi: il numero dice quante carte sono rimaste. Giocate le 3 carte in mano, il mazziere ne dà altre 3 a testa.",
                Area.Deck),
            Read("Le tue carte", "Queste sono le tue 3 carte: a ogni turno ne giochi una toccandola. Online hai 30 secondi, poi la carta la sceglie il gioco.",
                Area.Hand),
            Play("Prendi con la carta uguale", "Tocca l’Asso di denari: prende l’Asso di bastoni. Le carte prese vanno nel tuo mazzetto e valgono punti a fine smazzata.",
                TutorialScript.Moves1[0]),
            Play("Prendi per somma", "Sul tavolo non c’è un altro 7, ma 3 + 4 fa 7: tocca il 7 e prendi le due carte. Se c’è la carta uguale invece devi prendere quella.",
                TutorialScript.Moves1[2]),
            Play("La regola del 15", "La carta che giochi più quelle che prendi possono fare 15: il 5 prende il Re (5 + 10).", TutorialScript.Moves1[4]),
            Read("Scopa!", "Il bot ha svuotato il tavolo: è una scopa, 1 punto. E ha preso il 7 di denari, il settebello: 1 punto anche quello.",
                Area.Opponent, When.AccusoWindow),
            new Beat
            {
                Title = "Accusa", Kind = Kind.Accuso, When = When.AccusoWindow, Area = Area.Accuso,
                Text = "Le nuove carte fanno 2 + 1 + 3 = 6: con 3 carte che sommano 9 o meno hai l’Accuso (3 punti), con 3 carte uguali il Decino (10 punti). Premi ACCUSA.",
            },
            Read("La matta", "Il 7 di coppe è la matta: ha il bordo viola. Quando la giochi vale 7, negli accusi fa da jolly.", Area.Hand),
            Play("Posa una carta", "Il tavolo è vuoto: posa il 3. L’asso tienilo per dopo.", TutorialScript.Moves1[6]),
            Play("Posa ancora", "Il 2 non prende niente: posalo.", TutorialScript.Moves1[8]),
            Play("L’asso piglia tutto", "Senza assi sul tavolo l’asso prende tutte le carte, ed è scopa anche questa. Tocca l’Asso di coppe.", TutorialScript.Moves1[10]),
            Read("51 esatti: si torna a 0", "Il bot ha chiuso la smazzata con 51 esatti, quindi riparte da 0: per vincere bisogna superare 51. Tu sei a 50.",
                Area.Results, When.Results),
            Read("Ultima smazzata", "Ora il mazziere sei tu e il bot è di mano. Ti basta poco per superare 51.", Area.Score, round: 2),
            Play("Scopa col 15", "5 + 7 + Asso + 2 fa 15: prendi tutto il tavolo, è scopa. E con il 7 di denari hai anche il settebello.", TutorialScript.Moves2[1], 2),
            new Beat
            {
                Title = "Tocca a te", Kind = Kind.Free, Area = Area.Hand, Round = 2,
                Text = "Le ultime due carte giocale come vuoi. A fine smazzata contano carte, denari, settebello, primiera, grande, piccola, scope e accusi.",
            },
        };

        /// <summary>B33: partita guidata in corso (fino alla schermata finale): niente statistiche, ricompense di partita, abbandoni.</summary>
        public static bool Running { get; private set; }

        /// <summary>Nonna Rosa sta spiegando: il gioco (Accuso, bot, risultati) aspetta AVANTI o il tocco su ACCUSA.</summary>
        public static bool Holding => current != null && current.showing && Beats[current.beat].Kind != Kind.Move;

        private static UI51TutorialView current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ClearHooks();
        }

        private static void ClearHooks()
        {
            Running = false;
            current = null;
            Rules51.ScriptedDeck = null;
            TurnController.TutorialGate = null;
            TurnController.TutorialBotMove = null;
            TurnController.TutorialHold = null;
        }

        /// <summary>Allenamento 1v1 col bot facile e le regole del 1v1 del selettore delle modalità, col copione del tutorial.</summary>
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
        private Project51.UIV2.Core.MatchResultsV2 results;
        private bool sawDeal, showing, finished;
        private int beat;
        private float doneAt = -1f, trackAt;
        private Rect spotRect;
        private float spotRadius;

        private void Awake()
        {
            // Awake gira prima di GameSceneInitializer.Start, che consuma il flag.
            if (!GameSceneInitializer.Tutorial) { gameObject.SetActive(false); return; }
            current = this;
            Running = true;
            TurnController.TutorialGate = Allow;
            TurnController.TutorialBotMove = valid => turn != null && turn.GameState != null
                ? TutorialScript.BotMove(turn.GameState.RoundIndex <= 1 ? TutorialScript.Moves1 : TutorialScript.Moves2, valid) : null;
            TurnController.TutorialHold = () => Holding;
            coach.gameObject.SetActive(false);
            skipDialog.gameObject.SetActive(false);
            done.gameObject.SetActive(false);
            reward.gameObject.SetActive(false); // la scheda "+200" si accende solo se il server paga
            back.gameObject.SetActive(false); // i passi seguono la partita: non si torna indietro
            next.onClick.AddListener(Advance);
            skip.onClick.AddListener(() => SetSkip(true));
            skipContinue.onClick.AddListener(() => SetSkip(false));
            skipScrim.onClick.AddListener(() => SetSkip(false));
            skipHome.onClick.AddListener(Home);
            play.onClick.AddListener(PlayFirstMatch);
            home.onClick.AddListener(Home);
        }

        private void OnDestroy()
        {
            if (current == this) ClearHooks();
        }

        private void Update()
        {
            if (turn == null) turn = FindObjectOfType<TurnController>();
            if (turn == null || turn.GameState == null || finished) return;
            if (turn.IsDealInProgress) sawDeal = true;
            if (results == null) results = FindObjectOfType<Project51.UIV2.Core.MatchResultsV2>();

            if (showing)
            {
                // L'Accuso premuto (o la mano finita senza) chiude il passo.
                if (Beats[beat].Kind == Kind.Accuso && (turn.GameState.Players[0].RoundAccusiCount > 0 || !turn.IsAccusoWindowOpen)) Advance();
                else Track();
                return;
            }
            // Partita finita: dopo un attimo (l'ultima presa) la schermata finale, senza i risultati sotto (Test 8, terzo giro 08/10).
            // Anche con un passo mai arrivato (giro Android 08/10: su un telefono lento un passo puo' saltare), se no niente schermata
            // finale e niente premio.
            bool over = results != null && results.MatchOver;
            if (beat < Beats.Length && !over) { if (Ready(Beats[beat])) Show(); return; }
            if (!over) return;
            if (doneAt < 0f) doneAt = Time.unscaledTime + 1.2f;
            else if (Time.unscaledTime >= doneAt) Finish();
        }

        private bool Ready(Beat b)
        {
            var state = turn.GameState;
            if (!sawDeal || state.RoundIndex != b.Round) return false;
            switch (b.When)
            {
                case When.AccusoWindow: return turn.IsAccusoWindowOpen && state.Players[0].Hand.Count == 3 && state.Deck.Count == 0;
                case When.Results:
                    return state.RoundEnded && results != null && results.RoundPanel.activeInHierarchy
                        && results.RoundPanel.GetComponent<CanvasGroup>().alpha > 0.99f;
                default:
                    return !state.RoundEnded && !turn.IsDealInProgress && !turn.IsAccusoWindowOpen && turn.IsHumanPlayerTurn && !turn.IsBusy
                        && (b.Move == null || state.Players[0].Hand.Contains(b.Move.Card));
            }
        }

        /// <summary>Mossa del giocatore: passa solo quella del passo guidato (dalle ultime due carte in poi tutto).</summary>
        private bool Allow(Move move)
        {
            if (beat >= Beats.Length) return true;
            var b = Beats[beat];
            if (showing && b.Kind == Kind.Move && b.Move.Matches(move)) { Advance(); return true; }
            GameAudio.PlayUi(SoundId.UiError);
            return false;
        }

        private void Advance()
        {
            if (!showing) return;
            showing = false;
            beat++;
            if (beat == 1) Rules51.ScriptedDeck = TutorialScript.Deck2(); // la prima smazzata e' distribuita: alla prossima il mazzo della seconda
            DOTween.Kill(coach);
            coach.gameObject.SetActive(false);
        }

        private void Show()
        {
            bool first = !coach.gameObject.activeSelf;
            showing = true;
            var b = Beats[beat];
            if (first)
            {
                cards = FindObjectOfType<CardViewManager>();
                coach.gameObject.SetActive(true);
                UIAnim.FadeIn(coach, 0.3f);
                if (beat == 0) StartLoops();
            }

            var area = Target(b, out var tip, out float radius);
            MoveSpot(area, radius, first);
            finger.gameObject.SetActive(tip.HasValue);
            if (tip.HasValue) finger.anchoredPosition = tip.Value + FingerOffset;

            cap.text = $"NONNA ROSA · {beat + 1} DI {Beats.Length}";
            title.text = b.Title;
            text.text = b.Text;
            next.transform.parent.gameObject.SetActive(b.Kind == Kind.Read || b.Kind == Kind.Free); // Footer: senza bottone niente riga vuota
            nextLabel.text = b.Kind == Kind.Free ? "GIOCA" : "AVANTI";
            int lit = Mathf.FloorToInt((float)beat * dots.Length / Beats.Length);
            for (int k = 0; k < dots.Length; k++)
            {
                dots[k].rectTransform.sizeDelta = new Vector2(k == lit ? 20f : 6f, 6f);
                dots[k].color = k <= lit ? UI51Tokens.Gold : UI51Tokens.CreamA(0.3f);
            }

            // Fumetto sotto al buco se sta in alto, sopra se sta in basso; mai sopra alla barra coi pallini.
            LayoutRebuilder.ForceRebuildLayoutImmediate(bubble);
            float h = bubble.rect.height, top = safe.rect.yMax - TopLimit;
            float above = tip.HasValue ? Mathf.Max(area.yMax, tip.Value.y) : area.yMax; // il dito sopra al buco resta scoperto
            float y = area.center.y > 0f ? area.yMin - Gap : above + Gap + h;
            bubble.anchoredPosition = new Vector2(bubble.anchoredPosition.x, Mathf.Clamp(y, safe.rect.yMin + 16f + h, top));
            UIAnim.PopDialog(bubble);
        }

        static readonly Vector2 FingerOffset = new Vector2(-28f, 10f); // punta del dito a (28, -9) del riquadro 60x66 (pivot in alto a sinistra)

        /// <summary>
        /// Giro Android 08/10 (dito non sul bottone ACCUSA su un tablet): buco e dito seguono il bersaglio finche' il passo e' aperto.
        /// Prima si misuravano solo all'apertura, a volte prima che il tavolo finisse di sistemarsi. Il mazzo no: misurarlo lo apre.
        /// </summary>
        private void Track()
        {
            var b = Beats[beat];
            if (b.Area == Area.Deck || DOTween.IsTweening(spot) || Time.unscaledTime < trackAt) return;
            trackAt = Time.unscaledTime + 0.2f; // ponytail: 5 misure al secondo bastano (telefoni lenti), ogni frame se mai servisse
            var area = Target(b, out var tip, out float radius);
            if ((area.min - spotRect.min).sqrMagnitude < 1f && (area.max - spotRect.max).sqrMagnitude < 1f) return;
            SetSpot(area, radius);
            if (tip.HasValue) finger.anchoredPosition = tip.Value + FingerOffset;
        }

        private void StartLoops()
        {
            PulseHalo();
            if (UIAnim.DecorativeLoops)
                DOTween.Sequence()
                    .Join(fingerGlyph.DOAnchorPos(new Vector2(4f, -6f), 0.55f))
                    .Join(fingerGlyph.DOScale(0.92f, 0.55f))
                    .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(fingerGlyph.gameObject);
        }

        /// <summary>Nelle mosse guidate e su ACCUSA i tocchi dentro al buco passano al tavolo e al bottone sotto al velo.</summary>
        public bool IsRaycastLocationValid(Vector2 screen, Camera eventCamera)
        {
            if (!showing || Beats[beat].Kind == Kind.Read || Beats[beat].Kind == Kind.Free || skipDialog.gameObject.activeSelf) return true;
            return !spotRect.Contains(Local(screen));
        }

        /// <summary>Rettangolo da illuminare nelle coordinate di Safe (y in su), punta del dito e raggio.</summary>
        private Rect Target(Beat b, out Vector2? tip, out float radius)
        {
            var state = turn.GameState;
            var hand = state.Players[GameModeService.Current.LocalPlayerIndex].Hand;
            tip = b.Move != null ? CardTip(b.Move.Card) : null;
            radius = 22f;
            switch (b.Area)
            {
                case Area.Hand:
                    radius = 18f;
                    if (b.Kind == Kind.Read) tip = CardTip(hand.Count > 0 ? hand[0] : null);
                    return Grow(Cards(hand), 12f);
                case Area.Board:
                    return Grow(state.Table.Count > 0 ? Union(Cards(state.Table), Cards(hand)) : Cards(hand), 14f);
                case Area.Opponent:
                {
                    // Banner del bot con le sue scope (UI51TableBuilder: Banner_Top/UI51Banner/Scope).
                    var banner = GameObject.Find("Banner_Top")?.transform.Find("UI51Banner") as RectTransform;
                    if (banner == null) return Grow(Cards(state.Table), 16f);
                    var r = UiRect(banner);
                    if (banner.Find("Scope") is RectTransform scope) r = Union(r, UiRect(scope));
                    if (banner.Find("Dealer") is RectTransform dealer && dealer.gameObject.activeInHierarchy) r = Union(r, UiRect(dealer)); // la M
                    return Grow(r, 10f);
                }
                case Area.Deck:
                {
                    // Mazzo col medaglione delle carte rimaste (UI51TableBuilder: TableDeckView, figlio Count), che si apre come al tocco.
                    // E' una tela nel mondo: angoli in unita' mondo, dalla camera allo schermo.
                    var deck = FindObjectOfType<TableDeckView>();
                    if (deck == null) return Grow(Cards(state.Table), 16f);
                    deck.Tap();
                    var rt = (RectTransform)deck.transform;
                    var r = WorldUiRect(rt);
                    if (rt.Find("Count") is RectTransform medal) r = Union(r, WorldUiRect(medal));
                    radius = 16f;
                    return Grow(r, 10f);
                }
                case Area.Accuso:
                {
                    // Bottone e carte in mano: l'accuso si conta sulle carte.
                    // Secondo giro 08/10: il glifo e' una mano che punta in su, la punta va DENTRO il bottone (in basso a sinistra, la
                    // mano scende sotto senza coprire la scritta). Prima stava 29 sopra e a sinistra del bordo, come nel mockup che
                    // aveva un dito in diagonale: col glifo dritto indicava il vuoto.
                    var rest = RestUiRect(accuso);
                    tip = new Vector2(rest.xMin + rest.width * 0.35f, rest.yMin + rest.height * 0.3f);
                    return Grow(Union(Grow(rest, 8f), Cards(hand)), 6f);
                }
                case Area.Results:
                    return Grow(UiRect(results.View.Race), 6f);
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

        private Rect WorldUiRect(RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var b = new Bounds(corners[0], Vector3.zero);
            b.Encapsulate(corners[2]);
            return World(b);
        }

        private Rect UiRect(RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners); // tela in overlay: angoli in pixel dello schermo
            Vector2 a = Local(corners[0]), c = Local(corners[2]);
            return Rect.MinMaxRect(a.x, a.y, c.x, c.y);
        }

        /// <summary>
        /// ACCUSA pulsa durante la finestra (scala e rotazione sul bottone, TableActionButtonsController): il rettangolo a riposo, cosi'
        /// buco e dito non dipendono dall'istante in cui si misura.
        /// </summary>
        private Rect RestUiRect(RectTransform target)
        {
            var chain = new[] { target, target.parent };
            var scales = new Vector3[2];
            var turns = new Quaternion[2];
            for (int i = 0; i < 2; i++)
            {
                if (chain[i] == null) continue;
                scales[i] = chain[i].localScale; turns[i] = chain[i].localRotation;
                chain[i].localScale = Vector3.one; chain[i].localRotation = Quaternion.identity;
            }
            var r = UiRect(target);
            for (int i = 0; i < 2; i++)
                if (chain[i] != null) { chain[i].localScale = scales[i]; chain[i].localRotation = turns[i]; }
            return r;
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
            finished = true;
            ClearHooks(); // da qui si gioca davvero: la rivincita conta
            done.gameObject.SetActive(true);
            UIAnim.FadeIn(done, 0.25f);
            // @keyframes burst (1 s): scala .3 -> 1.2, alpha 0 -> .9 (40%) -> .6.
            new UIKeyframes(1f, UIEase.EaseOut).Track(AnimProp.Scale, 0f, 0.3f, 1f, 1.2f).Track(AnimProp.Alpha, 0f, 0f, 0.4f, 0.9f, 1f, 0.6f)
                .Play(doneBurst);
            UIAnim.PopDialog(doneHead);
            // B32 (TU3): premio una volta per account, deciso dal server; gli ospiti sanno come averlo.
            // Giro Android 08/10: ogni esito si vede; se la chiamata non arriva al server si riprova al ritorno in Home (RetryTutorial).
            Project51.Auth.RewardsService.ClaimTutorial(r =>
            {
                if (r.ok && this != null) { Project51.Auth.RewardsService.PlaySound(r); reward.gameObject.SetActive(true); UIAnim.PopDialog(reward); }
                else if (r.ospite) UI51Toast.Show("Registrati e rifai il tutorial per avere +200 monete", UI51Toast.Kind.Coins);
                else if (r.gia) UI51Toast.Show("Il premio del tutorial è già stato dato a questo account");
                else if (r.inConsegna) UI51Toast.Show("Premio del tutorial preso: le monete arrivano appena il server conferma");
                else UI51Toast.Show(TutorialRetryText, UI51Toast.Kind.Error);
            }, _ => UI51Toast.Show(TutorialRetryText, UI51Toast.Kind.Error));
        }

        const string TutorialRetryText = "Premio del tutorial non arrivato: riproviamo appena torni in Home";

        /// <summary>"GIOCA ORA": la rivincita dei risultati, ora una partita di allenamento vera. "VAI ALLA HOME" porta in Home.</summary>
        private void PlayFirstMatch()
        {
            gameObject.SetActive(false);
            if (results != null && results.MatchOver) results.Rematch.onClick.Invoke(); // anche a pannello spento: Next riparte
        }

        private static void Home() => AppFlowManager.LeaveGameAndGoToMenu();
    }
}
