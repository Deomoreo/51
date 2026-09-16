using Project51.Auth;
using Project51.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    [DefaultExecutionOrder(200)]
    public sealed class StartScreenV2 : MonoBehaviour
    {
        public CanvasGroup View;
        public Button GuestButton;
        public Button LoginButton;
        public Button RegisterButton;
        public Button OptionsButton;
        public AuthUIController AuthUI;
        public SettingsV2Integration Settings;
        public Canvas SettingsCanvas;
        private bool entered;

        private void Start()
        {
            GuestButton.onClick.AddListener(Guest);
            LoginButton.onClick.AddListener(Login);
            RegisterButton.onClick.AddListener(Register);
            OptionsButton.onClick.AddListener(Options);
            AuthUI.OnPlayPressed += Enter;
            AuthUI.OnLoginSuccess += Enter;
            AuthUI.OnRegistrationSuccess += Enter;
            AuthUI.OnClosed += AuthClosed;
            if (AppFlowManager.ConsumeReturnToHome()) CompleteEntrance();
            else { View.alpha = 1; View.blocksRaycasts = true; View.interactable = true; }
        }
        private void Guest()
        {
            AuthBootstrapper.Instance?.PlayFabAuth?.ForceGuestIdentity();
            Enter();
        }
        private void Login() { AuthUI.ShowAuthUI(); AuthUI.ShowLoginPanel(); }
        private void Register() { AuthUI.ShowAuthUI(); AuthUI.ShowRegisterPanel(); }
        private void Options() { Settings.Open(); SettingsCanvas.overrideSorting = true; SettingsCanvas.sortingOrder = 1500; }
        private void AuthClosed() { if (!entered) View.alpha = 1; }
        private void Enter()
        {
            if (AppLoadingView.Instance != null) AppLoadingView.Instance.EnterHome(CompleteEntrance);
            else CompleteEntrance();
        }
        private void CompleteEntrance()
        {
            entered = true; AuthUI.HideAuthUI(); SettingsCanvas.sortingOrder = 20;
            View.alpha = 0; View.blocksRaycasts = false; View.interactable = false;
        }
        private void OnDestroy()
        {
            if (AuthUI == null) return;
            AuthUI.OnPlayPressed -= Enter; AuthUI.OnLoginSuccess -= Enter;
            AuthUI.OnRegistrationSuccess -= Enter; AuthUI.OnClosed -= AuthClosed;
        }
    }
}
