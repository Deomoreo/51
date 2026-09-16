using Project51.Auth;
using Project51.Core;
using Project51.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    public sealed class SettingsV2Integration : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dimmerButton;
        [SerializeField] private Button accountButton;
        [SerializeField] private SimpleToggleSwitch audioToggle;
        [SerializeField] private TMP_Text accountName;
        [SerializeField] private TMP_Text accountSubtitle;
        [SerializeField] private AuthUIController authUI;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            closeButton.onClick.AddListener(Close);
            dimmerButton.onClick.AddListener(Close);
            accountButton.onClick.AddListener(OpenAccount);
            audioToggle.OnChanged += GameAudioPreferences.SetEnabled;
            Close();
        }

        public void Open()
        {
            var auth = AuthBootstrapper.Instance?.PlayFabAuth;
            accountName.text = auth?.GetBestDisplayName() ?? "Ospite";
            accountSubtitle.text = auth != null && auth.HasRealLogin ? "Gestisci account" : "Accedi o registrati";
            audioToggle.SetOn(GameAudioPreferences.Enabled);
            panel.SetActive(true);
        }

        public void Close() => panel.SetActive(false);

        private void OpenAccount()
        {
            Close();
            if (authUI != null) authUI.ShowAuthUI();
        }

        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (dimmerButton != null) dimmerButton.onClick.RemoveListener(Close);
            if (accountButton != null) accountButton.onClick.RemoveListener(OpenAccount);
            if (audioToggle != null) audioToggle.OnChanged -= GameAudioPreferences.SetEnabled;
        }
    }
}
