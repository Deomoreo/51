using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project51.Core;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Simple UI indicator showing whose turn it is.
    /// Uses TextMeshPro for text display.
    /// </summary>
    public class TurnIndicator : MonoBehaviour
    {
        [Header("Text Component (TextMeshPro)")]
        [SerializeField] private TMP_Text turnText;
        
        [Header("Colors")]
        [SerializeField] private Color yourTurnColor = new Color(0.2f, 1f, 0.2f); // Green
        [SerializeField] private Color otherTurnColor = Color.white;

        private TurnController turnController;

        private void Awake()
        {
            // Try to find TMP_Text component if not assigned
            if (turnText == null)
            {
                turnText = GetComponent<TMP_Text>();
            }
            
            // Create one if needed
            if (turnText == null)
            {
                turnText = gameObject.AddComponent<TextMeshProUGUI>();
                turnText.alignment = TextAlignmentOptions.Center;
                turnText.fontSize = 36;
                turnText.color = Color.white;
            }
        }

        private void Start()
        {
            // Find TurnController
            turnController = FindObjectOfType<TurnController>();
            
            // Update periodically
            InvokeRepeating(nameof(UpdateIndicator), 0.2f, 0.2f);
        }

        private void UpdateIndicator()
        {
            if (turnText == null) return;
            
            if (turnController == null || turnController.GameState == null)
            {
                turnText.text = "";
                return;
            }

            int current = turnController.CurrentPlayerIndex;
            if (current < 0)
            {
                turnText.text = "";
                return;
            }

            // Resolve display name using GameModeService
            var provider = GameModeService.Current;
            string displayName;
            
            if (provider.IsHumanPlayer(current))
            {
                if (provider.IsLocalPlayer(current))
                {
                    displayName = "Tu";
                }
                else
                {
                    displayName = $"Giocatore {current + 1}";
                }
            }
            else
            {
                displayName = $"Bot {current + 1}";
            }

            // Check if local player's turn
            bool isLocalTurn = provider.IsLocalPlayer(current);

            // Update text and color
            if (isLocalTurn)
            {
                turnText.text = $"<b>Il tuo turno!</b>";
                turnText.color = yourTurnColor;
            }
            else
            {
                turnText.text = $"Turno: {displayName}";
                turnText.color = otherTurnColor;
            }
        }
    }
}
