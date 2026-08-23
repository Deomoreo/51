using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Project51.Unity
{
    /// <summary>
    /// Gestisce esclusivamente le animazioni delle carte presenti nella scena.
    /// Non conosce lo stato o le regole della partita.
    /// </summary>
    public sealed class CardAnimationController : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float playDuration = 0.35f;
        [SerializeField] private float capturePreviewDuration = 0.45f;
        [SerializeField] private float captureDuration = 0.4f;
        [SerializeField] private float flipDuration = 0.2f;
        [SerializeField] private float dealRevealDuration = 0.22f;
        [SerializeField] private float dealRevealStagger = 0.045f;
        [SerializeField] private float initialDealFlightDuration = 0.3f;

        [Header("Feel")]
        [SerializeField] private Ease playEase = Ease.OutCubic;
        [SerializeField] private Ease captureEase = Ease.InOutQuad;
        [SerializeField] private float playArcHeight = 0.4f;
        [SerializeField] private int flightSortingOrderBase = 500;

        /// <summary>
        /// Crea una copia visiva indipendente da layout, input e refresh delle CardView.
        /// Il chiamante deve distruggerla con DestroyVisualCopy al termine della sequenza.
        /// </summary>
        public bool TryCreateVisualCopy(
            Transform sourceTransform,
            SpriteRenderer sourceRenderer,
            string visualName,
            out Transform visualTransform,
            out SpriteRenderer visualRenderer)
        {
            visualTransform = null;
            visualRenderer = null;

            if (sourceTransform == null || sourceRenderer == null || sourceRenderer.sprite == null)
            {
                return false;
            }

            var visual = new GameObject(visualName)
            {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };
            visualTransform = visual.transform;
            visualTransform.position = sourceTransform.position;
            visualTransform.rotation = sourceTransform.rotation;
            visualTransform.localScale = sourceTransform.localScale;

            visualRenderer = visual.AddComponent<SpriteRenderer>();
            visualRenderer.sprite = sourceRenderer.sprite;
            visualRenderer.color = sourceRenderer.color;
            visualRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            visualRenderer.sortingOrder = sourceRenderer.sortingOrder + flightSortingOrderBase;
            return true;
        }

        public void DestroyVisualCopy(Transform visualTransform)
        {
            if (visualTransform != null)
            {
                Destroy(visualTransform.gameObject);
            }
        }

        public Sequence PlayCardToTable(
            Transform cardTransform,
            SpriteRenderer cardRenderer,
            Vector3 targetPosition,
            float targetRotationZ,
            Action onComplete = null)
        {
            if (cardTransform == null || cardRenderer == null)
            {
                return CreateCompletedSequence(onComplete);
            }

            KillTweensOn(cardTransform);

            int originalSortingOrder = cardRenderer.sortingOrder;
            Vector3 originalScale = cardTransform.localScale;
            Vector3 startPosition = cardTransform.position;
            Vector3 midpoint = Vector3.Lerp(startPosition, targetPosition, 0.5f) + Vector3.up * playArcHeight;
            bool restored = false;

            void RestoreVisualState()
            {
                if (restored)
                {
                    return;
                }

                restored = true;
                cardRenderer.sortingOrder = originalSortingOrder;
                cardTransform.localScale = originalScale;
            }

            cardRenderer.sortingOrder = flightSortingOrderBase;

            Sequence sequence = DOTween.Sequence()
                .SetTarget(cardTransform)
                .Append(cardTransform.DOPath(new[] { midpoint, targetPosition }, playDuration, PathType.CatmullRom).SetEase(playEase))
                .Join(cardTransform.DORotate(new Vector3(0f, 0f, targetRotationZ), playDuration))
                .Join(cardTransform.DOScale(originalScale * 1.08f, playDuration * 0.4f).SetLoops(2, LoopType.Yoyo));

            sequence.OnComplete(() =>
            {
                RestoreVisualState();
                onComplete?.Invoke();
            });
            sequence.OnKill(RestoreVisualState);
            return sequence;
        }

        public Sequence CaptureSequence(
            Transform playedCard,
            SpriteRenderer playedCardRenderer,
            IReadOnlyList<Transform> tableCards,
            IReadOnlyList<SpriteRenderer> tableCardRenderers,
            Vector3 pileTargetPosition,
            Action onComplete = null)
        {
            if (playedCard == null || playedCardRenderer == null || tableCards == null || tableCardRenderers == null || tableCards.Count != tableCardRenderers.Count)
            {
                return CreateCompletedSequence(onComplete);
            }

            var cards = new List<Transform> { playedCard };
            cards.AddRange(tableCards);
            var renderers = new List<SpriteRenderer> { playedCardRenderer };
            renderers.AddRange(tableCardRenderers);

            if (renderers.Exists(renderer => renderer == null))
            {
                return CreateCompletedSequence(onComplete);
            }

            var originalOrders = new List<int>(renderers.Count);
            var originalScales = new List<Vector3>(cards.Count);
            for (int i = 0; i < cards.Count; i++)
            {
                KillTweensOn(cards[i]);
                originalOrders.Add(renderers[i].sortingOrder);
                originalScales.Add(cards[i].localScale);
            }

            bool restored = false;
            void RestoreVisualState()
            {
                if (restored)
                {
                    return;
                }

                restored = true;
                for (int i = 0; i < cards.Count; i++)
                {
                    if (cards[i] != null)
                    {
                        cards[i].localScale = originalScales[i];
                    }

                    if (renderers[i] != null)
                    {
                        renderers[i].sortingOrder = originalOrders[i];
                    }
                }
            }

            Sequence sequence = DOTween.Sequence().SetTarget(this);
            for (int i = 0; i < cards.Count; i++)
            {
                sequence.Join(cards[i].DOScale(originalScales[i] * 1.15f, 0.12f).SetLoops(2, LoopType.Yoyo));
            }

            sequence.AppendInterval(0.15f);
            for (int i = 0; i < cards.Count; i++)
            {
                int cardIndex = i;
                Transform card = cards[cardIndex];
                SpriteRenderer renderer = renderers[cardIndex];
                Vector3 destination = pileTargetPosition + new Vector3(0f, cardIndex * 0.015f, 0f);

                Sequence flight = DOTween.Sequence()
                    .AppendCallback(() => renderer.sortingOrder = flightSortingOrderBase + cardIndex)
                    .Append(card.DOMove(destination, captureDuration).SetEase(captureEase))
                    .Join(card.DORotate(new Vector3(0f, 0f, UnityEngine.Random.Range(-6f, 6f)), captureDuration))
                    .Join(card.DOScale(originalScales[cardIndex] * 0.7f, captureDuration));
                sequence.Join(flight);
            }

            sequence.OnComplete(() =>
            {
                RestoreVisualState();
                onComplete?.Invoke();
            });
            sequence.OnKill(RestoreVisualState);
            return sequence;
        }

        /// <summary>
        /// Mantiene la carta giocata sul tavolo abbastanza a lungo da rendere leggibile la presa.
        /// </summary>
        public Sequence CreateCapturePreview()
        {
            return DOTween.Sequence()
                .SetTarget(this)
                .AppendInterval(capturePreviewDuration);
        }

        public Sequence PlayDealtCardsReveal(IReadOnlyList<CardView> cardViews)
        {
            if (cardViews == null || cardViews.Count == 0)
            {
                return CreateCompletedSequence(null);
            }

            var sequence = DOTween.Sequence().SetTarget(this);
            for (int i = 0; i < cardViews.Count; i++)
            {
                var cardView = cardViews[i];
                if (cardView == null)
                {
                    continue;
                }

                Transform cardTransform = cardView.transform;
                Vector3 originalScale = cardTransform.localScale;
                cardTransform.localScale = originalScale * 0.12f;

                float startAt = i * dealRevealStagger;
                sequence.Insert(startAt, cardTransform.DOScale(originalScale, dealRevealDuration).SetEase(Ease.OutBack));
            }

            return sequence;
        }

        public Sequence PlayInitialDealFromSource(IReadOnlyList<CardView> cardViews, Vector3 sourcePosition)
        {
            if (cardViews == null || cardViews.Count == 0)
            {
                return CreateCompletedSequence(null);
            }

            var entries = new List<(Transform transform, Vector3 targetPosition, Vector3 targetScale)>();
            for (int i = 0; i < cardViews.Count; i++)
            {
                var cardView = cardViews[i];
                if (cardView == null)
                {
                    continue;
                }

                Transform cardTransform = cardView.transform;
                entries.Add((cardTransform, cardTransform.position, cardTransform.localScale));
                cardTransform.position = sourcePosition;
                cardTransform.localScale = cardTransform.localScale * 0.1f;
            }

            var sequence = DOTween.Sequence().SetTarget(this);
            for (int i = 0; i < entries.Count; i++)
            {
                int index = i;
                float startAt = index * dealRevealStagger;
                sequence.Insert(startAt,
                    entries[index].transform.DOMove(entries[index].targetPosition, initialDealFlightDuration).SetEase(Ease.OutCubic));
                sequence.Insert(startAt,
                    entries[index].transform.DOScale(entries[index].targetScale, dealRevealDuration).SetEase(Ease.OutBack));
            }

            return sequence;
        }

        public Sequence FlipCard(SpriteRenderer renderer, Sprite frontSprite, Action onComplete = null)
        {
            if (renderer == null)
            {
                return CreateCompletedSequence(onComplete);
            }

            Transform cardTransform = renderer.transform;
            KillTweensOn(cardTransform);

            Vector3 originalScale = cardTransform.localScale;
            Sequence sequence = DOTween.Sequence()
                .SetTarget(cardTransform)
                .Append(cardTransform.DOScaleX(0f, flipDuration * 0.5f).SetEase(Ease.InQuad))
                .AppendCallback(() =>
                {
                    if (frontSprite != null)
                    {
                        renderer.sprite = frontSprite;
                    }
                })
                .Append(cardTransform.DOScaleX(originalScale.x, flipDuration * 0.5f).SetEase(Ease.OutQuad));

            sequence.OnComplete(() =>
            {
                if (onComplete != null)
                {
                    onComplete();
                }
            });
            sequence.OnKill(() => cardTransform.localScale = originalScale);
            return sequence;
        }

        public void KillTweensOn(Transform cardTransform)
        {
            if (cardTransform != null)
            {
                cardTransform.DOKill();
            }
        }

        private static Sequence CreateCompletedSequence(Action onComplete)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.AppendCallback(() => onComplete?.Invoke());
            return sequence;
        }
    }
}
