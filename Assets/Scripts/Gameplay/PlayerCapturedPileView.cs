using UnityEngine;
using Project51.Core;
using System.Collections.Generic;

namespace Project51.Unity
{
    public class PlayerCapturedPileView : MonoBehaviour
    {
        [Header("References")]
        public Transform normalCapturesContainer;
        public Transform scopeContainer;
        [SerializeField] private Sprite cardBackSprite;

        [Header("Layout Settings")]
        [SerializeField] private float cardStackOffset = 0.05f;
        [SerializeField] private float scopeCardSpacing = 0.35f;
        [SerializeField] private float scopeExpandedSpacing = 1.0f;
        [SerializeField] private float expandAnimationSpeed = 8f;
        [SerializeField] private float scopeVerticalOffset = 0.1f;

        [Header("Visual Settings")]
        [SerializeField] private Vector3 normalPileScale = new Vector3(0.6f, 0.6f, 1f);
        [SerializeField] private Vector3 scopePileScale = new Vector3(0.6f, 0.6f, 1f);
        [SerializeField] private Color scopeCardTint = Color.white;

        private GameObject normalPileCardView;
        private List<GameObject> scopeCardViews = new List<GameObject>();
        private bool isHoveringScope = false;
        private float currentTargetSpacing = 0f;
        private float lastHoverChangeTime = 0f;

        private Transform NormalContainer => normalCapturesContainer ?? transform.Find("NormalCapturesContainer");
        private Transform ScopeContainer => scopeContainer ?? transform.Find("ScopeContainer");

        private void Awake()
        {
            if (cardBackSprite == null)
            {
                cardBackSprite = Resources.Load<Sprite>("Cards/CardBack");
            }
        }

        public void UpdatePile(PlayerState player)
        {
            if (player == null) return;
            UpdateNormalPile(player.CapturedCards.Count);
            UpdateScopePile(player.ScopaCount);
        }

        private void UpdateNormalPile(int totalCaptured)
        {
            var container = NormalContainer;
            if (container == null) return;

            if (totalCaptured > 0)
            {
                if (normalPileCardView == null)
                {
                    normalPileCardView = CreateCardView(container, cardBackSprite, normalPileScale, false);
                }
            }
            else
            {
                if (normalPileCardView != null)
                {
                    Destroy(normalPileCardView);
                    normalPileCardView = null;
                }
            }
        }

        private void UpdateScopePile(int scopaCount)
        {
            var container = ScopeContainer;
            if (container == null) return;

            while (scopeCardViews.Count < scopaCount)
            {
                var scopeCard = CreateCardView(container, GetScopaMarkerSprite(), scopePileScale, true);
                if (scopeCard != null)
                {
                    scopeCardViews.Add(scopeCard);
                    AddHoverDetection(scopeCard);
                }
            }

            while (scopeCardViews.Count > scopaCount)
            {
                var lastCard = scopeCardViews[scopeCardViews.Count - 1];
                scopeCardViews.RemoveAt(scopeCardViews.Count - 1);
                if (lastCard != null) Destroy(lastCard);
            }

            UpdateScopePositions();
        }

        public void UpdateScopePileWithCards(List<Card> scopeCards)
        {
            var container = ScopeContainer;
            if (container == null) return;

            foreach (var card in scopeCardViews)
            {
                if (card != null) Destroy(card);
            }
            scopeCardViews.Clear();

            for (int i = 0; i < scopeCards.Count; i++)
            {
                var sprite = GetSpriteForCard(scopeCards[i]) ?? GetScopaMarkerSprite();
                var scopeCard = CreateCardView(container, sprite, scopePileScale, true);
                
                if (scopeCard != null)
                {
                    float spacing = isHoveringScope ? scopeExpandedSpacing : scopeCardSpacing;
                    float xPos = i * spacing;
                    float yOffset = (i % 2 == 0) ? scopeVerticalOffset : -scopeVerticalOffset;
                    
                    scopeCard.transform.localPosition = new Vector3(xPos, yOffset, 0);
                    
                    var sr = scopeCard.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = 5 + i;
                    
                    scopeCardViews.Add(scopeCard);
                    AddHoverDetection(scopeCard);
                }
            }

            currentTargetSpacing = isHoveringScope ? scopeExpandedSpacing : scopeCardSpacing;
        }

        private Sprite GetSpriteForCard(Card card)
        {
            string suitName = card.Suit.ToString();
            int rank = card.Rank;

            var candidates = new List<string>
            {
                $"Cards/{suitName}_{rank}",
                $"Cards/{suitName}{rank}",
                $"Cards/{suitName.ToLower()}_{rank}",
                $"Cards/{suitName.ToLower()}{rank}"
            };

            if (rank == 1) candidates.Add($"Cards/{suitName}_Asso");
            else if (rank == 8) candidates.Add($"Cards/{suitName}_Fante");
            else if (rank == 9) candidates.Add($"Cards/{suitName}_Cavallo");
            else if (rank == 10) candidates.Add($"Cards/{suitName}_Re");

            foreach (var path in candidates)
            {
                var sprite = Resources.Load<Sprite>(path);
                if (sprite != null) return sprite;
            }

            return null;
        }

        private GameObject CreateCardView(Transform parent, Sprite sprite, Vector3 scale, bool isScopa)
        {
            if (sprite == null) return null;

            var cardObj = new GameObject(isScopa ? "ScopaCard" : "NormalPileCard");
            cardObj.transform.SetParent(parent, false);
            cardObj.transform.localScale = scale;
            cardObj.transform.localPosition = Vector3.zero;

            var sr = cardObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 5;
            if (isScopa) sr.color = scopeCardTint;

            if (!isScopa && parent.childCount > 1)
            {
                cardObj.transform.localPosition = Vector3.right * cardStackOffset * (parent.childCount - 1);
            }

            return cardObj;
        }

        private Sprite GetScopaMarkerSprite()
        {
            var scopaMarker = Resources.Load<Sprite>("Cards/ScopaMarker");
            if (scopaMarker != null) return scopaMarker;
            
            var aceSprite = Resources.Load<Sprite>("Cards/Denari_1");
            if (aceSprite != null) return aceSprite;
            
            return cardBackSprite;
        }

        private void AddHoverDetection(GameObject cardObj)
        {
            if (cardObj == null) return;

            var collider = cardObj.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = cardObj.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(1.5f, 2.0f);
            }

            var hoverScript = cardObj.AddComponent<ScopaCardHover>();
            hoverScript.OnHoverEnter = () => SetHoverState(true);
            hoverScript.OnHoverExit = () => SetHoverState(false);
        }

        private void SetHoverState(bool hovering)
        {
            float timeSinceLastChange = Time.time - lastHoverChangeTime;
            float debounceThreshold = hovering ? 0.01f : 0.15f;
            
            if (timeSinceLastChange < debounceThreshold) return;

            if (isHoveringScope != hovering)
            {
                isHoveringScope = hovering;
                lastHoverChangeTime = Time.time;
            }
        }

        private void UpdateScopePositions()
        {
            if (scopeCardViews.Count == 0) return;

            float targetSpacing = isHoveringScope ? scopeExpandedSpacing : scopeCardSpacing;
            bool needsUpdate = Mathf.Abs(targetSpacing - currentTargetSpacing) > 0.001f;
            
            for (int i = 0; i < scopeCardViews.Count; i++)
            {
                if (scopeCardViews[i] == null) continue;

                float xPos = i * targetSpacing;
                float yOffset = (i % 2 == 0) ? scopeVerticalOffset : -scopeVerticalOffset;
                Vector3 targetPos = new Vector3(xPos, yOffset, 0);
                
                if (needsUpdate)
                {
                    scopeCardViews[i].transform.localPosition = Vector3.Lerp(
                        scopeCardViews[i].transform.localPosition,
                        targetPos,
                        Time.deltaTime * expandAnimationSpeed
                    );
                }
                else
                {
                    scopeCardViews[i].transform.localPosition = targetPos;
                }
                
                var sr = scopeCardViews[i].GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = 5 + i;
            }

            if (needsUpdate)
                currentTargetSpacing = Mathf.Lerp(currentTargetSpacing, targetSpacing, Time.deltaTime * expandAnimationSpeed);
            else
                currentTargetSpacing = targetSpacing;
        }

        private void Update()
        {
            if (scopeCardViews.Count > 0)
            {
                UpdateScopePositions();
            }
        }

        public void Clear()
        {
            if (normalPileCardView != null)
            {
                Destroy(normalPileCardView);
                normalPileCardView = null;
            }

            foreach (var scopeCard in scopeCardViews)
            {
                if (scopeCard != null) Destroy(scopeCard);
            }
            scopeCardViews.Clear();
        }
    }

    public class ScopaCardHover : MonoBehaviour
    {
        public System.Action OnHoverEnter;
        public System.Action OnHoverExit;

        private bool isMouseOver = false;
        private Coroutine exitDelayCoroutine;

        private void OnMouseEnter()
        {
            if (exitDelayCoroutine != null)
            {
                StopCoroutine(exitDelayCoroutine);
                exitDelayCoroutine = null;
            }

            if (!isMouseOver)
            {
                isMouseOver = true;
                OnHoverEnter?.Invoke();
            }
        }

        private void OnMouseExit()
        {
            if (exitDelayCoroutine == null)
            {
                exitDelayCoroutine = StartCoroutine(DelayedExit());
            }
        }

        private System.Collections.IEnumerator DelayedExit()
        {
            yield return new WaitForSeconds(0.15f);

            if (isMouseOver)
            {
                isMouseOver = false;
                OnHoverExit?.Invoke();
            }

            exitDelayCoroutine = null;
        }

        private void OnDisable()
        {
            if (exitDelayCoroutine != null)
            {
                StopCoroutine(exitDelayCoroutine);
                exitDelayCoroutine = null;
            }
        }
    }
}
