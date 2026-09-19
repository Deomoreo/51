using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Banner parametrico per un giocatore al tavolo (nome, punteggio, stato turno).
    /// Una sola classe per i 4 slot (Locale/Sinistra/Alto/Destra); i dati reali arrivano
    /// da PlayerBannerManager, questo componente si limita a mostrarli.
    /// </summary>
    public class PlayerBanner : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private GameObject turnGlow;
        [SerializeField] private GameObject turnLabel;
        [Tooltip("Chip 'MAZZIERE', mostrato brevemente a inizio smazzata da TurnController (animazione di dichiarazione del dealer).")]
        [SerializeField] private GameObject dealerLabel;

        [Header("Scope dietro il banner (Assets/UI_SPEC_Tavolo.md, sezione 4)")]
        [Tooltip("Fino a 4 slot carta miniatura, gia' posizionati in ordine da TablePlayerBannersBuilder; qui vengono solo mostrati/nascosti e centrati in base al conteggio.")]
        [SerializeField] private Image[] scopeCardSlots = new Image[0];
        [SerializeField] private GameObject scopeBadge;
        [SerializeField] private TMP_Text scopeBadgeText;

        private const float ScopeSlotStep = 36f; // 54px carta - 18px overlap (sezione 4)

        // Mazzetto prese accanto al banner (mockup 09_tavolo_v4): due dorsi sfalsati + cerchio
        // con il numero di carte prese. Creato al primo uso, posizionato dal PlayerBannerManager.
        private RectTransform capturedPile;
        private Image capturedBackFront;
        private Image capturedBackRear;
        private TMP_Text capturedCountText;

        public void SetCapturedPile(int count, Sprite cardBack, Vector2 designOffsetFromCenter, Sprite roundedFill)
        {
            if (count <= 0)
            {
                if (capturedPile != null) capturedPile.gameObject.SetActive(false);
                return;
            }

            if (capturedPile == null)
            {
                capturedPile = NewChild("CapturedPile", transform, new Vector2(56f, 72f));
                capturedBackRear = NewChild("BackRear", capturedPile, new Vector2(44f, 62f)).gameObject.AddComponent<Image>();
                capturedBackRear.rectTransform.anchoredPosition = new Vector2(-4f, 4f);
                capturedBackRear.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 6f);
                capturedBackFront = NewChild("BackFront", capturedPile, new Vector2(44f, 62f)).gameObject.AddComponent<Image>();

                var badge = NewChild("CountBadge", capturedPile, new Vector2(40f, 40f));
                badge.anchoredPosition = new Vector2(12f, -26f);
                // Cerchio oro sotto e cerchio blu notte sopra, 3px piu' piccolo: anello oro come nel mockup.
                var ring = badge.gameObject.AddComponent<Image>();
                ring.color = new Color32(232, 178, 74, 255);
                var fill = NewChild("Fill", badge, new Vector2(34f, 34f)).gameObject.AddComponent<Image>();
                fill.color = new Color32(14, 28, 48, 255);
                SetCircle(ring, roundedFill, 40f);
                SetCircle(fill, roundedFill, 34f);

                capturedCountText = NewChild("Count", badge, new Vector2(40f, 40f)).gameObject.AddComponent<TextMeshProUGUI>();
                if (nameText != null) capturedCountText.font = nameText.font;
                capturedCountText.fontSize = 20f;
                capturedCountText.fontStyle = FontStyles.Bold;
                capturedCountText.alignment = TextAlignmentOptions.Center;
                capturedCountText.color = Color.white;
                capturedCountText.raycastTarget = false;
            }

            capturedPile.gameObject.SetActive(true);
            capturedPile.anchoredPosition = new Vector2(designOffsetFromCenter.x, -designOffsetFromCenter.y);
            capturedBackFront.sprite = cardBack;
            capturedBackRear.sprite = cardBack;
            capturedBackFront.preserveAspect = capturedBackRear.preserveAspect = true;
            capturedBackRear.gameObject.SetActive(count > 1);
            capturedCountText.text = count.ToString();
        }

        private static RectTransform NewChild(string name, Transform parent, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>panel_fill_r24 (PanelsNeutral_v2) a 9-slice con raggio pari a meta' lato = cerchio.</summary>
        private static void SetCircle(Image image, Sprite roundedFill, float diameter)
        {
            image.raycastTarget = false;
            if (roundedFill == null) return;
            image.sprite = roundedFill;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 48f / (diameter * 0.5f);
        }

        public void SetName(string playerName)
        {
            if (nameText != null) nameText.text = playerName;
        }

        public void SetScore(int score)
        {
            if (scoreText != null) scoreText.text = $"{score} punti";
        }

        public void SetTurnActive(bool active)
        {
            if (turnGlow != null) turnGlow.SetActive(active);
            if (turnLabel != null) turnLabel.SetActive(active);
        }

        /// <summary>
        /// Mostra/nasconde la chip "MAZZIERE" (animazione di dichiarazione dealer a inizio
        /// smazzata, orchestrata da TurnController - non un indicatore persistente).
        /// </summary>
        public void SetDealerIndicator(bool active)
        {
            if (dealerLabel != null) dealerLabel.SetActive(active);
        }

        /// <summary>
        /// Mostra fino a 4 carte scope reali (miniatura) dietro il banner. Oltre le 4,
        /// mostra un badge "+N" invece di continuare ad aggiungere carte (sezione 4).
        /// </summary>
        public void SetScopeCards(IReadOnlyList<Sprite> scopeSprites)
        {
            if (scopeCardSlots == null || scopeCardSlots.Length == 0) return;

            int totalCount = scopeSprites?.Count ?? 0;
            int visibleCards = Mathf.Min(totalCount, scopeCardSlots.Length);
            bool showBadge = totalCount > scopeCardSlots.Length;
            int totalItems = visibleCards + (showBadge ? 1 : 0);
            float startX = -(totalItems - 1) * ScopeSlotStep * 0.5f;

            for (int i = 0; i < scopeCardSlots.Length; i++)
            {
                var slot = scopeCardSlots[i];
                if (slot == null) continue;

                bool visible = i < visibleCards;
                slot.gameObject.SetActive(visible);
                if (visible)
                {
                    slot.sprite = scopeSprites[i];
                    var rt = slot.rectTransform;
                    rt.anchoredPosition = new Vector2(startX + i * ScopeSlotStep, rt.anchoredPosition.y);
                    rt.SetSiblingIndex(i);
                }
            }

            if (scopeBadge != null)
            {
                scopeBadge.SetActive(showBadge);
                if (showBadge)
                {
                    var rt = scopeBadge.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = new Vector2(startX + visibleCards * ScopeSlotStep, rt.anchoredPosition.y);
                    if (scopeBadgeText != null) scopeBadgeText.text = $"+{totalCount - scopeCardSlots.Length}";
                }
            }
        }
    }
}
