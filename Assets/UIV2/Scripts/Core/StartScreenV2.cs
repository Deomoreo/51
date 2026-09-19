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
        [Tooltip("Pulsante Novita' in alto e la pagina che apre (Tools/UIV2/Build News Screen).")]
        public Button NewsButton;
        public NewsScreenV2 News;
        private bool entered;

        /// <summary>Vero dopo che si e' entrati in Home: la schermata iniziale non tornera' piu'.</summary>
        public bool HasEntered => entered;

        private void Start()
        {
            GuestButton.onClick.AddListener(Guest);
            LoginButton.onClick.AddListener(Login);
            RegisterButton.onClick.AddListener(Register);
            OptionsButton.onClick.AddListener(Options);
            if (NewsButton != null && News != null) NewsButton.onClick.AddListener(News.Open);
            AuthUI.OnPlayPressed += Enter;
            AuthUI.OnLoginSuccess += Enter;
            AuthUI.OnRegistrationSuccess += Enter;
            AuthUI.OnClosed += AuthClosed;
            if (AppFlowManager.ConsumeReturnToHome()) CompleteEntrance();
            else { View.alpha = 1; View.blocksRaycasts = true; View.interactable = true; }
        }
        private void Guest() => PlayAsGuest();

        /// <summary>Identita' ospite temporanea e ingresso in Home. Anche dal pulsante "Accedi come ospite" della schermata Accesso (mockup 23).</summary>
        public void PlayAsGuest()
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
