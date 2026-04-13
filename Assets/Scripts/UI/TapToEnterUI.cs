using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project51.UI
{
    /// <summary>
    /// Prima schermata dell'app. SEMPRE visibile all'avvio.
    /// 
    /// Bottoni:
    /// - "Tap to Enter": se HasRealLogin -> Home diretta; altrimenti -> Auth UI
    /// - "Account":      se loggato con account reale -> info account/logout;
    ///                    altrimenti -> Auth UI (guest/login/register)
    /// - "Version":      mostra versione corrente
    /// 
    /// Dopo che l'utente entra (da qui o da Auth UI), questa schermata si nasconde.
    /// La Home/HUD e' gia' sotto in gerarchia.
    /// </summary>
    public sealed class TapToEnterUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button tapToEnterButton;
        [SerializeField] private Button accountButton;
        [SerializeField] private Button patchNotesButton;

        [Header("Info")]
        [SerializeField] private TextMeshProUGUI versionText;

        [Header("References")]
        [Tooltip("Drag the AuthUIController component here.")]
        [SerializeField] private Project51.Auth.AuthUIController authUI;

        [Header("Canvas")]
        [Tooltip("The Canvas of this screen. Used to toggle visibility cleanly.")]
        [SerializeField] private Canvas thisCanvas;

        private bool _authRequestedEnter;

        private void Awake()
        {
            if (tapToEnterButton != null)
                tapToEnterButton.onClick.AddListener(OnTapToEnter);

            if (accountButton != null)
                accountButton.onClick.AddListener(OnAccountPressed);

            if (versionText != null)
                versionText.text = string.Format("v{0}", Application.version);

            if (thisCanvas == null)
                thisCanvas = GetComponentInParent<Canvas>();
        }

        private void Start()
        {
            // Always show this screen on app start.
            Show();

            // Listen: when auth UI finishes (user chose guest/login/register) -> enter home.
            if (authUI != null)
            {
                authUI.OnPlayPressed += OnPlayPressed;
                authUI.OnLoginSuccess += OnLoginSuccess;
                authUI.OnRegistrationSuccess += OnRegistrationSuccess;
            }
        }

        private void OnDestroy()
        {
            if (authUI != null)
            {
                authUI.OnPlayPressed -= OnPlayPressed;
                authUI.OnLoginSuccess -= OnLoginSuccess;
                authUI.OnRegistrationSuccess -= OnRegistrationSuccess;
            }
        }

        /// <summary>Show TapToEnter screen (called on app start and after logout).</summary>
        public void Show()
        {
            if (thisCanvas != null)
                thisCanvas.enabled = true;

            gameObject.SetActive(true);
        }

        /// <summary>Hide TapToEnter screen and show the Home/HUD underneath.</summary>
        public void Hide()
        {
            Debug.Log("[TapToEnterUI] Hide() called. StackTrace:\n" + Environment.StackTrace);

            if (thisCanvas != null)
                thisCanvas.enabled = false;

            gameObject.SetActive(false);
        }

        // -- Button Handlers --------------------------------------------------

        private void OnTapToEnter()
        {
            var bs = Project51.Auth.AuthBootstrapper.Instance;
            bool hasRealLogin = bs != null && bs.PlayFabAuth != null && bs.PlayFabAuth.HasRealLogin;

            if (hasRealLogin)
            {
                // Returning registered user -> go straight to Home.
                Debug.Log("[TapToEnterUI] Returning registered user. Entering Home.");
                EnterHome();
            }
            else
            {
                // Guest or first time -> show auth UI (guest/login/register).
                Debug.Log("[TapToEnterUI] No real login. Showing Auth UI.");
                _authRequestedEnter = true;
                ShowAuth();
            }
        }

        private void OnAccountPressed()
        {
            // Always show auth UI: it will decide account panel vs guest panel.
            Debug.Log("[TapToEnterUI] Account button pressed. Showing Auth UI.");
            _authRequestedEnter = false;
            ShowAuth();
        }

        // -- Helpers ----------------------------------------------------------

        private void ShowAuth()
        {
            if (authUI != null)
                authUI.ShowAuthUI();
        }

        /// <summary>Hide TapToEnter and enter the game Home.</summary>
        private void EnterHome()
        {
            Hide();
        }

        /// <summary>Callback from AuthUIController events (guest/login/register done).</summary>
        private void OnAuthComplete()
        {

            Debug.Log("[TapToEnterUI] Auth complete. Entering Home.");
            EnterHome();
        }
        private void OnPlayPressed()
        {
            Debug.Log("[TapToEnterUI] Play pressed. Entering Home.");
            EnterHome();
        }

        private void OnLoginSuccess()
        {
            Debug.Log("[TapToEnterUI] Login success. Entering Home.");
            EnterHome();
        }

        private void OnRegistrationSuccess()
        {
            Debug.Log("[TapToEnterUI] Registration success. Entering Home.");
            EnterHome();
        }
    }
}
