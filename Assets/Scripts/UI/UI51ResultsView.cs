using System.Linq;
using DG.Tweening;
using Project51.Auth;
using Project51.Core;
using Project51.UI51;
using Project51.UIV2.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 7: fine smazzata (mockup FineSmazzata, 1v1 / 2v2 / 1v3) e fine partita (FinePartita: VITTORIA o SCONFITTA,
    /// sfida a due o classifica a quattro, XP, statistiche). Solo grafica: flusso, conto alla rovescia, XP e coriandoli restano
    /// in MatchResultsV2, che la chiama. Colonne: io (o la mia coppia) per primo, poi gli altri nell'ordine dei posti.
    /// Costruita da UI51TableBuilder.BuildResults.
    /// </summary>
    public sealed class UI51ResultsView : MonoBehaviour
    {
        const float Gap = 6f;

        [SerializeField] private PlayerBannerManager banners;
        [SerializeField] private CardViewManager cards;

        [Header("Fine smazzata")]
        [SerializeField] private GameObject round;
        [SerializeField] private TMP_Text roundMode;
        [SerializeField] private RectTransform[] heads = new RectTransform[0];        // 4
        [SerializeField] private AvatarFrame[] headAvatars = new AvatarFrame[0];      // 8: due per colonna (coppie)
        [SerializeField] private TMP_Text[] headNames = new TMP_Text[0];              // 4
        [SerializeField] private RectTransform[] rows = new RectTransform[0];         // 8
        [SerializeField] private Image[] rowIcons = new Image[0];                     // 8
        [SerializeField] private TMP_Text[] rowDetails = new TMP_Text[0];             // 8
        [SerializeField] private RectTransform[] cells = new RectTransform[0];        // 8 x 4 punti + 4 totali
        [SerializeField] private RectTransform[] pills = new RectTransform[0];        // 32
        [SerializeField] private TMP_Text[] pillTexts = new TMP_Text[0];              // 32
        [SerializeField] private GameObject[] dashes = new GameObject[0];             // 32
        [SerializeField] private TMP_Text[] handTotals = new TMP_Text[0];             // 4
        [SerializeField] private RectTransform race;
        [SerializeField] private TMP_Text raceTitle, raceNote;
        [SerializeField] private RectTransform[] raceRows = new RectTransform[0];     // 4
        [SerializeField] private TMP_Text[] raceNames = new TMP_Text[0], raceTotals = new TMP_Text[0];
        [SerializeField] private RectTransform[] raceFills = new RectTransform[0];
        [SerializeField] private RectTransform roundFooter;
        [SerializeField] private TMP_Text roundCaption;

        [Header("Fine partita")]
        [SerializeField] private GameObject match;
        [SerializeField] private RectTransform rays;
        [SerializeField] private Image raysImage;
        [SerializeField] private RectTransform title;
        [SerializeField] private TMP_Text matchMode, titleText;
        [SerializeField] private Image ribbon;
        [SerializeField] private Sprite ribbonGold, ribbonGray;
        [SerializeField] private RectTransform duel;
        [SerializeField] private RectTransform[] sides = new RectTransform[0];        // 2
        [SerializeField] private AvatarFrame[] sideAvatars = new AvatarFrame[0];      // 4: due per lato
        [SerializeField] private GameObject[] crowns = new GameObject[0], glows = new GameObject[0];
        [SerializeField] private TMP_Text[] sideNames = new TMP_Text[0], sideScores = new TMP_Text[0];
        [SerializeField] private RectTransform rank;
        [SerializeField] private UI51Shape[] medals = new UI51Shape[0];               // 4
        [SerializeField] private TMP_Text[] rankPos = new TMP_Text[0], rankNames = new TMP_Text[0], rankScores = new TMP_Text[0];
        [SerializeField] private AvatarFrame[] rankAvatars = new AvatarFrame[0];
        [SerializeField] private RectTransform rewards;
        [SerializeField] private TMP_Text levelText, xpGain, xpText;
        [SerializeField] private GameObject coinsRow;
        [SerializeField] private TMP_Text coinsLabel;
        [SerializeField] private RectTransform xpTrack, xpFill;
        [SerializeField] private RectTransform stats;
        [SerializeField] private TMP_Text[] statValues = new TMP_Text[0];             // Scope, Accusi, Settebelli
        [SerializeField] private RectTransform matchFooter;

        // Spareggio (mockup MomentiPartita, momento "spareggio"): dialog sopra al fine smazzata.
        [SerializeField] private RectTransform tie, tieCard;
        [SerializeField] private TMP_Text tieTitle, tieText;
        [SerializeField] private HorizontalLayoutGroup tieRow;
        [SerializeField] private RectTransform[] tieSides = new RectTransform[0];     // 4
        [SerializeField] private AvatarFrame[] tieAvatars = new AvatarFrame[0];       // 8: due per lato (coppie)
        [SerializeField] private TMP_Text[] tieScores = new TMP_Text[0];             // 4
        [SerializeField] private GameObject[] tieEquals = new GameObject[0];          // 3
        [SerializeField] private Button tieGo, tieScrim, roundNext;

        // Vittoria immediata (mockup Cappotto / TreAssi): al posto di nastro, sfida e statistiche.
        [SerializeField] private RectTransform instant, instantBurst, instantHead, instantDuo, instantGlow, instantAces, coinRain;
        [SerializeField] private TMP_Text instantCap, instantTitle, instantShade, instantSub;
        [SerializeField] private AvatarFrame[] instantAvatars = new AvatarFrame[0];   // 2
        [SerializeField] private RectTransform[] aceCards = new RectTransform[0];     // 3
        [SerializeField] private Image[] aceImages = new Image[0];                    // 3
        [SerializeField] private RectTransform[] coins = new RectTransform[0];
        [SerializeField] private Vector3[] coinTimes = new Vector3[0];                // durata, ritardo, gradi di rotazione

        /// <summary>Fine partita chiusa da cappotto o tre assi e vinta: niente coriandoli, piovono monete.</summary>
        public bool Instant { get; private set; }
        private bool tiePending;
        static readonly float[] AceAngles = { -14f, 0f, 14f };
        static readonly CubicBezier AceIn = new CubicBezier(0.3f, 0.7f, 0.3f, 1.2f);

        private int columns;
        private bool rankMode;
        private float[] raceFrom = new float[4], raceTo = new float[4];

        static readonly Color Dim = new Color(0.72f, 0.72f, 0.74f, 1f); // grayscale(.5) brightness(.8) del mockup, a occhio
        static readonly Gradient[] MedalFills =
        {
            UI51Shape.Linear((UI51Tokens.GoldLight, 0f), (UI51Tokens.GoldDark, 1f)),
            UI51Shape.Linear((UI51Tokens.Hex("#F2F2F2"), 0f), (UI51Tokens.Hex("#9AA3AF"), 1f)),
            UI51Shape.Linear((UI51Tokens.Hex("#E8B07A"), 0f), (UI51Tokens.Hex("#9A5B2A"), 1f)),
            UI51Shape.Solid(UI51Tokens.CreamA(0.25f)),
        };

        /// <summary>Concorrenti in ordine di colonna: il mio per primo, poi gli altri nell'ordine dei posti.</summary>
        public static int[] Columns(GameState state, int localEntry)
        {
            int n = MatchScore.EntryCount(state);
            return Enumerable.Range(0, n).Select(i => (localEntry + i) % n).ToArray();
        }

        /// <summary>Riga sopra il nastro al posto del modo (vittoria per abbandono).</summary>
        public void SetModeCaption(string text) => matchMode.text = text;

        public static string ModeLabel(GameState state) =>
            state.TeamMode ? "PARTITA 2 VS 2" : state.NumPlayers == 4 ? "TUTTI CONTRO TUTTI" : "PARTITA 1 VS 1";

        /// <summary>Centro della colonna j di n dal bordo destro, griglia "1fr + n x w" (passo 6): regge anche le aree sicure piu' larghe di 390.</summary>
        static float ColumnX(int j, int n)
        {
            float w = n == 4 ? 50f : 92f;
            return -((n - 1 - j) * (w + Gap) + w * 0.5f);
        }

        // ------------------------------------------------------------------ fine smazzata

        private void Awake()
        {
            if (tieScrim != null) tieScrim.onClick.AddListener(CloseTie);
            if (tieGo != null) tieGo.onClick.AddListener(() =>
            {
                CloseTie();
                if (roundNext != null && roundNext.interactable) roundNext.onClick.Invoke(); // chi non e' l'host aspetta e basta
            });
        }

        public void BindRound(GameState state, int target, int localEntry)
        {
            match.SetActive(false);
            round.SetActive(true);
            if (tie != null) tie.gameObject.SetActive(false);
            var order = Columns(state, localEntry);
            int n = columns = order.Length;
            var b = PunteggioManager.CalculateBreakdown(state);
            var byColumn = order.Select(e => b[e]).ToArray(); // "22 – 18" nell'ordine delle colonne (io per primo), non dei posti
            var roundScores = MatchScore.RoundScores(state);
            var totals = MatchScore.Totals(state);
            roundMode.text = ModeLabel(state) + " · MANO " + state.RoundIndex;

            for (int j = 0; j < heads.Length; j++)
            {
                heads[j].gameObject.SetActive(j < n);
                if (j >= n) continue;
                int e = order[j];
                heads[j].anchoredPosition = new Vector2(ColumnX(j, n), heads[j].anchoredPosition.y);
                heads[j].sizeDelta = new Vector2(n == 4 ? 50f : 92f, heads[j].sizeDelta.y);
                headNames[j].text = state.TeamMode ? (j == 0 ? "Noi" : "Loro") : GameSocialV2.PlayerName(e);
                headNames[j].fontSize = n == 4 ? 10f : 11f;
                float size = n == 4 ? 34f : 40f;
                headNames[j].rectTransform.anchoredPosition = new Vector2(0f, -(size + 5f));
                BindAvatars(state, e, headAvatars, j, size, 10f, j == 0, -1f);
            }

            Sprite back = banners != null ? banners.MatchCardBack() : null;
            var icons = new Sprite[rows.Length];
            icons[0] = back;
            if (cards != null)
            {
                icons[2] = cards.GetSpriteForCard(new Card(Suit.Denari, 7));
                icons[6] = cards.GetSpriteForCard(new Card(Suit.Spade, 3));
            }
            float first = rows[0].rect.width - n * ((n == 4 ? 50f : 92f) + Gap); // colonna "1fr" con etichetta e dettaglio
            for (int r = 0; r < rows.Length; r++)
            {
                if (icons[r] != null) rowIcons[r].sprite = icons[r];
                var detail = rowDetails[r].rectTransform;
                detail.sizeDelta = new Vector2(first - detail.anchoredPosition.x, detail.sizeDelta.y);
                rowDetails[r].text = ResultsSheet.Detail(state, byColumn, r, GameSocialV2.PlayerName, k => EntryLabel(state, order[k], localEntry),
                    GameModeService.Current.LocalPlayerIndex);
                for (int j = 0; j < 4; j++)
                {
                    var cell = cells[r * 4 + j];
                    cell.gameObject.SetActive(j < n);
                    if (j >= n) continue;
                    PlaceCell(cell, j, n);
                    int pts = ResultsSheet.Points(b[order[j]], r);
                    pills[r * 4 + j].gameObject.SetActive(pts > 0);
                    dashes[r * 4 + j].SetActive(pts == 0);
                    if (pts > 0) SetPill(r * 4 + j, "+" + pts, n == 4 ? 30f : 38f);
                }
            }
            for (int j = 0; j < 4; j++)
            {
                var cell = cells[rows.Length * 4 + j];
                cell.gameObject.SetActive(j < n);
                if (j >= n) continue;
                PlaceCell(cell, j, n);
                int s = roundScores[order[j]];
                handTotals[j].text = MatchScore.IsCappotto(s) ? "Cappotto" : "+" + s;
                handTotals[j].fontSize = MatchScore.IsCappotto(s) ? 12f : n == 4 ? 16f : 20f;
            }

            // Corsa al traguardo: pannello a 600 (a quattro 588 e piu' fitto); barre da prima della smazzata a dopo.
            float gap = n == 4 ? 7f : 12f;
            race.anchoredPosition = new Vector2(race.anchoredPosition.x, -(n == 4 ? 588f : 600f));
            race.sizeDelta = new Vector2(race.sizeDelta.x, 12f + 15f + n * (gap + 18f) + 12f);
            raceTitle.text = "CORSA AL " + target;
            raceNote.text = ResultsSheet.RaceNote(order.Select(e => totals[e]).ToArray(), 0, target);
            for (int j = 0; j < raceRows.Length; j++)
            {
                raceRows[j].gameObject.SetActive(j < n);
                if (j >= n) continue;
                int e = order[j], after = totals[e], before = after - roundScores[e];
                raceRows[j].anchoredPosition = new Vector2(raceRows[j].anchoredPosition.x, -(12f + 15f + gap + j * (18f + gap)));
                raceNames[j].text = state.TeamMode ? (j == 0 ? "Noi" : "Loro") : GameSocialV2.PlayerName(e);
                bool cappotto = MatchScore.IsCappotto(after);
                raceTotals[j].text = (cappotto ? "Capp." : after.ToString()) + "<size=10><color=#F5E9D073> /" + target + "</color></size>";
                raceFrom[j] = Mathf.Clamp01((float)before / target);
                raceTo[j] = cappotto ? 1f : Mathf.Clamp01((float)after / target);
            }

            var leaders = MatchScore.Leaders(state);
            tiePending = tie != null && leaders.Length > 1 && totals[leaders[0]] >= target;
            if (tiePending) BindTie(state, order.Where(leaders.Contains).ToArray(), totals[leaders[0]], target, localEntry);
        }

        /// <summary>Parita' sopra al traguardo: avatar dei primi (i miei in oro) con "=" in mezzo, punti, testo e pulsante.</summary>
        void BindTie(GameState state, int[] leaders, int score, int target, int localEntry)
        {
            int n = Mathf.Min(leaders.Length, tieSides.Length);
            float size = n > 3 ? 48f : 62f; // in quattro a 62 non ci stanno
            tieRow.spacing = n > 3 ? 10f : 18f;
            tieTitle.text = "Parità a " + score + "!";
            string who = leaders.Contains(localEntry) ? "Avete superato "
                : string.Join(" e ", leaders.Select(e => EntryLabel(state, e, localEntry))) + " hanno superato ";
            tieText.text = who + target + " a pari punti: si gioca un’altra smazzata. Vince chi resta in testa da solo.";
            for (int j = 0; j < tieSides.Length; j++)
            {
                bool on = j < n;
                tieSides[j].gameObject.SetActive(on);
                if (j > 0 && j - 1 < tieEquals.Length) tieEquals[j - 1].SetActive(on);
                if (!on) continue;
                int e = leaders[j];
                tieSides[j].GetComponent<LayoutElement>().preferredWidth = MatchScore.MembersOf(state, e).Length > 1 ? size * 2f - 14f : size;
                BindAvatars(state, e, tieAvatars, j, size, 14f, e == localEntry, -1f);
                tieScores[j].rectTransform.anchoredPosition = new Vector2(0f, -(size + 6f));
                tieScores[j].text = score.ToString();
            }
        }

        void OpenTie()
        {
            if (!round.activeInHierarchy) return;
            tie.gameObject.SetActive(true);
            UIAnim.FadeIn(tie, 0.2f);
            UIAnim.Pop(tieCard, 0.8f, 1.04f, 0.3f); // .pop del mockup
        }

        void CloseTie()
        {
            if (tie != null) tie.gameObject.SetActive(false);
        }

        /// <summary>Riga sotto il pulsante ("Si riparte da sola tra N secondi"); vuota = nascosta.</summary>
        public void SetRoundCaption(string text)
        {
            if (roundCaption == null) return;
            roundCaption.gameObject.SetActive(!string.IsNullOrEmpty(text));
            if (roundCaption.text != text) roundCaption.text = text;
        }

        private void PlayRound()
        {
            UIAnim.RowsIn(rows, 0.18f, 0.15f);
            for (int r = 0; r < rows.Length; r++)
                for (int j = 0; j < columns; j++)
                    if (pills[r * 4 + j].gameObject.activeSelf) UIAnim.Pop(pills[r * 4 + j], 0.4f, 1.12f, 0.3f, 0.35f + r * 0.18f);
            for (int j = 0; j < columns; j++) UIAnim.FadeUp(cells[rows.Length * 4 + j], 1.8f, 10f, 0.4f);
            UIAnim.FadeUp(race, 1.8f, 10f, 0.4f);
            UIAnim.FadeUp(roundFooter, 1.8f, 10f, 0.4f);
            for (int j = 0; j < columns; j++) UIAnim.Fill(raceFills[j], raceFrom[j], raceTo[j]);
            // Lo spareggio arriva dopo le righe: prima si vede la smazzata, poi la parita'. Il conto alla rovescia continua.
            if (tiePending) DOVirtual.DelayedCall(1.2f, OpenTie).SetUpdate(true).SetLink(round, LinkBehaviour.KillOnDisable);
        }

        void PlaceCell(RectTransform cell, int j, int n) => cell.anchoredPosition = new Vector2(ColumnX(j, n), cell.anchoredPosition.y);

        void SetPill(int i, string text, float minWidth)
        {
            pillTexts[i].text = text;
            float w = Mathf.Max(minWidth, pillTexts[i].GetPreferredValues(text).x + 12f);
            pills[i].sizeDelta = new Vector2(w, pills[i].sizeDelta.y);
        }

        static string EntryLabel(GameState state, int entry, int localEntry) =>
            state.TeamMode ? (entry == localEntry ? "Noi" : "Loro") : GameSocialV2.PlayerName(entry);

        /// <summary>Avatar della colonna/lato slot (due per le coppie, accavallati di overlap): anello oro per me o la mia coppia, blu agli altri.</summary>
        void BindAvatars(GameState state, int entry, AvatarFrame[] pool, int slot, float size, float overlap, bool mine, float dimWhenNot)
        {
            var members = MatchScore.MembersOf(state, entry);
            for (int k = 0; k < 2; k++)
            {
                var avatar = pool[slot * 2 + k];
                bool on = k < members.Length;
                avatar.gameObject.SetActive(on);
                if (!on) continue;
                var rt = (RectTransform)avatar.transform;
                rt.sizeDelta = new Vector2(size, size);
                float x = members.Length > 1 ? (k == 0 ? -1f : 1f) * (size - overlap) * 0.5f : 0f;
                rt.anchoredPosition = new Vector2(x, -size * 0.5f);
                if (banners != null) avatar.SetAvatar(banners.SeatAvatar(members[k]));
                avatar.SetFrame(mine ? FrameStyle.Oro : FrameStyle.Blu);
                avatar.SetTint(dimWhenNot > 0f ? Dim : Color.white);
            }
        }

        // ------------------------------------------------------------------ fine partita

        public void BindMatch(GameState state, int localEntry, int winner, int scope, int accusi, int settebelli)
        {
            round.SetActive(false);
            match.SetActive(true);
            bool won = winner == localEntry;
            var order = Columns(state, localEntry);
            var totals = MatchScore.Totals(state);
            rankMode = order.Length > 2;
            // Cappotto e tre assi valgono CappottoScore: si distinguono dai tre assi ancora in mano.
            int treAssi = RoundManager.TreAssiHolder(state);
            string closed = treAssi >= 0 ? "TRE ASSI" : "CAPPOTTO";
            Instant = won && MatchScore.IsCappotto(totals[winner]) && instant != null;
            matchMode.text = ModeLabel(state);
            titleText.text = won ? "VITTORIA" : "SCONFITTA"; // anche a quattro (scelta utente 01/10, non "2° POSTO")
            ribbon.sprite = won ? ribbonGold : ribbonGray;
            raysImage.color = won ? UI51Tokens.WhiteA(0.16f) : new Color(0.55f, 0.62f, 0.75f, 0.08f); // mockup: oro .18 a opacita' .5
            rays.anchoredPosition = new Vector2(0f, Instant ? -300f : -170f);
            if (instant != null) instant.gameObject.SetActive(Instant);
            if (Instant) BindInstant(state, winner, treAssi);
            title.gameObject.SetActive(!Instant);
            stats.gameObject.SetActive(!Instant);

            duel.gameObject.SetActive(!rankMode && !Instant);
            rank.gameObject.SetActive(rankMode && !Instant);
            if (!rankMode)
            {
                for (int s = 0; s < 2; s++)
                {
                    int e = order[s];
                    bool win = e == winner;
                    float size = state.TeamMode ? 68f : 84f;
                    BindAvatars(state, e, sideAvatars, s, size, 16f, s == 0, win ? -1f : 1f);
                    crowns[s].SetActive(win);
                    glows[s].SetActive(win);
                    ((RectTransform)glows[s].transform).anchoredPosition = new Vector2(0f, -size * 0.5f);
                    ((RectTransform)crowns[s].transform).anchoredPosition = new Vector2(0f, 14f);
                    sideNames[s].rectTransform.anchoredPosition = new Vector2(0f, -(size + 6f));
                    sideScores[s].rectTransform.anchoredPosition = new Vector2(0f, -(size + 30f));
                    sideNames[s].text = state.TeamMode ? string.Join(" e ", MatchScore.MembersOf(state, e).Select(GameSocialV2.PlayerName)) : GameSocialV2.PlayerName(e);
                    bool cappotto = MatchScore.IsCappotto(totals[e]);
                    sideScores[s].text = cappotto ? closed : totals[e].ToString();
                    sideScores[s].fontSize = cappotto ? 20f : 32f;
                    sideScores[s].color = win ? UI51Tokens.GoldLight : UI51Tokens.CreamA(0.6f);
                }
            }
            else
            {
                var ranks = ResultsSheet.Ranks(totals);
                // Vittoria per abbandono: chi vince sta primo anche con meno punti, gli altri scalano.
                for (int e = 0; e < ranks.Length && ranks[winner] != 1; e++) if (e != winner && ranks[e] < ranks[winner]) ranks[e]++;
                ranks[winner] = 1;
                var byRank = Enumerable.Range(0, totals.Length).OrderBy(e => ranks[e]).ThenBy(e => System.Array.IndexOf(order, e)).ToArray();
                for (int i = 0; i < byRank.Length && i < medals.Length; i++)
                {
                    int e = byRank[i];
                    bool me = e == localEntry;
                    medals[i].fill = MedalFills[Mathf.Clamp(ranks[e] - 1, 0, 3)];
                    rankPos[i].text = ranks[e].ToString();
                    if (banners != null) rankAvatars[i].SetAvatar(banners.SeatAvatar(e));
                    rankAvatars[i].SetFrame(i == 0 ? FrameStyle.Oro : FrameStyle.Blu);
                    rankNames[i].text = GameSocialV2.PlayerName(e);
                    rankNames[i].color = me ? UI51Tokens.GoldLight : UI51Tokens.Cream;
                    rankNames[i].font = UI51Tokens.Font(me ? FontFace.NunitoExtraBold : FontFace.NunitoBold);
                    rankScores[i].text = MatchScore.IsCappotto(totals[e]) ? closed : totals[e].ToString();
                }
            }

            // Ricompense (XP, monete per chi ha un account: ShowXp ne fissa l'altezza), statistiche subito sotto.
            float top = Instant ? 470f : rankMode ? 392f : 372f;
            rewards.anchoredPosition = new Vector2(rewards.anchoredPosition.x, -top);
            stats.anchoredPosition = new Vector2(stats.anchoredPosition.x, -(top + rewards.sizeDelta.y + 14f));
            statValues[0].text = scope.ToString();
            statValues[1].text = accusi.ToString();
            statValues[2].text = settebelli.ToString();
        }

        /// <summary>Testa (VITTORIA IMMEDIATA, CAPPOTTO! o TRE ASSI!, riga sotto) e avatar 92 dei vincitori o i tre assi veri in mano.</summary>
        void BindInstant(GameState state, int winner, int treAssi)
        {
            bool aces = treAssi >= 0;
            instantCap.text = aces ? "VITTORIA IMMEDIATA" : ModeLabel(state) + " · VITTORIA IMMEDIATA";
            instantTitle.text = instantShade.text = aces ? "TRE ASSI!" : "CAPPOTTO!";
            var members = MatchScore.MembersOf(state, winner);
            if (aces)
                instantSub.text = treAssi == GameModeService.Current.LocalPlayerIndex ? "Hai ricevuto tre assi in mano: la partita è tua"
                    : GameSocialV2.PlayerName(treAssi) + " ha ricevuto tre assi in mano: la partita è vostra";
            else
                instantSub.text = members.Length > 1 ? "Tutti e 10 i denari presi da " + string.Join(" e ", members.Select(GameSocialV2.PlayerName))
                    : "Hai preso tutti e 10 i denari";
            instantAces.gameObject.SetActive(aces);
            instantDuo.gameObject.SetActive(!aces);
            if (aces)
            {
                var hand = state.Players[treAssi].Hand.Where(c => c.IsAce).ToArray();
                for (int i = 0; i < aceImages.Length && i < hand.Length; i++)
                    if (cards != null) aceImages[i].sprite = cards.GetSpriteForCard(hand[i]);
                return;
            }
            BindAvatars(state, winner, instantAvatars, 0, 92f, 14f, true, -1f);
            instantGlow.sizeDelta = new Vector2(members.Length > 1 ? 230f : 152f, 152f); // box-shadow 0 0 30 oro .6 attorno agli avatar
        }

        /// <summary>Entrate del mockup Cappotto: bagliore che scoppia, testa che fa pop, assi che cadono, monete a pioggia.</summary>
        private void PlayInstant()
        {
            // @keyframes burst (1 s): scala .2 -> 1.15, alpha 0 -> 1 (35%) -> .65.
            new UIKeyframes(1f, UIEase.EaseOut).Track(AnimProp.Scale, 0f, 0.2f, 1f, 1.15f).Track(AnimProp.Alpha, 0f, 0f, 0.35f, 1f, 1f, 0.65f)
                .Play(instantBurst);
            UIAnim.Pop(instantHead, 0.8f, 1.04f, 0.3f);
            // @keyframes aceIn (.5 s, ritardi 0 / .15 / .3): da 80 piu' su, scala .6 e dritto, alla sua posa ruotata.
            if (instantAces.gameObject.activeSelf)
                for (int i = 0; i < aceCards.Length && i < AceAngles.Length; i++)
                    new UIKeyframes(0.5f, AceIn).Track(AnimProp.Y, 0f, -80f, 1f, 0f).Track(AnimProp.Scale, 0f, 0.6f, 1f, 1f)
                        .Track(AnimProp.Rotation, 0f, -AceAngles[i], 1f, 0f).Track(AnimProp.Alpha, 0f, 0f, 1f, 1f).Play(aceCards[i], i * 0.15f);
            UIAnim.FadeUp(rewards, 0.9f);
            UIAnim.FadeUp(matchFooter, 1.1f);
            // @keyframes drop: da -60 a 900 girando, compare all'8%; 28 monete coi tempi del mockup. Con la grafica ridotta niente pioggia.
            coinRain.gameObject.SetActive(UIAnim.DecorativeLoops);
            if (!UIAnim.DecorativeLoops) return;
            for (int i = 0; i < coins.Length && i < coinTimes.Length; i++)
                new UIKeyframes(coinTimes[i].x, UIEase.Linear).Track(AnimProp.Y, 0f, -60f, 1f, 900f).Track(AnimProp.Rotation, 0f, 0f, 1f, coinTimes[i].z)
                    .Track(AnimProp.Alpha, 0f, 0f, 0.08f, 1f, 1f, 0.9f).Play(coins[i], coinTimes[i].y, -1);
        }

        /// <summary>Riga XP: from/to totali (from &lt; 0 = niente progresso, riga nascosta), ospite = invito a registrarsi.</summary>
        public void ShowXp(int from, int to, bool guest)
        {
            rewards.gameObject.SetActive(from >= 0);
            if (from < 0) return;
            // Riga delle monete solo per chi ha un account: il posto si prende subito (prima delle entrate), il numero arriva dal server.
            if (coinsRow != null)
            {
                coinsRow.SetActive(!guest);
                rewards.sizeDelta = new Vector2(rewards.sizeDelta.x, guest ? 77f : 107f);
                stats.anchoredPosition = new Vector2(stats.anchoredPosition.x, rewards.anchoredPosition.y - rewards.sizeDelta.y - 14f);
                ShowCoins(null);
            }
            int level = PlayerXp.LevelOf(to), need = PlayerXp.XpToNext(level), into = PlayerXp.XpInLevel(to);
            levelText.gameObject.SetActive(!guest);
            xpTrack.gameObject.SetActive(!guest);
            xpText.gameObject.SetActive(!guest);
            xpGain.text = guest ? "Registrati per guadagnare XP" : "+" + (to - from) + " XP";
            xpGain.rectTransform.sizeDelta = new Vector2(guest ? 300f : 64f, xpGain.rectTransform.sizeDelta.y);
            if (guest) return;
            SetXp(from, to);
        }

        /// <summary>"+XP" e barra, anche a pannello aperto quando il server corregge la stima (limiti, abbandono non valido).</summary>
        public void SetXp(int from, int to)
        {
            int level = PlayerXp.LevelOf(to), need = PlayerXp.XpToNext(level), into = PlayerXp.XpInLevel(to);
            xpGain.text = "+" + (to - from) + " XP";
            levelText.text = "Livello " + level;
            xpText.text = into + "/" + need;
            float start = PlayerXp.LevelOf(from) == level ? (float)PlayerXp.XpInLevel(from) / need : 0f;
            UIAnim.XpBar(xpFill, start, (float)into / need);
        }

        /// <summary>Monete di fine partita dal server: null = in attesa ("…"); errore = "non disponibili".</summary>
        public void ShowCoins(ServerReward r, bool failed = false)
        {
            if (coinsLabel == null) return;
            coinsLabel.color = UI51Tokens.GoldLight;
            if (failed) { coinsLabel.text = "Monete non disponibili"; coinsLabel.color = UI51Tokens.CreamA(0.5f); }
            else if (r == null) coinsLabel.text = "…";
            else if (r.monete > 0) coinsLabel.text = "+" + r.monete + " monete";
            else
            {
                coinsLabel.text = r.limiteAbbandoni ? "Niente monete: abbandoni già premiati oggi" : r.tetto ? "Tetto di monete di oggi raggiunto" : "+0 monete";
                coinsLabel.color = UI51Tokens.CreamA(0.6f);
            }
        }

        private void PlayMatch()
        {
            if (Instant) { UIAnim.Rays(rays); PlayInstant(); return; }
            UIAnim.PopTitle(title);
            UIAnim.FadeUp(rankMode ? rank : duel, 0.35f);
            UIAnim.FadeUp(rewards, 0.6f);
            UIAnim.FadeUp(stats, 0.85f);
            UIAnim.FadeUp(matchFooter, 1.1f);
            UIAnim.Rays(rays);
        }

        /// <summary>Animazioni d'entrata, una volta che il pannello e' acceso.</summary>
        public void Play(bool finished)
        {
            if (finished) PlayMatch();
            else PlayRound();
        }
    }
}
