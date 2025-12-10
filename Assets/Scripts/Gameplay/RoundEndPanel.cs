using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project51.Core;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Unity
{
    /// <summary>
    /// UI Panel that displays the end-of-round (smazzata) scores.
    /// Shows detailed breakdown of points for each player.
    /// </summary>
    public class RoundEndPanel : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform playerScoresContainer;
        [SerializeField] private GameObject playerScoreRowPrefab;
        
        [Header("Summary Text (TextMeshPro)")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text winnerText;
        
        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button mainMenuButton;
        
        [Header("Animation")]
        [SerializeField] private float showDelay = 0.5f;
        [SerializeField] private float rowAnimationDelay = 0.15f;

        [Header("Visual Settings")]
        [SerializeField] private float titleFontSize = 48f;
        [SerializeField] private float winnerFontSize = 36f;
        [SerializeField] private Color winnerTextColor = new Color(1f, 0.84f, 0f);

        // Events
        public event System.Action OnContinueClicked;
        public event System.Action OnMainMenuClicked;

        private List<GameObject> spawnedRows = new List<GameObject>();

        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (continueButton != null)
                continueButton.onClick.AddListener(() => OnContinueClicked?.Invoke());

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(() => OnMainMenuClicked?.Invoke());

            // Configure title text
            if (titleText != null)
            {
                titleText.fontSize = titleFontSize;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.Center;
            }

            // Configure winner text
            if (winnerText != null)
            {
                winnerText.fontSize = winnerFontSize;
                winnerText.fontStyle = FontStyles.Bold;
                winnerText.alignment = TextAlignmentOptions.Center;
                winnerText.color = winnerTextColor;
            }
        }

        /// <summary>
        /// Shows the round end panel with scores from the given GameState.
        /// </summary>
        public void Show(GameState gameState)
        {
            if (gameState == null)
            {
                Debug.LogError("RoundEndPanel: Cannot show with null GameState");
                return;
            }

            StartCoroutine(ShowWithAnimation(gameState));
        }

        private System.Collections.IEnumerator ShowWithAnimation(GameState gameState)
        {
            // Clear previous rows
            ClearRows();

            // Calculate scores
            var scores = PunteggioManager.CalculateSmazzataScores(gameState);
            var scoreDetails = CalculateScoreDetails(gameState);

            // ALWAYS log detailed scores to console for debugging/verification
            Debug.Log("========== FINE SMAZZATA - PUNTEGGI DETTAGLIATI ==========");
            for (int i = 0; i < gameState.NumPlayers; i++)
            {
                Debug.Log($"\n<b>Giocatore {i} ({GetPlayerName(i)}): {scores[i]} punti totali</b>");
                var d = scoreDetails[i];
                if (d.ScopaCount > 0) Debug.Log($"  Scope: {d.ScopaCount}");
                if (d.HasSetteBello) Debug.Log($"  7 Bello: SI");
                if (d.DenariCount > 0) Debug.Log($"  Denari: {d.DenariCount}{(d.WonDenari ? " (VINTO +1)" : "")}");
                if (d.CardCount > 0) Debug.Log($"  Carte: {d.CardCount}{(d.WonCards ? " (VINTO +1)" : "")}");
                if (d.PrimieraScore > 0) Debug.Log($"  Primiera: {d.PrimieraScore}{(d.WonPrimiera ? " (VINTO +1)" : "")}");
                if (d.HasGrande) Debug.Log($"  Grande: +5");
                if (d.HasPiccola) Debug.Log($"  Piccola: +{3 + d.PiccolaExtras}");
                if (d.AccusiPoints > 0) Debug.Log($"  Accusi: +{d.AccusiPoints}");
            }
            Debug.Log("==========================================================\n");

            // Set title with better formatting
            if (titleText != null)
            {
                titleText.text = "FINE SMAZZATA";
            }

            // Show panel
            if (panelRoot != null)
                panelRoot.SetActive(true);

            yield return new WaitForSeconds(showDelay);

            // Determine winner(s) first
            int maxScore = scores.Max();
            var winners = new List<int>();
            for (int i = 0; i < scores.Length; i++)
            {
                if (scores[i] == maxScore)
                    winners.Add(i);
            }

            // Create rows for each player (winners first for better visibility)
            var playerIndices = Enumerable.Range(0, gameState.NumPlayers).OrderByDescending(i => scores[i]).ToList();
            
            for (int idx = 0; idx < playerIndices.Count; idx++)
            {
                int i = playerIndices[idx];
                bool isWinner = winners.Contains(i);
                CreatePlayerRow(i, gameState.Players[i], scores[i], scoreDetails[i], isWinner);
                yield return new WaitForSeconds(rowAnimationDelay);
            }

            // Show winner text with improved formatting
            if (winnerText != null)
            {
                if (winners.Count == 1)
                {
                    string playerName = GetPlayerName(winners[0]);
                    winnerText.text = $"{playerName.ToUpper()} VINCE LA SMAZZATA\n<size=80%>{maxScore} punti totali</size>";
                }
                else
                {
                    var winnerNames = string.Join(" e ", winners.Select(w => GetPlayerName(w).ToUpper()));
                    winnerText.text = $"PAREGGIO\n<size=80%>{winnerNames} - {maxScore} punti</size>";
                }
            }
        }

        /// <summary>
        /// Hides the panel.
        /// </summary>
        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            ClearRows();
        }

        private void ClearRows()
        {
            foreach (var row in spawnedRows)
            {
                if (row != null)
                    Destroy(row);
            }
            spawnedRows.Clear();
        }

        private void CreatePlayerRow(int playerIndex, PlayerState player, int totalScore, ScoreDetails details, bool isWinner)
        {
            if (playerScoresContainer == null || playerScoreRowPrefab == null)
            {
                // Fallback: log to console if no UI prefab with detailed breakdown
                Debug.Log($"=== Player {playerIndex} === {totalScore} pts");
                if (details.ScopaCount > 0) Debug.Log($"  Scope: {details.ScopaCount}");
                if (details.HasSetteBello) Debug.Log($"  7 Bello: SI");
                if (details.DenariCount > 0) Debug.Log($"  Denari: {details.DenariCount}{(details.WonDenari ? " (+1)" : "")}");
                if (details.CardCount > 0) Debug.Log($"  Carte: {details.CardCount}{(details.WonCards ? " (+1)" : "")}");
                if (details.PrimieraScore > 0) Debug.Log($"  Primiera: {details.PrimieraScore}{(details.WonPrimiera ? " (+1)" : "")}");
                if (details.HasGrande) Debug.Log($"  Grande: +5");
                if (details.HasPiccola) Debug.Log($"  Piccola: +{3 + details.PiccolaExtras}");
                if (details.AccusiPoints > 0) Debug.Log($"  Accusi: +{details.AccusiPoints}");
                return;
            }

            var row = Instantiate(playerScoreRowPrefab, playerScoresContainer);
            spawnedRows.Add(row);

            // Try to find and populate UI elements
            var rowUI = row.GetComponent<PlayerScoreRow>();
            if (rowUI != null)
            {
                rowUI.Setup(GetPlayerName(playerIndex), totalScore, details, IsLocalPlayer(playerIndex));
                
                // Highlight winner
                if (isWinner)
                {
                    rowUI.SetAsWinner();
                }
            }
            else
            {
                // Fallback: map by child names to avoid dumping all details in one field
                var textsByName = row.GetComponentsInChildren<TMP_Text>(true)
                                     .ToDictionary(t => t.gameObject.name, t => t);

                TMP_Text nameText;
                if (textsByName.TryGetValue("PlayerNameText", out nameText))
                {
                    nameText.text = GetPlayerName(playerIndex);
                    nameText.fontSize = 28;
                    nameText.fontStyle = FontStyles.Bold;
                }
                TMP_Text totalScoreText;
                if (textsByName.TryGetValue("TotalScoreText", out totalScoreText))
                {
                    totalScoreText.text = $"<b>{totalScore}</b> punti";
                    totalScoreText.fontSize = 24;
                }

                // Category fields: set ONLY their specific values
                TMP_Text scopeText;
                if (textsByName.TryGetValue("ScopeText", out scopeText))
                {
                    scopeText.text = details.ScopaCount > 0 ? details.ScopaCount.ToString() : string.Empty;
                    scopeText.gameObject.SetActive(details.ScopaCount > 0);
                }
                TMP_Text setteBelloText;
                if (textsByName.TryGetValue("SetteBelloText", out setteBelloText))
                {
                    setteBelloText.text = details.HasSetteBello ? "V" : string.Empty;
                    setteBelloText.gameObject.SetActive(details.HasSetteBello);
                }
                TMP_Text denariText;
                if (textsByName.TryGetValue("DenariText", out denariText))
                {
                    denariText.text = details.DenariCount > 0 ? details.DenariCount.ToString() : string.Empty;
                    denariText.color = details.WonDenari ? new Color(0.3f, 1f, 0.3f) : new Color(0.8f, 0.8f, 0.8f);
                    denariText.gameObject.SetActive(details.DenariCount > 0);
                }
                TMP_Text carteText;
                if (textsByName.TryGetValue("CarteText", out carteText))
                {
                    carteText.text = details.CardCount > 0 ? details.CardCount.ToString() : string.Empty;
                    carteText.color = details.WonCards ? new Color(0.3f, 1f, 0.3f) : new Color(0.8f, 0.8f, 0.8f);
                    carteText.gameObject.SetActive(details.CardCount > 0);
                }
                TMP_Text primieraText;
                if (textsByName.TryGetValue("PrimieraText", out primieraText))
                {
                    primieraText.text = details.PrimieraScore > 0 ? details.PrimieraScore.ToString() : string.Empty;
                    primieraText.color = details.WonPrimiera ? new Color(0.3f, 1f, 0.3f) : new Color(0.8f, 0.8f, 0.8f);
                    primieraText.gameObject.SetActive(details.PrimieraScore > 0);
                }
                TMP_Text grandeText;
                if (textsByName.TryGetValue("GrandeText", out grandeText))
                {
                    grandeText.text = details.HasGrande ? "+5" : string.Empty;
                    grandeText.gameObject.SetActive(details.HasGrande);
                }
                TMP_Text piccolaText;
                if (textsByName.TryGetValue("PiccolaText", out piccolaText))
                {
                    piccolaText.text = details.HasPiccola ? $"+{3 + details.PiccolaExtras}" : string.Empty;
                    piccolaText.gameObject.SetActive(details.HasPiccola);
                }
                TMP_Text accusiText;
                if (textsByName.TryGetValue("AccusiText", out accusiText))
                {
                    accusiText.text = details.AccusiPoints > 0 ? $"+{details.AccusiPoints}" : string.Empty;
                    accusiText.gameObject.SetActive(details.AccusiPoints > 0);
                }
            }
        }

        private string GetPlayerName(int playerIndex)
        {
            var provider = GameModeService.Current;
            
            if (provider.IsHumanPlayer(playerIndex))
            {
                if (provider.IsLocalPlayer(playerIndex))
                    return "Tu";
                return $"Giocatore {playerIndex + 1}";
            }
            else
            {
                return $"Bot {playerIndex + 1}";
            }
        }

        private bool IsLocalPlayer(int playerIndex)
        {
            return GameModeService.Current.IsLocalPlayer(playerIndex);
        }

        /// <summary>
        /// Calculates detailed score breakdown for each player.
        /// </summary>
        private ScoreDetails[] CalculateScoreDetails(GameState state)
        {
            int n = state.NumPlayers;
            var details = new ScoreDetails[n];

            for (int i = 0; i < n; i++)
            {
                details[i] = new ScoreDetails();
                var player = state.Players[i];
                var captured = player.CapturedCards;

                // Scopa count
                details[i].ScopaCount = player.ScopaCount;

                // Sette Bello
                details[i].HasSetteBello = captured.Any(c => c.IsSetteBello);

                // Card count
                details[i].CardCount = captured.Count;

                // Denari count
                details[i].DenariCount = captured.Count(c => c.Suit == Suit.Denari);

                // Primiera score
                details[i].PrimieraScore = ComputePrimieraScore(captured);

                // Grande (Re, Cavallo, Fante di Denari)
                var denariRanks = captured.Where(c => c.Suit == Suit.Denari).Select(c => c.Rank).ToHashSet();
                details[i].HasGrande = denariRanks.Contains(10) && denariRanks.Contains(9) && denariRanks.Contains(8);

                // Piccola (Asso, 2, 3 di Denari)
                details[i].HasPiccola = denariRanks.Contains(1) && denariRanks.Contains(2) && denariRanks.Contains(3);
                if (details[i].HasPiccola)
                {
                    details[i].PiccolaExtras = 0;
                    for (int r = 4; r <= 6; r++)
                        if (denariRanks.Contains(r)) details[i].PiccolaExtras++;
                }

                // Accusi points
                details[i].AccusiPoints = player.AccusiPoints;
            }

            // Determine winners for comparative categories
            int[] denariCounts = details.Select(d => d.DenariCount).ToArray();
            int[] cardCounts = details.Select(d => d.CardCount).ToArray();
            int[] primieraScores = details.Select(d => d.PrimieraScore).ToArray();

            int maxDenari = denariCounts.Max();
            int maxCards = cardCounts.Max();
            int maxPrimiera = primieraScores.Max();

            for (int i = 0; i < n; i++)
            {
                details[i].WonDenari = maxDenari >= 6 && denariCounts[i] == maxDenari && denariCounts.Count(x => x == maxDenari) == 1;
                details[i].WonCards = maxCards >= 21 && cardCounts[i] == maxCards && cardCounts.Count(x => x == maxCards) == 1;
                details[i].WonPrimiera = primieraScores[i] == maxPrimiera && primieraScores.Count(x => x == maxPrimiera) == 1;
            }

            return details;
        }

        private int ComputePrimieraScore(List<Card> cards)
        {
            var suits = System.Enum.GetValues(typeof(Suit)).Cast<Suit>();
            int total = 0;
            foreach (var s in suits)
            {
                var best = cards.Where(c => c.Suit == s).Select(c => c.PrimieraValue).DefaultIfEmpty(0).Max();
                total += best;
            }
            return total;
        }
    }

    /// <summary>
    /// Detailed score breakdown for a single player.
    /// </summary>
    public class ScoreDetails
    {
        public int ScopaCount;
        public bool HasSetteBello;
        public int CardCount;
        public int DenariCount;
        public int PrimieraScore;
        public bool HasGrande;
        public bool HasPiccola;
        public int PiccolaExtras;
        public int AccusiPoints;

        // Winners (determined after comparing all players)
        public bool WonDenari;
        public bool WonCards;
        public bool WonPrimiera;

        public override string ToString()
        {
            var parts = new List<string>();
            if (ScopaCount > 0) parts.Add($"Scope: {ScopaCount}");
            if (HasSetteBello) parts.Add("7?");
            if (WonDenari) parts.Add($"Denari ({DenariCount})");
            if (WonCards) parts.Add($"Carte ({CardCount})");
            if (WonPrimiera) parts.Add("Primiera");
            if (HasGrande) parts.Add("Grande (+5)");
            if (HasPiccola) parts.Add($"Piccola (+{3 + PiccolaExtras})");
            if (AccusiPoints > 0) parts.Add($"Accusi: {AccusiPoints}");
            return string.Join(", ", parts);
        }
    }
}

