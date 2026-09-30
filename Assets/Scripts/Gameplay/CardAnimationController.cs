using System;
using System.Collections.Generic;
using DG.Tweening;
using Project51.Core;
using UnityEngine;

namespace Project51.Unity
{
    /// <summary>
    /// Gestisce esclusivamente le animazioni delle carte presenti nella scena.
    /// Non conosce lo stato o le regole della partita.
    /// </summary>
    public sealed class CardAnimationController : MonoBehaviour
    {
        private const int DealFlightSortingBoost = 30; // tavolo 10+ e dorsi 20+ a riposo: in volo il tavolo passa sopra

        [Header("Timing")]
        [SerializeField] private float playDuration = 0.35f;
        [SerializeField] private float capturePreviewDuration = 0.45f;
        [SerializeField] private float captureDuration = 0.4f;
        [SerializeField] private float flipDuration = 0.2f;
        [SerializeField] private float dealRevealDuration = 0.22f;
        [SerializeField] private float dealRevealStagger = 0.045f;

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
            visualRenderer.flipX = sourceRenderer.flipX;
            visualRenderer.flipY = sourceRenderer.flipY;
            visual.AddComponent<CardDropShadow>().Bind(visualRenderer);
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
            float targetScale,
            Action onComplete = null)
        {
            if (cardTransform == null || cardRenderer == null)
            {
                return CreateCompletedSequence(onComplete);
            }

            KillTweensOn(cardTransform);

            int originalSortingOrder = cardRenderer.sortingOrder;
            Vector3 originalScale = cardTransform.localScale;
            Vector3 destinationScale = Vector3.one * Mathf.Max(0.01f, targetScale);
            Vector3 liftScale = Vector3.Lerp(originalScale, destinationScale, 0.5f) * 1.06f;
            Vector3 startPosition = cardTransform.position;
            Quaternion startRotation = cardTransform.rotation;
            Vector3 midpoint = Vector3.Lerp(startPosition, targetPosition, 0.5f) + Vector3.up * playArcHeight;
            bool restored = false;
            bool completed = false;
            var shadow = cardTransform.GetComponent<CardDropShadow>();
            float duration = Mathf.Max(.01f, playDuration);

            void RestoreVisualState()
            {
                if (restored)
                {
                    return;
                }

                restored = true;
                if (cardRenderer != null) cardRenderer.sortingOrder = originalSortingOrder;
                if (shadow != null) shadow.SetElevation(0f);
                if (!completed && cardTransform != null)
                {
                    cardTransform.position = startPosition;
                    cardTransform.rotation = startRotation;
                    cardTransform.localScale = originalScale;
                }
            }

            cardRenderer.sortingOrder = flightSortingOrderBase;

            Sequence sequence = DOTween.Sequence()
                .SetTarget(cardTransform)
                // One continuous path: separate eased moves introduce a visible stop at
                // the midpoint and an abrupt landing before the sequence has finished.
                .Append(cardTransform.DOPath(new[] { midpoint, targetPosition }, duration, PathType.CatmullRom).SetEase(playEase))
                .Join(cardTransform.DORotate(new Vector3(0f, 0f, targetRotationZ), duration))
                .Join(DOTween.Sequence()
                    .Append(cardTransform.DOScale(liftScale, duration * .45f).SetEase(Ease.OutQuad))
                    .Append(cardTransform.DOScale(destinationScale, duration * .55f).SetEase(Ease.InOutQuad)));

            if (shadow != null)
                sequence.Insert(0f, DOVirtual.Float(0f, 1f, duration,
                    progress => shadow.SetElevation(Mathf.Sin(progress * Mathf.PI))).SetEase(Ease.Linear));

            sequence.OnComplete(() =>
            {
                completed = true;
                RestoreVisualState();
                onComplete?.Invoke();
            });
            sequence.OnKill(RestoreVisualState);
            // Il colpo del suono cade quando la carta tocca il tavolo.
            GameAudio.Play(SoundId.CardPlay, sync: GameAudio.Sync.Hit, hitIn: GamePreferences.Scaled(duration));
            return Paced(sequence);
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
                    .Join(card.DOScale(originalScales[cardIndex] * 0.45f, captureDuration))
                    // Sparisce entrando nel mazzetto accanto al banner, invece di restare grande
                    // sopra al mazzetto per un frame quando la scala viene ripristinata.
                    .Insert(captureDuration * 0.75f, DOTween.ToAlpha(() => renderer.color, c => renderer.color = c, 0f, captureDuration * 0.25f));
                sequence.Join(flight);
            }

            sequence.OnComplete(() =>
            {
                RestoreVisualState();
                onComplete?.Invoke();
            });
            sequence.OnKill(RestoreVisualState);
            GameAudio.Play(SoundId.CardCapture);
            return Paced(sequence);
        }

        /// <summary>
        /// Mantiene la carta giocata sul tavolo abbastanza a lungo da rendere leggibile la presa.
        /// </summary>
        public Sequence CreateCapturePreview()
        {
            return Paced(DOTween.Sequence()
                .SetTarget(this)
                .AppendInterval(capturePreviewDuration));
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

                float startAt = i * dealRevealStagger;
                sequence.InsertCallback(startAt, () => cardTransform.localScale = originalScale * 0.12f);
                sequence.Insert(startAt, cardTransform.DOScale(originalScale, dealRevealDuration).SetEase(Ease.OutBack));
            }

            PlayDealSound(cardViews.Count, dealRevealStagger);
            return Paced(sequence);
        }

        /// <summary>
        /// Come PlayDealtCardsReveal, ma le carte partono visivamente dalla posizione del
        /// mazziere (originPosition) invece di comparire ferme nella posizione finale - usata
        /// per l'animazione di distribuzione a inizio smazzata e ad ogni redeal (mano + carte
        /// tavolo). Le carte devono essere gia' state "staged" da
        /// CardViewManager.StageCardsAtOriginForDealAnimation PRIMA di chiamare questo metodo
        /// (posizione/scala finale gia' catturate, transform gia' spostato sul mazziere) - qui ci
        /// si limita a farle rientrare, non a leggere una posizione "attuale" che a questo punto
        /// sarebbe gia' quella del mazziere per tutte.
        /// </summary>
        public Sequence PlayDealtCardsFromOrigin(IReadOnlyList<CardViewManager.StagedCard> stagedCards, Vector3 originPosition, float staggerOverride = -1f)
        {
            if (stagedCards == null || stagedCards.Count == 0)
            {
                return CreateCompletedSequence(null);
            }

            float stagger = staggerOverride >= 0f ? staggerOverride : dealRevealStagger;
            var sequence = DOTween.Sequence().SetTarget(this);
            // In volo sopra le carte gia' posate: dal mazzo (in alto a sinistra) quelle del tavolo passano sui dorsi avversari.
            var boosted = new List<KeyValuePair<SpriteRenderer, int>>();
            for (int i = 0; i < stagedCards.Count; i++)
            {
                var staged = stagedCards[i];
                var cardView = staged.View;
                if (cardView == null)
                {
                    continue;
                }

                Transform cardTransform = cardView.transform;
                Vector3 finalPosition = staged.FinalPosition;
                Vector3 finalScale = staged.FinalScale;
                SpriteRenderer cardRenderer = cardView.CardRenderer;

                float startAt = i * stagger;
                sequence.InsertCallback(startAt, () =>
                {
                    cardTransform.position = originPosition;
                    cardTransform.localScale = finalScale * 0.12f;
                    if (cardRenderer == null) return;
                    cardRenderer.enabled = true;
                    if (cardRenderer.sortingOrder >= 400) return; // gia' in volo alto
                    boosted.Add(new KeyValuePair<SpriteRenderer, int>(cardRenderer, cardRenderer.sortingOrder));
                    cardRenderer.sortingOrder += DealFlightSortingBoost;
                });
                sequence.Insert(startAt, cardTransform.DOMove(finalPosition, dealRevealDuration).SetEase(playEase));
                sequence.Insert(startAt, cardTransform.DOScale(finalScale, dealRevealDuration).SetEase(Ease.OutBack));
            }

            TweenCallback land = () =>
            {
                foreach (var b in boosted)
                    if (b.Key != null && b.Key.sortingOrder == b.Value + DealFlightSortingBoost) b.Key.sortingOrder = b.Value;
                boosted.Clear();
            };
            sequence.OnComplete(land).OnKill(land);

            PlayDealSound(stagedCards.Count, stagger);
            return Paced(sequence);
        }

        /// <summary>
        /// Spazza via le carte indicate verso una posizione unica (es. il mazziere che si prende
        /// le carte tavolo dopo un accuso Dealer15/Dealer30) - stessa idea del volo di
        /// CaptureSequence ma senza la "carta giocata" iniziale, per un gruppo di carte che parte
        /// gia' ferma sul tavolo invece che da un tiro di gioco.
        /// </summary>
        public Sequence PlaySweepToPile(IReadOnlyList<Transform> cards, IReadOnlyList<SpriteRenderer> renderers, Vector3 pileTargetPosition)
        {
            if (cards == null || renderers == null || cards.Count == 0 || cards.Count != renderers.Count)
            {
                return CreateCompletedSequence(null);
            }

            var sequence = DOTween.Sequence().SetTarget(this);
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null || renderers[i] == null) continue;

                int cardIndex = i;
                Transform card = cards[cardIndex];
                SpriteRenderer renderer = renderers[cardIndex];
                Vector3 originalScale = card.localScale;
                Vector3 destination = pileTargetPosition + new Vector3(0f, cardIndex * 0.015f, 0f);
                float startAt = cardIndex * 0.08f;

                var flight = DOTween.Sequence()
                    .InsertCallback(startAt, () => renderer.sortingOrder = flightSortingOrderBase + cardIndex)
                    .Insert(startAt, card.DOMove(destination, captureDuration).SetEase(captureEase))
                    .Insert(startAt, card.DORotate(new Vector3(0f, 0f, UnityEngine.Random.Range(-8f, 8f)), captureDuration))
                    .Insert(startAt, card.DOScale(originalScale * 0.6f, captureDuration));
                sequence.Join(flight);
            }

            GameAudio.Play(SoundId.CardCapture);
            return Paced(sequence);
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
            return Paced(sequence);
        }

        public void KillTweensOn(Transform cardTransform)
        {
            if (cardTransform != null)
            {
                cardTransform.DOKill();
            }
        }

        /// <summary>Distribuzione: suono breve per poche carte, lungo se l'animazione dura piu' di un secondo.</summary>
        private void PlayDealSound(int cardCount, float stagger)
        {
            if (cardCount <= 0) return;
            float seconds = GamePreferences.Scaled(Mathf.Max(0, cardCount - 1) * stagger + dealRevealDuration);
            GameAudio.Play(seconds > 1f ? SoundId.CardDealLong : SoundId.CardDeal);
        }

        /// <summary>Impostazioni in partita: con "Animazioni veloci" la sequenza gira piu' veloce.</summary>
        private static Sequence Paced(Sequence sequence)
        {
            sequence.timeScale = GamePreferences.AnimationSpeed;
            return sequence;
        }

        private static Sequence CreateCompletedSequence(Action onComplete)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.AppendCallback(() => onComplete?.Invoke());
            return sequence;
        }
    }
}
