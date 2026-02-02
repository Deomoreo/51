using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    public class TopBarUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI gemsText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI xpText;

        [Header("Shop (optional)")]
        [SerializeField] private ModalWindowBaseUI shopModal;
        [SerializeField] private Button goldPlusButton;
        [SerializeField] private Button gemsPlusButton;

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

            HookButtons();
        }

        private void OnEnable()
        {
            HookButtons();
        }

        private void HookButtons()
        {
            if (goldPlusButton != null)
            {
                goldPlusButton.onClick.RemoveListener(OnGoldPlusClicked);
                goldPlusButton.onClick.AddListener(OnGoldPlusClicked);
            }

            if (gemsPlusButton != null)
            {
                gemsPlusButton.onClick.RemoveListener(OnGemsPlusClicked);
                gemsPlusButton.onClick.AddListener(OnGemsPlusClicked);
            }
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
            if (levelText != null) levelText.text = "" + playerData.Level;
            if (xpText != null)
            {
                if (playerData.NextLevelXp > 0)
                    xpText.text = $"{playerData.CurrentXp}/{playerData.NextLevelXp}";
                else
                    xpText.text = playerData.CurrentXp.ToString();
            }
        }

        public void SetTitle(string title)
        {
            if (titleText != null) titleText.text = title;
        }

        public void OnGoldPlusClicked()
        {
            OpenShop();
        }

        public void OnGemsPlusClicked()
        {
            OpenShop();
        }

        private void OpenShop()
        {
            if (shopModal == null)
            {
                Debug.LogWarning("[TopBarUI] Shop modal not assigned.");
                return;
            }

            if (ModalManager.Instance == null)
            {
                Debug.LogWarning("[TopBarUI] ModalManager.Instance not found in scene.");
                shopModal.Open();
                return;
            }

            ModalManager.Instance.Open(shopModal);
        }
    }
}
