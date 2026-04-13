using System;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

namespace Project51.Auth
{
    /// <summary>
    /// Controller UI per Login/Registrazione con email e password.
    /// Gestisce pannelli di Login, Registrazione e stato Guest.
    /// 
    /// SETUP:
    /// 1. Crea un Canvas con questo componente
    /// 2. Crea 3 pannelli: GuestPanel, LoginPanel, RegisterPanel
    /// 3. Collega i riferimenti UI (InputField, Button, Text)
    /// 4. AuthBootstrapper deve esistere nella scena
    /// 
    /// FLUSSO:
    /// - All'avvio: mostra GuestPanel se non registrato, altrimenti nasconde tutto
    /// - GuestPanel: "Gioca" (prosegue come guest), "Registrati", "Ho già un account"
    /// - RegisterPanel: campi username/email/password + bottone Registra
    /// - LoginPanel: campi email/password + bottone Login
    /// </summary>
    public class AuthUIController : MonoBehaviour
    {
        #region Serialized Fields - Panels
        
        [Header("Panels")]
        [SerializeField] private GameObject guestPanel;
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject accountPanel;
        [SerializeField] private CanvasGroup mainCanvasGroup;

        [Header("Account Panel")]
        [Tooltip("Text that shows the current player name / account info.")]
        [SerializeField] private TextMeshProUGUI accountPlayerNameText;
        [Tooltip("Text that shows the PlayFab ID or account type (guest/registered).")]
        [SerializeField] private TextMeshProUGUI accountStatusText;
        [Tooltip("Button to close the account panel and go back to TapToEnter.")]
        [SerializeField] private Button accountCloseButton;

        [Header("Canvas Sorting")]
        [Tooltip("If true, this canvas will be forced to a high sortingOrder while the auth UI is visible.")]
        [SerializeField] private bool forceHighSortingOrder = true;
        [SerializeField] private int sortingOrderWhileVisible = 2000;

        [Header("Gate (block other UI until user chooses)")]
        [Tooltip("Se abilitato, disabilita l'interazione sugli altri Canvas mentre questa UI è visibile.")]
        [SerializeField] private bool gateOtherCanvases = true;

        [Tooltip("Canvas da disabilitare durante il gate. Se vuoto, verranno presi tutti i Canvas attivi (tranne questo).")]
        [SerializeField] private Canvas[] canvasesToGate;
        
        #endregion
        
        #region Serialized Fields - Guest Panel
        
        [Header("Guest Panel")]
        [SerializeField] private Button playAsGuestButton;
        [SerializeField] private Button showRegisterButton;
        [SerializeField] private Button showLoginButton;
        [SerializeField] private Button guestBackButton;
        [SerializeField] private TextMeshProUGUI guestInfoText;
        
        #endregion
        
        #region Serialized Fields - Register Panel
        
        [Header("Register Panel")]
        [SerializeField] private TMP_InputField registerUsernameInput;
        [SerializeField] private TMP_InputField registerEmailInput;
        [SerializeField] private TMP_InputField registerPasswordInput;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button registerBackButton;
        [SerializeField] private TextMeshProUGUI registerStatusText;
        
        #endregion
        
        #region Serialized Fields - Login Panel
        
        [Header("Login Panel")]
        [SerializeField] private TMP_InputField loginEmailInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button loginBackButton;
        [SerializeField] private TextMeshProUGUI loginStatusText;
        
        #endregion
        
        #region Serialized Fields - Loading
        
        [Header("Loading")]
        [SerializeField] private GameObject loadingOverlay;
        [SerializeField] private TextMeshProUGUI loadingText;

        [Header("Account")]
        [Tooltip("Optional: button that logs out of the current account and restarts auth.")]
        [SerializeField] private Button logoutButton;
        
        #endregion
        
        #region Events
        
        /// <summary>Invocato quando l'utente sceglie di giocare (registrato o guest).</summary>
        public event Action OnPlayPressed;

        /// <summary>Invocato quando l'utente chiude l'UI auth senza entrare nel gioco.</summary>
        public event Action OnClosed;
        
        /// <summary>Invocato quando la registrazione ha successo.</summary>
        public event Action OnRegistrationSuccess;
        
        /// <summary>Invocato quando il login ha successo.</summary>
        public event Action OnLoginSuccess;

        public bool IsClosingToTapToEnter { get; private set; }
        
        #endregion
        
        #region Private Fields
        
        private bool _isProcessing;
        private CanvasGroup[] _gatedCanvasGroups;
        private Canvas _thisCanvas;
        private int _previousSortingOrder;

        // Stato locale (coerente con `PlayFabAuthService`)
        private const string KEY_IS_REGISTERED = "Project51_IsRegistered";
        
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

            // Setup button listeners - Guest Panel
            if (playAsGuestButton != null)
            {
                playAsGuestButton.onClick.RemoveAllListeners();
                playAsGuestButton.onClick.AddListener(OnPlayAsGuestClicked);
            }
            
            if (showRegisterButton != null)
            {
                showRegisterButton.onClick.RemoveAllListeners();
                showRegisterButton.onClick.AddListener(ShowRegisterPanel);
            }
            
            if (showLoginButton != null)
            {
                showLoginButton.onClick.RemoveAllListeners();
                showLoginButton.onClick.AddListener(ShowLoginPanel);
            }

            if (guestBackButton != null)
            {
                guestBackButton.onClick.RemoveAllListeners();
                guestBackButton.onClick.AddListener(OnAccountCloseClicked);
            }
            
            // Setup button listeners - Register Panel
            if (registerButton != null)
                registerButton.onClick.AddListener(OnRegisterClicked);
            
            if (registerBackButton != null)
            {
                registerBackButton.onClick.RemoveAllListeners();
                registerBackButton.onClick.AddListener(ShowGuestPanel);
            }
            
            // Setup button listeners - Login Panel
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginClicked);
            
            if (loginBackButton != null)
            {
                loginBackButton.onClick.RemoveAllListeners();
                loginBackButton.onClick.AddListener(ShowGuestPanel);
            }
            
            // Hide loading
            SetLoading(false);

            if (logoutButton != null)
                logoutButton.onClick.AddListener(Logout);

            if (accountCloseButton != null)
            {
                accountCloseButton.onClick.RemoveAllListeners();
                accountCloseButton.onClick.AddListener(OnAccountCloseClicked);
            }
        }

        public void Logout()
        {
            var bs = Project51.Auth.AuthBootstrapper.Instance;
            if (bs != null)
                bs.LogoutAndRestart(clearRealAccountFlag: true);

            // After logout, stay in auth UI and show guest/login/register panel.
            ShowGuestPanel();
            Debug.Log("[AuthUIController] Logged out. Showing login/register/guest panel.");
        }
        
        private void Start()
        {
            // Auth UI starts hidden. TapToEnterUI is the entry point and will call
            // ShowAuthUI() when the user needs to login/register.
            HideAuthUI();
        }

        private void OnDestroy()
        {
            var bootstrapper = AuthBootstrapper.Instance;
            if (bootstrapper != null)
            {
                bootstrapper.OnAuthReady -= HandleBootstrapperReady;
            }
        }

        private void HandleBootstrapperReady()
        {
            var bootstrapper = AuthBootstrapper.Instance;
            if (bootstrapper != null)
            {
                bootstrapper.OnAuthReady -= HandleBootstrapperReady;
            }

            SetLoading(false);
        }
        
        #endregion
        
        #region Panel Navigation
        
        public void ShowGuestPanel()
        {
            HideAllPanels();
            if (guestPanel != null)
            {
                guestPanel.SetActive(true);
                UpdateGuestPanelInfo();
            }
            ClearStatusTexts();
        }
        
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
            if (guestPanel != null) guestPanel.SetActive(false);
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

            GateGameUI(false);

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

            GateGameUI(true);
            ForceCanvasSorting();

            // Decide which panel to show based on login state.
            // Only show AccountPanel for REAL logins (email/register), not guest.
            var bs = AuthBootstrapper.Instance;
            bool hasRealLogin = bs != null && bs.PlayFabAuth != null && bs.PlayFabAuth.HasRealLogin;

            if (hasRealLogin)
                ShowAccountPanel();
            else
                ShowGuestPanel();
        }

        /// <summary>
        /// Shows the account info panel (player name, account type, logout).
        /// </summary>
        public void ShowAccountPanel()
        {
            HideAllPanels();

            if (accountPanel != null)
            {
                accountPanel.SetActive(true);
                UpdateAccountPanelInfo();
            }
            else
            {
                // Fallback: if no account panel is configured, show guest panel.
                Debug.LogWarning("[AuthUIController] accountPanel not assigned. Falling back to GuestPanel.");
                ShowGuestPanel();
            }

            ClearStatusTexts();
        }

        private void UpdateAccountPanelInfo()
        {
            var bs = AuthBootstrapper.Instance;
            if (bs == null || bs.PlayFabAuth == null) return;

            string displayName = bs.PlayFabAuth.GetBestDisplayName();
            bool isRegistered = bs.PlayFabAuth.IsRegistered;

            if (accountPlayerNameText != null)
                accountPlayerNameText.text = displayName;

            if (accountStatusText != null)
            {
                if (isRegistered)
                    accountStatusText.text = "Account registrato";
                else
                    accountStatusText.text = "Guest";
            }
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnPlayAsGuestClicked()
        {
            Debug.Log("[AuthUIController] Play as guest clicked");

            var bs = Project51.Auth.AuthBootstrapper.Instance;
            if (bs != null && bs.PlayFabAuth != null)
            {
                // Force guest identity: clear registered name, use Guest_xxxx.
                bs.PlayFabAuth.ForceGuestIdentity();

                // Do NOT call MarkHasLoggedIn: guest sessions are temporary.
                // Next app launch will show auth UI again.

                // Update Photon nickname to match the new guest name.
                string guestName = bs.PlayFabAuth.GetBestDisplayName();
                try { Photon.Pun.PhotonNetwork.NickName = guestName; } catch { }
            }

            HideAuthUI();
            OnPlayPressed?.Invoke();
        }

        /// <summary>
        /// Close button on Account panel: hide auth UI and go back to TapToEnter.
        /// </summary>
        private void OnAccountCloseClicked()
        {
            IsClosingToTapToEnter = true;
            HideAuthUI();

            OnClosed?.Invoke();

            // Reset next frame to avoid any same-frame callbacks entering the game.
            StartCoroutine(ResetClosingFlagNextFrame());
        }

        private System.Collections.IEnumerator ResetClosingFlagNextFrame()
        {
            yield return null;
            IsClosingToTapToEnter = false;
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
                    MarkRegisteredLocal(true);
                    
                    // Aggiorna display name tramite il servizio centrale, così la UI (Banner/LoginGate) riceve l'evento.
                    var bs = Project51.Auth.AuthBootstrapper.Instance;
                    if (bs != null && !string.IsNullOrWhiteSpace(username))
                    {
                        bs.PlayFabAuth.UpdateDisplayName(username);
                    }

                    _isProcessing = false;
                    SetLoading(false);
                    SetStatusText(registerStatusText, "Registrazione completata!", false);
                    Debug.Log("[AuthUIController] Registration successful!");
                    
                    // Se esiste `PlayerProgressLocal`, riscatta pendingExp
                    PlayerProgressLocal.Instance?.ClaimPendingExp();

                    var bsReg = Project51.Auth.AuthBootstrapper.Instance;
                    bsReg?.PlayFabAuth?.MarkHasLoggedIn();

                    OnRegistrationSuccess?.Invoke();
                    HideAuthUI();
                },
                error =>
                {
                    _isProcessing = false;
                    SetLoading(false);
                    SetStatusText(registerStatusText, GetUserFriendlyError(error), true);
                }
            );
        }
        
        private void OnLoginClicked()
        {
            if (_isProcessing) return;
            
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
                    MarkRegisteredLocal(true);
                    PlayerProgressLocal.Instance?.ClaimPendingExp();

                    bs.PlayFabAuth?.MarkHasLoggedIn();

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
        
        private void UpdateGuestPanelInfo()
        {
            if (guestInfoText == null) return;
            
            bool isRegistered = IsRegisteredLocal();
            bool hasPending = PlayerProgressLocal.Instance?.HasPendingExp ?? false;
            int pendingExp = PlayerProgressLocal.Instance?.PendingExp ?? 0;
            
            if (isRegistered)
            {
                guestInfoText.text = "Bentornato! Puoi accedere o giocare come guest.";
            }
            else if (hasPending)
            {
                guestInfoText.text = $"Puoi giocare come guest, ma l'EXP andrà in attesa.\nHai {pendingExp} EXP in attesa: registrati per riscattarli.";
            }
            else
            {
                guestInfoText.text = "Puoi entrare come guest oppure registrarti per salvare i progressi.";
            }
        }
        
        private void SetLoading(bool show, string message = null)
        {
            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(show);
            }
            
            if (loadingText != null && !string.IsNullOrEmpty(message))
            {
                loadingText.text = message;
            }
            
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
        
        #region Public Utility Methods
        
        /// <summary>
        /// Mostra un messaggio di errore all'utente.
        /// </summary>
        public void ShowError(string message)
        {
            // Mostra nel pannello attivo
            if (registerPanel != null && registerPanel.activeSelf)
            {
                SetStatusText(registerStatusText, message, true);
            }
            else if (loginPanel != null && loginPanel.activeSelf)
            {
                SetStatusText(loginStatusText, message, true);
            }
            else if (guestInfoText != null)
            {
                guestInfoText.text = message;
                guestInfoText.color = Color.red;
            }
        }
        
        /// <summary>
        /// Restituisce true se l'utente è registrato.
        /// </summary>
        public bool IsUserRegistered => IsRegisteredLocal();
        
        #endregion

        private bool IsRegisteredLocal()
        {
            return PlayerPrefs.GetInt(KEY_IS_REGISTERED, 0) == 1;
        }

        private void MarkRegisteredLocal(bool value)
        {
            PlayerPrefs.SetInt(KEY_IS_REGISTERED, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private string GetUserFriendlyError(PlayFabError error)
        {
            switch (error.Error)
            {
                case PlayFabErrorCode.InvalidEmailAddress:
                    return "Email non valida";
                case PlayFabErrorCode.InvalidEmailOrPassword:
                    return "Email o password non corretti";
                case PlayFabErrorCode.InvalidPassword:
                    return "Password non corretta";
                case PlayFabErrorCode.EmailAddressNotAvailable:
                    return "Questa email è già in uso";
                case PlayFabErrorCode.UsernameNotAvailable:
                    return "Questo username è già in uso";
                case PlayFabErrorCode.AccountNotFound:
                    return "Account non trovato";
                case PlayFabErrorCode.InvalidParams:
                    return "Dati inseriti non validi";
                default:
                    return error.ErrorMessage;
            }
        }

        private void GateGameUI(bool gate)
        {
            if (!gateOtherCanvases) return;

            if (gate)
            {
                var myCanvas = GetComponentInParent<Canvas>();
                Canvas[] targets = canvasesToGate;
                if (targets == null || targets.Length == 0)
                {
                    // In scene complesse è rischioso gate-are "tutto": può includere canvas di sistema/modali.
                    // Se non configurato esplicitamente, facciamo solo soft-gate sul parent canvas group di root.
                    Debug.LogWarning("[AuthUIController] canvasesToGate is empty. Assign Canvas_Static/Canvas_Dynamic/Canvas_Overlay explicitly for reliable gating.");
                    return;
                }

                // Usa ESATTAMENTE l'ordine configurato dall'Inspector.
                // L'ordine di attivazione può influenzare CanvasScaler/SafeArea/LayoutGroup.
                targets = FilterTargetsKeepOrder(targets, myCanvas);

                var list = new System.Collections.Generic.List<CanvasGroup>();

                foreach (var c in targets)
                {
                    if (c == null) continue;

                    var cg = c.GetComponent<CanvasGroup>();
                    if (cg == null)
                    {
                        cg = c.gameObject.AddComponent<CanvasGroup>();
                    }

                    // Disabilita input, ma non nasconde la grafica
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                    list.Add(cg);
                }

                _gatedCanvasGroups = list.ToArray();
            }
            else
            {
                if (_gatedCanvasGroups != null)
                {
                    foreach (var cg in _gatedCanvasGroups)
                    {
                        if (cg == null) continue;
                        cg.interactable = true;
                        cg.blocksRaycasts = true;
                    }
                }

                _gatedCanvasGroups = null;
            }
        }

        private Canvas[] FilterTargetsKeepOrder(Canvas[] input, Canvas myCanvas)
        {
            var seen = new System.Collections.Generic.HashSet<int>();
            var list = new System.Collections.Generic.List<Canvas>(input.Length);

            foreach (var c in input)
            {
                if (c == null) continue;
                if (myCanvas != null && c == myCanvas) continue;
                if (!seen.Add(c.GetInstanceID())) continue;
                list.Add(c);
            }

            return list.ToArray();
        }

        // Layout rebuild for gated canvases removed: toggling GameObjects/layout caused instability in complex UIs.

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
