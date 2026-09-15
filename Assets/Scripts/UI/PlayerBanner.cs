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
