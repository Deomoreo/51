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
    /// Pugno dell'accuso sul tavolo UI51 (mockup Partita): con <see cref="Parts"/> collegati un pugno solo cade al centro
    /// del tavolo con onde d'urto, bagliore e scritta (UIAnim.Accuso); trema il tavolo (GameSocialV2). Finche' dura, il
    /// gioco aspetta (GamePresentation.MarkBusy). Senza Parts non mostra niente.
    /// </summary>
    public sealed class AccusoImpactV2 : MonoBehaviour
    {
        public TMP_Text Caption;

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

        private Sequence animation;
        private float speed = 1f;
        private int slams;
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
            mattaCard = null;
            mattaTarget = null;
            // Impostazioni in partita: con "Animazioni veloci" tutto il pugno dura meno.
            speed = GamePreferences.AnimationSpeed;
            slams = 0;
            Caption.text = title;
            if (CaptionShadow != null) CaptionShadow.text = title;

            if (!IsUI51) return;
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
        }

        private void Impact()
        {
            var feedback = firstImpact;
            firstImpact = null;
            feedback?.Invoke();
            // Il colpo del suono cade proprio sull'impatto; il secondo colpo e' un po' piu' piano.
            GameAudio.Play(SoundId.Accuso, slams == 0 ? 1f : 0.75f, GameAudio.Sync.Hit, 0.03f);
            slams++;
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

        private void OnDestroy()
        {
            animation?.Kill();
            impactCall?.Kill();
        }
    }
}
