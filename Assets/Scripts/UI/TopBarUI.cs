using TMPro;
using UnityEngine;

namespace Project51.Unity
{
    public class TopBarUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI gemsText;
        [SerializeField] private TextMeshProUGUI levelText;

        private PlayerData playerData;

        public void Initialize(PlayerDataProvider playerDataProvider)
        {
            if (playerDataProvider == null)
            {
                Debug.LogError("[TopBarUI] PlayerDataProvider is NULL. Ensure it is assigned before initialization.");
                return;
            }

            playerData = playerDataProvider.Data;
            if (playerData == null)
            {
                Debug.LogError("[TopBarUI] PlayerData is NULL. Ensure PlayerDataProvider is initialized correctly.");
                return;
            }

            playerData.OnChanged += UpdateUI;
            UpdateUI();
        }

        private void OnDestroy()
        {
            if (playerData != null) playerData.OnChanged -= UpdateUI;
        }

        public void UpdateUI()
        {
            if (playerData == null)
            {
                Debug.LogError("[TopBarUI] Cannot update UI. PlayerData is NULL.");
                return;
            }

            if (goldText != null) goldText.text = playerData.Gold.ToString();
            if (gemsText != null) gemsText.text = playerData.Gems.ToString();
            if (levelText != null) levelText.text = "Livello " + playerData.Level;
        }

        public void SetTitle(string title)
        {
            if (titleText != null) titleText.text = title;
        }
    }
}
