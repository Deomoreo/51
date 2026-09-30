using System.Collections.Generic;
using DG.Tweening;
using Project51.Core;
using Project51.UI51;
using Project51.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Pugno dell'accuso: il pugno sbatte due volte con un'esplosione di luce e a ogni colpo tutte le
    /// carte al tavolo (mani e tavolo) saltano a caso restando al loro posto. Finche' dura, il gioco
    /// aspetta (GamePresentation.MarkBusy). Usato anche per l'anteprima in Collezione (senza carte).
    /// Con <see cref="Parts"/> collegati (tavolo UI51, mockup Partita) invece un pugno solo cade al centro del tavolo
    /// con onde d'urto, bagliore e scritta (UIAnim.Accuso); le carte non saltano, trema il tavolo (GameSocialV2).
    /// </summary>
    public sealed class AccusoImpactV2 : MonoBehaviour
    {
        public CanvasGroup Group;
        public RectTransform Fist;
        public TMP_Text Caption;
        [Tooltip("Esplosione di luce dietro al pugno (Bagliore morbido). Facoltativa.")]
        public Image Burst;

        [Header("UI51 (tavolo del mockup Partita)")]
        public AccusoParts Parts;
        [Tooltip("Ombra netta del titolo (stesso testo di Caption).")]
        public TMP_Text CaptionShadow;
        [Tooltip("Riga sotto al titolo: chi accusa e i punti.")]
        public TMP_Text Who;
        [Tooltip("Centro del tavolo: posizione e scala messe da Stage.")]
        public RectTransform Anchor;
        public RectTransform Hit;
        [Tooltip("Carte accusate sotto la scritta (4 giocatori, accuso di un altro).")]
        public RectTransform CardsRow;

        /// <summary>Tavolo UI51: il campo Parts esiste sempre (serializzato), conta la sua radice.</summary>
        public bool IsUI51 => Parts != null && Parts.root != null;

        /// <summary>Accuso UI51 ancora a schermo: il prossimo aspetta (GameSocialV2) invece di cancellarlo.</summary>
        public bool Revealing => IsUI51 && animation != null && animation.IsActive();

        /// <summary>Scala dell'accuso sul tavolo: 1 fino al tavolo del mockup (499 di altezza), piu' piccola sui telefoni bassi.</summary>
        public static float RevealScale(float rimUnits) => Mathf.Min(1f, rimUnits / 499f);

        private const float SlamDuration = 0.18f;
        private const float JumpDuration = 0.55f;
        private const float HoldDuration = 0.7f;
        private const float FadeDuration = 0.25f;

        private Sequence animation;
        private float speed = 1f;
        private int slams;
        private readonly List<Transform> jumpingCards = new List<Transform>();
        private Image mattaCard;
        private Sprite mattaTarget;
        private System.Action firstImpact;
        private Tween impactCall;

        /// <summary>Al primo colpo del pugno la carta della matta si gira e diventa target (accuso con il jolly).</summary>
        public void QueueMattaFlip(Image card, Sprite target)
        {
            mattaCard = card;
            mattaTarget = target;
        }

        /// <summary>
        /// Tavolo UI51, prima di Play: riga "chi · +punti", centro del tavolo sullo schermo (px) e px per unita' del mockup
        /// (gia' moltiplicati per <see cref="RevealScale"/>). pixelsPerUnit 0 = centro dello schermo, larghezza del mockup.
        /// Nei 4 giocatori pugno e scritta salgono per fare posto alle carte (withCards).
        /// </summary>
        public void Stage(string who, Vector2 screen, float pixelsPerUnit, bool four, bool withCards)
        {
            if (!IsUI51) return;
            if (Who != null) Who.text = who;
            if (pixelsPerUnit <= 0f)
            {
                screen = new Vector2(Screen.width, Screen.height) * 0.5f;
                pixelsPerUnit = Screen.width / 390f;
            }
            if (Anchor != null)
            {
                // Canvas overlay: le coordinate del mondo sono pixel dello schermo.
                Anchor.position = new Vector3(screen.x, screen.y, Anchor.position.z);
                float parent = Anchor.parent != null ? Anchor.parent.lossyScale.x : 1f;
                if (parent > 0f) Anchor.localScale = new Vector3(pixelsPerUnit / parent, pixelsPerUnit / parent, 1f);
            }
            if (Hit != null) Hit.anchoredPosition = new Vector2(195f, four ? -64f : -140.8f);
            if (Parts.text != null)
            {
                UIAnim.Stop(Parts.text); // un accuso ancora in corso rimetterebbe la sua posa
                Parts.text.anchoredPosition = new Vector2(195f, four ? -115.2f : -220.8f);
            }
            if (CardsRow != null) CardsRow.gameObject.SetActive(withCards);
        }

        /// <param name="onDone">Solo tavolo UI51: alla chiusura (2.6 s), quando Revealing e' gia' falso.</param>
        public void Play(string title, Transform[] cards, System.Action onFirstImpact = null, System.Action onDone = null)
        {
            animation?.Kill();
            impactCall?.Kill();
            // Several players can declare in the same frame, before the first slam.
            firstImpact += onFirstImpact;
            RestCards();
            mattaCard = null;
            mattaTarget = null;
            // Impostazioni in partita: con "Animazioni veloci" tutto il pugno dura meno.
            speed = GamePreferences.AnimationSpeed;
            slams = 0;
            Caption.text = title;
            if (CaptionShadow != null) CaptionShadow.text = title;

            if (IsUI51)
            {
                if (cards != null) GamePresentation.MarkBusy(GamePreferences.Scaled(2.6f) + 0.1f);
                animation = UIAnim.Accuso(Parts, () =>
                {
                    animation = null;
                    onDone?.Invoke();
                });
                if (CardsRow != null && CardsRow.gameObject.activeSelf)
                    foreach (RectTransform card in CardsRow) UIAnim.Flip(card);
                // Colpo al 45% della caduta del pugno; la matta si gira quando le carte sono gia' visibili.
                impactCall = DOTween.Sequence().SetUpdate(true)
                    .InsertCallback(GamePreferences.Scaled(0.27f), Impact)
                    .InsertCallback(GamePreferences.Scaled(0.9f), FlipMatta);
                return;
            }

            Group.gameObject.SetActive(true);
            Group.alpha = 1f;
            Fist.localScale = Vector3.one * 2.2f;
            Fist.localRotation = Quaternion.identity;
            if (Burst != null)
            {
                Burst.rectTransform.localScale = Vector3.one * 0.3f;
                Burst.color = new Color(1f, 1f, 1f, 0f);
            }

            if (cards != null)
            {
                foreach (var card in cards) if (card != null) jumpingCards.Add(card);
            }

            float total = 2f * (SlamDuration + JumpDuration) + HoldDuration + FadeDuration;
            if (jumpingCards.Count > 0) GamePresentation.MarkBusy(total / speed + 0.1f);

            animation = DOTween.Sequence().SetUpdate(true);
            animation.timeScale = speed;
            AppendSlam(animation, 2.2f);
            AppendSlam(animation, 1.7f);
            animation.AppendInterval(HoldDuration)
                .Append(Group.DOFade(0f, FadeDuration))
                .OnComplete(() =>
                {
                    Group.gameObject.SetActive(false);
                    RestCards();
                });
        }

        private void AppendSlam(Sequence sequence, float fromScale)
        {
            sequence.AppendCallback(() => Fist.localScale = Vector3.one * fromScale)
                .Append(Fist.DOScale(1f, SlamDuration).SetEase(Ease.InCubic))
                .AppendCallback(Impact)
                .Append(Fist.DOPunchRotation(new Vector3(0f, 0f, 10f), JumpDuration, 6, 0.5f))
                .Join(((RectTransform)Group.transform).DOPunchAnchorPos(new Vector2(0f, -18f), 0.3f, 8, 0.6f));
        }

        private void Impact()
        {
            var feedback = firstImpact;
            firstImpact = null;
            feedback?.Invoke();
            // Il colpo del suono cade proprio sull'impatto; il secondo colpo e' un po' piu' piano.
            GameAudio.Play(SoundId.Accuso, slams == 0 ? 1f : 0.75f, GameAudio.Sync.Hit, 0.03f);
            slams++;
            if (IsUI51) return;

            FlipMatta();
            if (Burst != null)
            {
                Burst.DOKill();
                Burst.rectTransform.DOKill();
                Burst.rectTransform.localScale = Vector3.one * 0.4f;
                Burst.color = Color.white;
                Paced(Burst.rectTransform.DOScale(1.35f, 0.5f).SetEase(Ease.OutCubic).SetUpdate(true));
                Paced(Burst.DOFade(0f, 0.5f).SetEase(Ease.InQuad).SetUpdate(true));
            }

            foreach (var card in jumpingCards)
            {
                if (card == null) continue;
                card.DOKill(true);
                // Salto relativo: il tween torna esattamente al punto di partenza, il layout resta del gioco.
                float delay = Random.Range(0f, 0.12f);
                Paced(card.DOPunchPosition(Vector3.up * Random.Range(0.25f, 0.75f), JumpDuration, 2, 0.5f).SetDelay(delay).SetUpdate(true).SetLink(card.gameObject));
                Paced(card.DOPunchRotation(new Vector3(0f, 0f, Random.Range(-16f, 16f)), JumpDuration, 3, 0.6f).SetDelay(delay).SetUpdate(true).SetLink(card.gameObject));
            }
        }

        private void FlipMatta()
        {
            if (mattaCard == null || mattaTarget == null) return;
            var card = mattaCard.rectTransform;
            var target = mattaTarget;
            mattaCard = null;
            card.DOKill();
            Paced(card.DOScaleX(0f, 0.14f).SetUpdate(true).OnComplete(() =>
            {
                card.GetComponent<Image>().sprite = target;
                Paced(card.DOScaleX(1f, 0.14f).SetUpdate(true));
                Paced(card.DOPunchScale(Vector3.one * 0.18f, 0.45f, 4, 0.6f).SetDelay(0.14f).SetUpdate(true));
            }));
        }

        private Tween Paced(Tween tween)
        {
            tween.timeScale = speed;
            return tween;
        }

        /// <summary>Ferma i salti e rimette ogni carta nella sua posa di riposo.</summary>
        private void RestCards()
        {
            foreach (var card in jumpingCards)
            {
                if (card == null) continue;
                card.DOKill(true);
                var view = card.GetComponent<CardView>();
                if (view != null) view.SnapToRestPose();
            }
            jumpingCards.Clear();
        }

        private void OnDestroy()
        {
            animation?.Kill();
            impactCall?.Kill();
            RestCards();
        }
    }
}
