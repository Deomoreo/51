using System;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

namespace Project51.Auth
{
    /// <summary>
    /// Controller UI per Login/Registrazione con email e password (pannelli UI51 in Canvas_Login).
    ///
    /// FLUSSO:
    /// - All'avvio e' nascosto; chi lo apre (StartScreenV2, Home, Impostazioni) chiama ShowAuthUI e poi
    ///   ShowLoginPanel o ShowRegisterPanel. ShowAuthUI da solo apre AccountPanel solo con un login vero.
    /// - RegisterPanel: campi username/email/password + bottone Registra
    /// - LoginPanel: campi email/password + bottone Login
    /// </summary>
    public class AuthUIController : MonoBehaviour
    {
        #region Serialized Fields - Panels
        
        [Header("Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject accountPanel;
        [SerializeField] private CanvasGroup mainCanvasGroup;

        [Header("Account Panel")]
        [Tooltip("Button to close the account panel.")]
        [SerializeField] private Button accountCloseButton;

        [Header("Canvas Sorting")]
        [Tooltip("If true, this canvas will be forced to a high sortingOrder while the auth UI is visible.")]
        [SerializeField] private bool forceHighSortingOrder = true;
        [SerializeField] private int sortingOrderWhileVisible = 2000;

        #endregion
        
        #region Serialized Fields - Register Panel
        
        [Header("Register Panel")]
        [SerializeField] private TMP_InputField registerUsernameInput;
        [SerializeField] private TMP_InputField registerEmailInput;
        [SerializeField] private TMP_InputField registerPasswordInput;
        [SerializeField] private Button registerButton;
        [SerializeField] private TextMeshProUGUI registerStatusText;
        
        #endregion
        
        #region Serialized Fields - Login Panel
        
        [Header("Login Panel")]
        [SerializeField] private TMP_InputField loginEmailInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private TextMeshProUGUI loginStatusText;

        #endregion
        
        #region Events
        
        /// <summary>Vecchio ingresso da ospite: qui nessuno lo alza piu', resta finche' TapToEnterUI/StartScreenV2 vi si iscrivono (Fase 10 B7H).</summary>
#pragma warning disable 0067
        public event Action OnPlayPressed;
#pragma warning restore 0067

        /// <summary>Invocato quando l'utente chiude l'UI auth senza entrare nel gioco.</summary>
        public event Action OnClosed;
        
        /// <summary>Invocato quando la registrazione ha successo.</summary>
        public event Action OnRegistrationSuccess;
        
        /// <summary>Invocato quando il login ha successo.</summary>
        public event Action OnLoginSuccess;

        #endregion
        
        #region Private Fields
        
        private bool _isProcessing;
        private Canvas _thisCanvas;
        private int _previousSortingOrder;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            EnsureThisCanvasBlocksInput();

            _thisCanvas = GetComponent<Canvas>();
            if (_thisCanvas != null)
            {
                _previousSortingOrder = _thisCanvas.sortingOrder;
            }

            // Setup button listeners - Register Panel
            if (registerButton != null)
                registerButton.onClick.AddListener(OnRegisterClicked);
            
            // Setup button listeners - Login Panel
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginClicked);
            
            // Hide loading
            SetLoading(false);

            if (accountCloseButton != null)
            {
                accountCloseButton.onClick.RemoveAllListeners();
                accountCloseButton.onClick.AddListener(OnAccountCloseClicked);
            }
        }

        private void Start()
        {
            // Auth UI starts hidden: StartScreenV2, the Home and Settings open it with ShowAuthUI().
            HideAuthUI();
        }
        
        #endregion
        
        #region Panel Navigation
        
        public void ShowLoginPanel()
        {
            HideAllPanels();
            if (loginPanel != null)
            {
                loginPanel.SetActive(true);
                ClearInputFields(loginEmailInput, loginPasswordInput);
            }
            ClearStatusTexts();
        }
        
        public void ShowRegisterPanel()
        {
            HideAllPanels();
            if (registerPanel != null)
            {
                registerPanel.SetActive(true);
                ClearInputFields(registerUsernameInput, registerEmailInput, registerPasswordInput);
            }
            ClearStatusTexts();
        }
        
        public void HideAllPanels()
        {
            if (loginPanel != null) loginPanel.SetActive(false);
            if (registerPanel != null) registerPanel.SetActive(false);
            if (accountPanel != null) accountPanel.SetActive(false);
        }
        
        /// <summary>
        /// Nasconde completamente l'UI di autenticazione.
        /// </summary>
        public void HideAuthUI()
        {
            HideAllPanels();

            RestoreCanvasSorting();
            
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 0;
                mainCanvasGroup.interactable = false;
                mainCanvasGroup.blocksRaycasts = false;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Mostra l'UI di autenticazione.
        /// </summary>
        public void ShowAuthUI()
        {
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 1;
                mainCanvasGroup.interactable = true;
                mainCanvasGroup.blocksRaycasts = true;
            }
            else
            {
                gameObject.SetActive(true);
            }

            ForceCanvasSorting();

            // AccountPanel only for REAL logins (email/register). Guests get no panel here:
            // callers pick Login or Register right after.
            var bs = AuthBootstrapper.Instance;
            bool hasRealLogin = bs != null && bs.PlayFabAuth != null && bs.PlayFabAuth.HasRealLogin;

            if (hasRealLogin)
                ShowAccountPanel();
            else
            {
                HideAllPanels();
                ClearStatusTexts();
            }
        }

        /// <summary>
        /// Shows the account panel (real logins only).
        /// </summary>
        public void ShowAccountPanel()
        {
            HideAllPanels();

            if (accountPanel != null)
                accountPanel.SetActive(true);
            else
                Debug.LogWarning("[AuthUIController] accountPanel not assigned.");

            ClearStatusTexts();
        }

        #endregion
        
        #region Button Handlers
        
        /// <summary>
        /// Close button on Account panel: hide auth UI.
        /// </summary>
        private void OnAccountCloseClicked()
        {
            HideAuthUI();
            OnClosed?.Invoke();
        }
        
        private void OnRegisterClicked()
        {
            if (_isProcessing) return;
            
            string username = registerUsernameInput != null ? (registerUsernameInput.text ?? string.Empty).Trim() : string.Empty;
            string email = registerEmailInput != null ? (registerEmailInput.text ?? string.Empty).Trim() : string.Empty;
            string password = registerPasswordInput != null ? (registerPasswordInput.text ?? string.Empty) : string.Empty;
            
            SetLoading(true, "Registrazione in corso...");
            _isProcessing = true;

            var request = new AddUsernamePasswordRequest
            {
                Username = username,
                Email = email,
                Password = password
            };

            PlayFabClientAPI.AddUsernamePassword(request,
                result =>
                {
                    // Da qui e' un login vero: senza questo Profilo e Impostazioni restano in veste ospite.
                    Project51.Auth.AuthBootstrapper.Instance?.PlayFabAuth?.MarkRegistered(email);
                    
                    // Aggiorna display name tramite il servizio centrale, così la UI (Banner) riceve l'evento.
                    var bs = Project51.Auth.AuthBootstrapper.Instance;
                    if (bs != null && !string.IsNullOrWhiteSpace(username))
                    {
                        bs.PlayFabAuth.UpdateDisplayName(username);
                    }

                    _isProcessing = false;
                    SetLoading(false);
                    SetStatusText(registerStatusText, "Registrazione completata!", false);
                    Debug.Log("[AuthUIController] Registration successful!");

                    OnRegistrationSuccess?.Invoke();
                    HideAuthUI();
                },
                error =>
                {
                    _isProcessing = false;
                    SetLoading(false);
                    SetStatusText(registerStatusText, PlayFabAuthService.GetUserFriendlyError(error), true);
                }
            );
        }
        
        private void OnLoginClicked()
        {
            if (_isProcessing) return;
            // Accesso da ospite dell'avvio ancora in viaggio: la sua risposta arriverebbe dopo e sostituirebbe questo account.
            var boot = Project51.Auth.AuthBootstrapper.Instance;
            if (boot != null && (boot.CurrentState == AuthState.Initializing || boot.CurrentState == AuthState.LoggingInPlayFab))
            {
                SetStatusText(loginStatusText, "Connessione al server in corso: riprova tra un attimo.", true);
                return;
            }
            
            string email = loginEmailInput != null ? (loginEmailInput.text ?? string.Empty).Trim() : string.Empty;
            string password = loginPasswordInput != null ? (loginPasswordInput.text ?? string.Empty) : string.Empty;
            
            SetLoading(true, "Login in corso...");
            _isProcessing = true;

            var bs = Project51.Auth.AuthBootstrapper.Instance;
            if (bs == null)
            {
                _isProcessing = false;
                SetLoading(false);
                SetStatusText(loginStatusText, "AuthBootstrapper non trovato", true);
                return;
            }

            bs.PlayFabAuth.LoginWithEmail(
                email,
                password,
                onSuccess: _ =>
                {
                    bs.RebindPhoton();

                    _isProcessing = false;
                    SetLoading(false);
                    SetStatusText(loginStatusText, "Login effettuato!", false);

                    OnLoginSuccess?.Invoke();
                    HideAuthUI();
                },
                onError: errorMsg =>
                {
                    _isProcessing = false;
                    SetLoading(false);
                    SetStatusText(loginStatusText, errorMsg, true);
                }
            );
        }
        
        #endregion
        
        #region Private Methods
        
        private void SetLoading(bool show, string message = null)
        {
            Project51.Core.AppLoading.SetAuthenticationBusy(show, message);

            // Disabilita interazione durante il caricamento
            if (registerButton != null) registerButton.interactable = !show;
            if (loginButton != null) loginButton.interactable = !show;
        }
        
        private void SetStatusText(TextMeshProUGUI statusText, string message, bool isError)
        {
            if (statusText == null) return;
            
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.green;
            statusText.gameObject.SetActive(true);
        }
        
        private void ClearStatusTexts()
        {
            if (registerStatusText != null)
            {
                registerStatusText.text = "";
                registerStatusText.gameObject.SetActive(false);
            }
            
            if (loginStatusText != null)
            {
                loginStatusText.text = "";
                loginStatusText.gameObject.SetActive(false);
            }
        }
        
        private void ClearInputFields(params TMP_InputField[] fields)
        {
            foreach (var field in fields)
            {
                if (field != null)
                {
                    field.text = "";
                }
            }
        }
        
        #endregion

        private void EnsureThisCanvasBlocksInput()
        {
            if (mainCanvasGroup == null)
            {
                mainCanvasGroup = GetComponent<CanvasGroup>();
                if (mainCanvasGroup == null)
                {
                    mainCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            // Quando visibile, deve bloccare input sotto
            mainCanvasGroup.blocksRaycasts = true;
            mainCanvasGroup.interactable = true;
        }

        private void ForceCanvasSorting()
        {
            if (!forceHighSortingOrder) return;

            if (_thisCanvas == null)
                _thisCanvas = GetComponent<Canvas>();
            if (_thisCanvas == null) return;

            _previousSortingOrder = _thisCanvas.sortingOrder;
            _thisCanvas.overrideSorting = true;
            _thisCanvas.sortingOrder = sortingOrderWhileVisible;
        }

        private void RestoreCanvasSorting()
        {
            if (!forceHighSortingOrder) return;
            if (_thisCanvas == null) return;

            _thisCanvas.sortingOrder = _previousSortingOrder;
        }
    }
}
