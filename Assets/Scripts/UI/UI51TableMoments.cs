using DG.Tweening;
using Project51.Core;
using Project51.UI51;
using Project51.UIV2.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Momenti di partita (mockup MomentiPartita) sopra al tavolo, senza tocchi: chip della mano ("MANO 3 DI 6", "ULTIMA MANO ·
    /// MAZZO FINITO") 100 sotto al banner in alto, avvisi sui giocatori e di connessione 18 sotto, SCOPA! al centro.
    /// Avvisi brevi coi tempi della SPEC: compaiono in .3 s, restano 2.5 s, svaniscono in .6 s.
    /// Ascolta GamePresentation (HandDealt, Scopa, PlayerNotice, ConnectionNotice). Costruita da UI51TableBuilder.BuildMoments.
    /// </summary>
    public sealed class UI51TableMoments : MonoBehaviour
    {
        const float In = 0.3f, Stay = 2.5f, Out = 0.6f;
        // Mockup: banner in alto da 82 a 132, avvisi a 150, chip della mano a 232.
        const float NoticeGap = 18f, ChipGap = 100f;
        const float ScopaSeconds = 2.2f;

        [SerializeField] private PlayerBannerManager banners;
        [SerializeField] private RectTransform chipHolder, chip;
        [SerializeField] private UI51Shape chipShape;
        [SerializeField] private TMP_Text chipText;
        [SerializeField] private RectTransform noticeHolder, notice;
        [SerializeField] private UI51Shape noticeShape;
        [SerializeField] private HorizontalLayoutGroup noticeRow;
        [SerializeField] private AvatarFrame noticeAvatar;
        [SerializeField] private TMP_Text noticeTitle, noticeSub;
        [SerializeField] private RectTransform scopa, scopaVeil, scopaBurst, scopaWords;
        [SerializeField] private TMP_Text scopaWho;
        [SerializeField] private RectTransform[] sparks = new RectTransform[0];
        [SerializeField] private Vector2[] sparkTargets = new Vector2[0];

        // Tempo del turno (SPEC: compare solo negli ultimi 5 secondi; lo conta TurnController): pillola rossa vicino al banner di chi
        // gioca e anello rosso sul banner; al proprio turno "Gioca adesso" sopra la mano.
        const float LowSeconds = 5f;
        [SerializeField] private RectTransform timerHolder, timerPill;
        [SerializeField] private TMP_Text timerSecs, timerWho;
        [SerializeField] private RectTransform tempoHolder, tempoPill;
        [SerializeField] private TMP_Text tempoSecs, tempoText;
        private TurnController turn;
        private Project51.UI51.PlayerBanner lowBanner;
        private int lowSeat = -1;

        private readonly Vector3[] corners = new Vector3[4];

        // Chip "a riposo" (mano, ultima mano) come la fa il builder: il turno la rovescia in oro pieno e la riporta qui.
        private Gradient chipFill;
        private float chipHeight, chipFont;

        private void Awake()
        {
            chipFill = chipShape.fill;
            chipHeight = chip.sizeDelta.y;
            chipFont = chipText.fontSize;
            chip.gameObject.SetActive(false);
            notice.gameObject.SetActive(false);
            scopa.gameObject.SetActive(false);
            if (timerPill != null) { timerPill.gameObject.SetActive(false); tempoPill.gameObject.SetActive(false); }
            noticeAvatar.SetFrame(FrameStyle.Grigio); // mockup: anello crema .3
        }

        private void OnEnable()
        {
            GamePresentation.HandDealt += Hand;
            GamePresentation.YourTurn += YourTurn;
            GamePresentation.Scopa += Scopa;
            GamePresentation.PlayerNotice += Player;
            GamePresentation.ConnectionNotice += Connection;
        }

        private void OnDisable()
        {
            GamePresentation.HandDealt -= Hand;
            GamePresentation.YourTurn -= YourTurn;
            GamePresentation.Scopa -= Scopa;
            GamePresentation.PlayerNotice -= Player;
            GamePresentation.ConnectionNotice -= Connection;
            if (timerPill != null) ShowLowTime(-1);
        }

        private void Update()
        {
            if (turn == null) turn = FindObjectOfType<TurnController>();
            if (turnChip && (turn == null || !turn.AcceptsLocalInput || (tempoPill != null && tempoPill.gameObject.activeSelf))) HideTurnChip();
            if (timerPill == null) return;
            float left = turn != null ? turn.TurnTimeLeft : -1f;
            int seat = left >= 0f && left <= LowSeconds ? turn.CurrentPlayerIndex : -1;
            if (seat != lowSeat) ShowLowTime(seat);
            if (seat < 0) return;
            int secs = Mathf.CeilToInt(left);
            if (tempoPill.gameObject.activeSelf) tempoSecs.text = secs.ToString();
            else timerSecs.text = secs + "s";
        }

        private void ShowLowTime(int seat)
        {
            lowSeat = seat;
            if (lowBanner != null) lowBanner.SetLowTime(false);
            lowBanner = seat >= 0 && banners != null ? banners.UI51BannerForPlayer(seat) : null;
            if (lowBanner != null) lowBanner.SetLowTime(true);
            bool mine = seat >= 0 && GameModeService.Current.IsLocalPlayer(seat);
            tempoPill.gameObject.SetActive(mine);
            timerPill.gameObject.SetActive(seat >= 0 && !mine);
            if (seat < 0) return;
            if (mine)
            {
                // All'ultimo tempo scaduto si esce dalla partita (TurnController.InactiveTooLong): lo dice la pillola.
                tempoText.text = turn.Timeouts >= TurnController.MaxTimeouts - 1 ? "Gioca adesso, o uscirai dalla partita"
                    : "Gioca adesso, o la carta verrà scelta per te";
                tempoSecs.text = Mathf.CeilToInt(turn.TurnTimeLeft).ToString();
                PlaceTimer(tempoHolder, tempoPill, banners != null ? banners.UI51Banner(0) : null, true);
                UIAnim.Pop(tempoPill, 0.8f, 1.04f);
            }
            else
            {
                timerWho.text = GameSocialV2.PlayerName(seat);
                timerSecs.text = Mathf.CeilToInt(turn.TurnTimeLeft) + "s";
                PlaceTimer(timerHolder, timerPill, lowBanner, false);
                UIAnim.Pop(timerPill, 0.8f, 1.04f);
            }
        }

        /// <summary>
        /// Mockup: la mia pillola 192 sopra al mio banner (top 552 contro 774), centrata; quella degli altri 12 sotto al banner in alto
        /// o 6 sopra ai banner laterali, centrata sul banner e dentro lo schermo (12 dai bordi).
        /// </summary>
        private void PlaceTimer(RectTransform holder, RectTransform pill, Project51.UI51.PlayerBanner banner, bool mine)
        {
            if (banner == null) return;
            var parent = (RectTransform)holder.parent;
            float unit = holder.localScale.y;
            ((RectTransform)banner.transform).GetWorldCorners(corners);
            Vector2 min = parent.InverseTransformPoint(corners[0]), max = parent.InverseTransformPoint(corners[2]);
            LayoutRebuilder.ForceRebuildLayoutImmediate(pill);
            float height = pill.rect.height * unit, half = (pill.rect.width * 0.5f + 12f) * unit;
            float top = mine ? max.y + (192f + 30f) * unit : max.x - min.x > max.y - min.y ? min.y - 12f * unit : max.y + 6f * unit + height;
            float x = mine ? parent.rect.center.x : Mathf.Clamp((min.x + max.x) * 0.5f, parent.rect.xMin + half, parent.rect.xMax - half);
            holder.anchoredPosition = new Vector2(x, top) - parent.rect.center;
        }

        private void Hand(int hand, int total)
        {
            turnChip = false;
            UIAnim.Stop(chip);
            TurnLook(false);
            bool last = hand >= total;
            chipText.text = last ? "ULTIMA MANO · MAZZO FINITO" : $"MANO {hand} DI {total}";
            chipText.color = last ? UI51Tokens.DangerText : UI51Tokens.Gold;
            chipShape.borderColor = last ? UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.6f) : UI51Tokens.GoldA(0.55f);
            Below(chipHolder, ChipGap);
            Brief(chip, Stay, true);
        }

        /// <summary>
        /// B5 (scelta utente 07/10): "TOCCA A TE" sopra la mano, con la chip della mano messa dove sta "Gioca adesso". Resta anche con
        /// la grafica ridotta. Se c'e' gia' "Gioca adesso" basta quella. Giro Android 08/10 (turno ancora poco evidente): resta per tutto
        /// il turno con un respiro lieve (prima spariva dopo 1,2 s) e va via appena il tocco non conta piu' o arriva "Gioca adesso".
        /// Secondo giro Android 08/10 (ancora poco evidente): chip rovesciata, oro pieno con scritta scura, piu' alta e col testo piu'
        /// grande, entrata con rimbalzo e respiro piu' marcato. Resta dove stava, sopra al banner e sotto la mano: niente carte coperte.
        /// </summary>
        private void YourTurn()
        {
            if (tempoPill != null && tempoPill.gameObject.activeSelf) return;
            chipText.text = "TOCCA A TE";
            TurnLook(true);
            PlaceTimer(chipHolder, chip, banners != null ? banners.UI51Banner(0) : null, true);
            UIAnim.Stop(chip);
            chip.gameObject.SetActive(true);
            turnChip = true;
            var pop = new UIKeyframes(In, UIEase.EaseOut).Track(AnimProp.Alpha, 0f, 0f, 1f, 1f).Track(AnimProp.Scale, 0f, 0.7f, 0.65f, 1.18f, 1f, 1f)
                .Play(chip);
            if (pop != null) pop.OnComplete(() => { if (turnChip) UIAnim.Breathe(chip, 1.09f, 1.2f); });
        }

        /// <summary>Aspetto del turno (oro pieno, scritta blu notte, 36 alta, testo 15) o quello a riposo del builder.</summary>
        private void TurnLook(bool turn)
        {
            chipShape.fill = turn ? UI51Shape.Linear((UI51Tokens.Rgba(255, 226, 150, 1f), 0f), (UI51Tokens.Gold, 1f)) : chipFill;
            chipShape.borderWidth = turn ? 2f : 1f;
            chipShape.borderColor = turn ? UI51Tokens.Rgba(255, 244, 214, 1f) : UI51Tokens.GoldA(0.55f);
            chipText.color = turn ? UI51Tokens.Rgba(6, 13, 27, 1f) : UI51Tokens.Gold;
            chipText.fontSize = turn ? chipFont + 3f : chipFont;
            chip.sizeDelta = new Vector2(chip.sizeDelta.x, turn ? chipHeight + 6f : chipHeight);
        }

        private bool turnChip;

        private void HideTurnChip()
        {
            turnChip = false;
            UIAnim.Stop(chip);
            var fade = new UIKeyframes(0.25f, UIEase.EaseInOut).Track(AnimProp.Alpha, 0f, 1f, 1f, 0f).Track(AnimProp.Scale, 0f, 1f, 1f, 1f).Play(chip);
            if (fade != null) fade.OnComplete(() => chip.gameObject.SetActive(false));
            else chip.gameObject.SetActive(false);
        }

        private void Player(int seat, string title, string sub, bool alert) => ShowNotice(seat, title, sub, alert, Stay);

        /// <summary>Avvisi di connessione senza giocatore ("Sei di nuovo in partita!"): secondi 0 = resta, testo vuoto = via.</summary>
        private void Connection(string message, float seconds)
        {
            if (string.IsNullOrEmpty(message))
            {
                if (notice.gameObject.activeSelf)
                    new UIKeyframes(Out, UIEase.EaseInOut).Track(AnimProp.Alpha, 0f, 1f, 1f, 0f).Play(notice)
                        ?.OnComplete(() => notice.gameObject.SetActive(false));
                return;
            }
            ShowNotice(-1, message, null, false, seconds);
        }

        private void ShowNotice(int seat, string title, string sub, bool alert, float stay)
        {
            var portrait = seat >= 0 && banners != null ? banners.SeatAvatar(seat) : null;
            noticeAvatar.gameObject.SetActive(portrait != null);
            if (portrait != null) noticeAvatar.SetAvatar(portrait);
            // Disconnesso: ritratto in scala di grigi (.8) nel mockup; la UI non desatura, si spegne.
            noticeAvatar.SetTint(alert ? new Color(0.62f, 0.62f, 0.62f, 1f) : Color.white);
            noticeRow.padding.left = portrait != null ? 8 : 16;
            noticeTitle.text = title;
            bool two = !string.IsNullOrEmpty(sub);
            noticeSub.gameObject.SetActive(two);
            if (two) noticeSub.text = sub;
            noticeShape.borderColor = alert ? UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.55f) : UI51Tokens.GoldA(0.5f);
            Below(noticeHolder, NoticeGap);
            if (stay > 0f) Brief(notice, stay, false);
            else
            {
                notice.gameObject.SetActive(true);
                new UIKeyframes(In, UIEase.EaseOut).Track(AnimProp.Y, 0f, -10f, 1f, 0f).Track(AnimProp.Alpha, 0f, 0f, 1f, 1f).Play(notice);
            }
        }

        /// <summary>@keyframes chip / notice del mockup coi tempi della SPEC; pop = chip (scala), altrimenti scende di 10.</summary>
        private static void Brief(RectTransform t, float stay, bool pop)
        {
            t.gameObject.SetActive(true);
            float total = In + stay + Out, a = In / total, b = (In + stay) / total;
            var k = new UIKeyframes(total, UIEase.EaseInOut).Track(AnimProp.Alpha, 0f, 0f, a, 1f, b, 1f, 1f, 0f);
            if (pop) k.Track(AnimProp.Scale, 0f, 0.85f, a, 1.04f, a * 1.5f, 1f, 1f, 1f);
            else k.Track(AnimProp.Y, 0f, -10f, a, 0f, b, 0f, 1f, -6f);
            var tween = k.Play(t);
            if (tween != null) tween.OnComplete(() => t.gameObject.SetActive(false));
            else t.gameObject.SetActive(false);
        }

        /// <summary>Holder (scala del mockup, pivot in alto) gap sotto al banner in alto; senza banner, alla stessa distanza dal bordo.</summary>
        private void Below(RectTransform holder, float gap)
        {
            var parent = (RectTransform)holder.parent;
            float unit = holder.localScale.y;
            float y = parent.rect.yMax - 132f * unit;
            var seat = banners != null ? banners.UI51Banner(2) : null;
            if (seat != null && seat.isActiveAndEnabled)
            {
                ((RectTransform)seat.transform).GetWorldCorners(corners);
                y = parent.InverseTransformPoint(corners[0]).y;
            }
            holder.anchoredPosition = new Vector2(0f, y - gap * unit - parent.rect.center.y);
        }

        /// <summary>
        /// Mockup MomentoScopa: velo scuro, bagliore che scoppia, SCOPA! che entra ruotando grande e si posa, "Tu · +1",
        /// scintille dal centro; poi tutto svanisce (in 2.2 s). La presa vera vola intanto nel mazzetto.
        /// </summary>
        private void Scopa(int player)
        {
            scopaWho.text = GameSocialV2.PlayerName(player) + " · <color=#FCE29A>+1</color>";
            scopa.gameObject.SetActive(true);
            new UIKeyframes(ScopaSeconds, UIEase.EaseInOut).Track(AnimProp.Alpha, 0f, 0f, 0.05f, 1f, 0.8f, 1f, 1f, 0f).Play(scopa)
                ?.OnComplete(() => scopa.gameObject.SetActive(false));
            // @keyframes burst (1 s): scala .2 -> 1.15, alpha 0 -> 1 (35%) -> .65.
            new UIKeyframes(1f, UIEase.EaseOut).Track(AnimProp.Scale, 0f, 0.2f, 1f, 1.15f).Track(AnimProp.Alpha, 0f, 0f, 0.35f, 1f, 1f, 0.65f)
                .Play(scopaBurst);
            // @keyframes scopaTxt (.6 s, cubic .3,.7,.3,1): 2.4 -8 gradi trasparente, .92 +2 gradi (45%), 1.05 (60%), 1.
            new UIKeyframes(0.6f, new CubicBezier(0.3f, 0.7f, 0.3f, 1f))
                .Track(AnimProp.Scale, 0f, 2.4f, 0.45f, 0.92f, 0.6f, 1.05f, 1f, 1f)
                .Track(AnimProp.Rotation, 0f, -8f, 0.45f, 2f, 0.6f, 0f, 1f, 0f)
                .Track(AnimProp.Alpha, 0f, 0f, 0.45f, 1f, 1f, 1f)
                .Play(scopaWords);
            // @keyframes spark (.9 s dopo .25 s): dal centro a (dx, dy), scala 0 -> 1, alpha 1 -> 0.
            for (int i = 0; i < sparks.Length && i < sparkTargets.Length; i++)
                new UIKeyframes(0.9f, UIEase.EaseOut)
                    .Track(AnimProp.X, 0f, 0f, 1f, sparkTargets[i].x).Track(AnimProp.Y, 0f, 0f, 1f, sparkTargets[i].y)
                    .Track(AnimProp.Scale, 0f, 0f, 1f, 1f).Track(AnimProp.Alpha, 0f, 1f, 1f, 0f)
                    .Play(sparks[i], 0.25f);
        }
    }
}
