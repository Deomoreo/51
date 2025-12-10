using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project51.Unity
{
    /// <summary>
    /// UI component for a single player's score row in the RoundEndPanel.
    /// Attach this to the player score row prefab.
    /// </summary>
    public class PlayerScoreRow : MonoBehaviour
    {
        [Header("Player Info (TextMeshPro)")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text totalScoreText;
        [SerializeField] private Image playerHighlight;

        [Header("Score Breakdown (TextMeshPro)")]
        [SerializeField] private TMP_Text scopeText;
        [SerializeField] private TMP_Text setteBelloText;
        [SerializeField] private TMP_Text denariText;
        [SerializeField] private TMP_Text carteText;
        [SerializeField] private TMP_Text primieraText;
        [SerializeField] private TMP_Text grandeText;
        [SerializeField] private TMP_Text piccolaText;
        [SerializeField] private TMP_Text accusiText;

        [Header("Fallback - Single Text for All Details")]
        [SerializeField] private TMP_Text allDetailsText;

        [Header("Icons (Optional)")]
        [SerializeField] private GameObject scopeIcon;
        [SerializeField] private GameObject setteBelloIcon;
        [SerializeField] private GameObject denariIcon;
        [SerializeField] private GameObject carteIcon;
        [SerializeField] private GameObject primieraIcon;
        [SerializeField] private GameObject grandeIcon;
        [SerializeField] private GameObject piccolaIcon;

        [Header("Colors")]
        [SerializeField] private Color localPlayerColor = new Color(0.2f, 0.6f, 1f, 0.4f);
        [SerializeField] private Color winnerColor = new Color(1f, 0.84f, 0f, 0.5f);
        [SerializeField] private Color normalColor = new Color(0.5f, 0.5f, 0.5f, 0.15f);
        [SerializeField] private Color wonItemColor = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private Color normalItemColor = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color disabledItemColor = new Color(0.4f, 0.4f, 0.4f);

        [Header("Font Sizes")]
        [SerializeField] private float playerNameFontSize = 24f;
        [SerializeField] private float totalScoreFontSize = 24f;
        [SerializeField] private float scoreItemFontSize = 16f;

        [Header("Column Widths (px)")]
        [SerializeField] private float scopeWidth = 60f;
        [SerializeField] private float setteWidth = 40f;
        [SerializeField] private float denariWidth = 60f;
        [SerializeField] private float carteWidth = 60f;
        [SerializeField] private float primieraWidth = 70f;
        [SerializeField] private float grandeWidth = 50f;
        [SerializeField] private float piccolaWidth = 50f;
        [SerializeField] private float accusiWidth = 60f;

        /// <summary>
        /// Setup the row with player data.
        /// </summary>
        public void Setup(string playerName, int totalScore, ScoreDetails details, bool isLocalPlayer)
        {
            // Player name with improved formatting
            if (playerNameText != null)
            {
                playerNameText.text = playerName;
                playerNameText.enableAutoSizing = false;
                playerNameText.fontSize = playerNameFontSize;
                playerNameText.fontStyle = FontStyles.Bold;
                playerNameText.alignment = TextAlignmentOptions.Left;
            }

            // Total score with bold and larger size
            if (totalScoreText != null)
            {
                totalScoreText.text = $"{totalScore} punti";
                totalScoreText.enableAutoSizing = false;
                totalScoreText.fontSize = totalScoreFontSize;
                totalScoreText.fontStyle = FontStyles.Bold;
                totalScoreText.alignment = TextAlignmentOptions.Right;
            }

            // Highlight for local player
            if (playerHighlight != null)
            {
                playerHighlight.color = isLocalPlayer ? localPlayerColor : normalColor;
            }

            // Check if we should use fallback (all details in one text)
            bool useIndividualFields = scopeText != null || setteBelloText != null || denariText != null;
            
            if (!useIndividualFields && allDetailsText != null)
            {
                // Fallback: show all details in a single text field
                SetupAllDetailsText(allDetailsText, details);
            }
            else
            {
                // Use individual text fields with clean, specific info
                SetupIndividualFields(details);
            }
        }

        /// <summary>
        /// Fallback: Display all score details in a single formatted text.
        /// </summary>
        private void SetupAllDetailsText(TMP_Text text, ScoreDetails details)
        {
            var lines = new System.Collections.Generic.List<string>();

            // Scope
            if (details.ScopaCount > 0)
            {
                lines.Add($"Scope: {details.ScopaCount}");
            }

            // Sette Bello
            if (details.HasSetteBello)
            {
                lines.Add("7 Bello: SI");
            }

            // Denari
            if (details.DenariCount > 0)
            {
                if (details.WonDenari)
                    lines.Add($"Denari: {details.DenariCount} (VINTO)");
                else
                    lines.Add($"Denari: {details.DenariCount}");
            }

            // Carte
            if (details.CardCount > 0)
            {
                if (details.WonCards)
                    lines.Add($"Carte: {details.CardCount} (VINTO)");
                else
                    lines.Add($"Carte: {details.CardCount}");
            }

            // Primiera
            if (details.PrimieraScore > 0)
            {
                if (details.WonPrimiera)
                    lines.Add($"Primiera: {details.PrimieraScore} (VINTO)");
                else
                    lines.Add($"Primiera: {details.PrimieraScore}");
            }

            // Grande
            if (details.HasGrande)
            {
                lines.Add("Grande: +5");
            }

            // Piccola
            if (details.HasPiccola)
            {
                int piccolaPoints = 3 + details.PiccolaExtras;
                lines.Add($"Piccola: +{piccolaPoints}");
            }

            // Accusi
            if (details.AccusiPoints > 0)
            {
                lines.Add($"Accusi: +{details.AccusiPoints}");
            }

            // If no items, show a message
            if (lines.Count == 0)
            {
                lines.Add("Nessun punto");
            }

            text.enableAutoSizing = false;
            text.fontSize = scoreItemFontSize;
            text.text = string.Join("\n", lines);
        }

        /// <summary>
        /// Setup individual text fields - each shows ONLY its specific data.
        /// </summary>
        private void SetupIndividualFields(ScoreDetails details)
        {
            // Tutti i campi hanno la stessa dimensione del font e allineamento centrale per tabella regolare
            ApplyUniformStyle(scopeText);
            ApplyUniformStyle(setteBelloText);
            ApplyUniformStyle(denariText);
            ApplyUniformStyle(carteText);
            ApplyUniformStyle(primieraText);
            ApplyUniformStyle(grandeText);
            ApplyUniformStyle(piccolaText);
            ApplyUniformStyle(accusiText);

            // Larghezze fisse per ogni colonna
            ApplyPreferredWidth(scopeText, scopeWidth);
            ApplyPreferredWidth(setteBelloText, setteWidth);
            ApplyPreferredWidth(denariText, denariWidth);
            ApplyPreferredWidth(carteText, carteWidth);
            ApplyPreferredWidth(primieraText, primieraWidth);
            ApplyPreferredWidth(grandeText, grandeWidth);
            ApplyPreferredWidth(piccolaText, piccolaWidth);
            ApplyPreferredWidth(accusiText, accusiWidth);

            // Solo valori (etichette nella riga intestazione del pannello)
            if (scopeText != null)
            {
                scopeText.text = details.ScopaCount > 0 ? details.ScopaCount.ToString() : "";
                scopeText.gameObject.SetActive(details.ScopaCount > 0);
                scopeText.color = wonItemColor;
            }
            if (scopeIcon != null) scopeIcon.SetActive(details.ScopaCount > 0);

            if (setteBelloText != null)
            {
                setteBelloText.text = details.HasSetteBello ? "V" : "";
                setteBelloText.gameObject.SetActive(details.HasSetteBello);
                setteBelloText.color = wonItemColor;
            }
            if (setteBelloIcon != null) setteBelloIcon.SetActive(details.HasSetteBello);

            if (denariText != null)
            {
                denariText.text = details.DenariCount > 0 ? details.DenariCount.ToString() : "";
                denariText.gameObject.SetActive(details.DenariCount > 0);
                denariText.color = details.WonDenari ? wonItemColor : normalItemColor;
            }
            if (denariIcon != null) denariIcon.SetActive(details.WonDenari);

            if (carteText != null)
            {
                carteText.text = details.CardCount > 0 ? details.CardCount.ToString() : "";
                carteText.gameObject.SetActive(details.CardCount > 0);
                carteText.color = details.WonCards ? wonItemColor : normalItemColor;
            }
            if (carteIcon != null) carteIcon.SetActive(details.WonCards);

            if (primieraText != null)
            {
                primieraText.text = details.PrimieraScore > 0 ? details.PrimieraScore.ToString() : "";
                primieraText.gameObject.SetActive(details.PrimieraScore > 0);
                primieraText.color = details.WonPrimiera ? wonItemColor : normalItemColor;
            }
            if (primieraIcon != null) primieraIcon.SetActive(details.WonPrimiera);

            if (grandeText != null)
            {
                grandeText.text = details.HasGrande ? "+5" : "";
                grandeText.gameObject.SetActive(details.HasGrande);
                grandeText.color = wonItemColor;
            }
            if (grandeIcon != null) grandeIcon.SetActive(details.HasGrande);

            if (piccolaText != null)
            {
                piccolaText.text = details.HasPiccola ? $"+{3 + details.PiccolaExtras}" : "";
                piccolaText.gameObject.SetActive(details.HasPiccola);
                piccolaText.color = wonItemColor;
            }
            if (piccolaIcon != null) piccolaIcon.SetActive(details.HasPiccola);

            if (accusiText != null)
            {
                accusiText.text = details.AccusiPoints > 0 ? $"+{details.AccusiPoints}" : "";
                accusiText.gameObject.SetActive(details.AccusiPoints > 0);
                accusiText.color = wonItemColor;
            }
        }

        private void ApplyUniformStyle(TMP_Text t)
        {
            if (t == null) return;
            t.enableAutoSizing = false;
            t.fontSize = scoreItemFontSize;
            t.alignment = TextAlignmentOptions.Center;
        }

        private void ApplyPreferredWidth(TMP_Text t, float width)
        {
            if (t == null) return;
            var le = t.GetComponent<LayoutElement>();
            if (le == null) le = t.gameObject.AddComponent<LayoutElement>();
            le.minWidth = width;
            le.preferredWidth = width;
            le.flexibleWidth = 0f;
        }

        /// <summary>
        /// Highlight this row as the winner with improved visual feedback.
        /// </summary>
        public void SetAsWinner()
        {
            if (playerHighlight != null)
            {
                playerHighlight.color = winnerColor;
            }

            // Make player name even more prominent for winner
            if (playerNameText != null)
            {
                playerNameText.text = $"VINCITORE: {playerNameText.text}";
            }

            // Make total score golden
            if (totalScoreText != null)
            {
                totalScoreText.color = new Color(1f, 0.84f, 0f);
            }
        }
    }
}

