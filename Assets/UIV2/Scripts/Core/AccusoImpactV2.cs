using System.Collections.Generic;
using DG.Tweening;
using Project51.Core;
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
    /// </summary>
    public sealed class AccusoImpactV2 : MonoBehaviour
    {
        public CanvasGroup Group;
        public RectTransform Fist;
        public TMP_Text Caption;
        [Tooltip("Esplosione di luce dietro al pugno (Bagliore morbido). Facoltativa.")]
        public Image Burst;

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

        /// <summary>Al primo colpo del pugno la carta della matta si gira e diventa target (accuso con il jolly).</summary>
        public void QueueMattaFlip(Image card, Sprite target)
        {
            mattaCard = card;
            mattaTarget = target;
        }

        public void Play(string title, Transform[] cards)
        {
            animation?.Kill();
            RestCards();
            mattaCard = null;
            mattaTarget = null;
            Group.gameObject.SetActive(true);
            Group.alpha = 1f;
            Caption.text = title;
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

            // Impostazioni in partita: con "Animazioni veloci" tutto il pugno dura meno.
            speed = GamePreferences.AnimationSpeed;
            slams = 0;
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
            // Il colpo del suono cade proprio sull'impatto; il secondo colpo e' un po' piu' piano.
            GameAudio.Play(SoundId.Accuso, slams == 0 ? 1f : 0.75f, GameAudio.Sync.Hit, 0.03f);
            slams++;

            if (mattaCard != null && mattaTarget != null)
            {
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
            RestCards();
        }
    }
}
