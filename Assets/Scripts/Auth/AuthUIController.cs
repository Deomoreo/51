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
        [SerializeField] private CanvasGroup mainCanvasGroup;

        [Header("Visual Gate (optional)")]
        [Tooltip("Optional full-screen overlay (Image) to visually hide other canvases behind the auth UI.")]
        [SerializeField] private CanvasGroup dimmerCanvasGroup;
        [SerializeField] private float dimmerAlpha = 0.9f;

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
        
        #endregion
        
        #region Events
        
        /// <summary>Invocato quando l'utente sceglie di giocare (registrato o guest).</summary>
        public event Action OnPlayPressed;
        
        /// <summary>Invocato quando la registrazione ha successo.</summary>
        public event Action OnRegistrationSuccess;
        
        /// <summary>Invocato quando il login ha successo.</summary>
        public event Action OnLoginSuccess;
        
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
                playAsGuestButton.onClick.AddListener(OnPlayAsGuestClicked);
            
            if (showRegisterButton != null)
                showRegisterButton.onClick.AddListener(ShowRegisterPanel);
            
            if (showLoginButton != null)
                showLoginButton.onClick.AddListener(ShowLoginPanel);
            
            // Setup button listeners - Register Panel
            if (registerButton != null)
                registerButton.onClick.AddListener(OnRegisterClicked);
            
            if (registerBackButton != null)
                registerBackButton.onClick.AddListener(ShowGuestPanel);
            
            // Setup button listeners - Login Panel
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginClicked);
            
            if (loginBackButton != null)
                loginBackButton.onClick.AddListener(ShowGuestPanel);
            
            // Hide loading
            SetLoading(false);
        }
        
        private void Start()
        {
            // Integrato: se `AuthBootstrapper` è presente, aspetta che la state machine sia Ready
            // prima di mostrare UI e permettere register/login.
            var bootstrapper = AuthBootstrapper.Instance;
            if (bootstrapper == null)
            {
                Debug.LogWarning("[AuthUIController] AuthBootstrapper non trovato. UI in modalità standalone.");
                GateGameUI(true);
                OnAuthReady();
                return;
            }

            if (bootstrapper.IsReady)
            {
                GateGameUI(true);
                OnAuthReady();
                return;
            }

            // Mostra overlay mentre aspettiamo (se configurato)
            SetLoading(true, "Connessione in corso...");
            GateGameUI(true);
            bootstrapper.OnAuthReady += HandleBootstrapperReady;
        }

        private void OnEnable()
        {
            // Se la UI viene riattivata (o la scena ricarica), assicurati che questo Canvas stia sopra.
            ForceCanvasSorting();

            // Il dimmer deve stare dietro ai pannelli della auth UI, ma comunque sopra agli altri canvas.
            if (dimmerCanvasGroup != null)
            {
                dimmerCanvasGroup.transform.SetAsFirstSibling();
            }
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
            OnAuthReady();
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
        }
        
        /// <summary>
        /// Nasconde completamente l'UI di autenticazione.
        /// </summary>
        public void HideAuthUI()
        {
            HideAllPanels();

            GateGameUI(false);

            SetDimmerVisible(false);
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
            SetDimmerVisible(true);
            ForceCanvasSorting();
            ShowGuestPanel();
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnPlayAsGuestClicked()
        {
            Debug.Log("[AuthUIController] Play as guest clicked");

            // Nota: anche se l'utente entra come guest, l'EXP verrà gestita da PlayerProgressLocal
            // e messa in pending finché non si registra.
            HideAuthUI();
            OnPlayPressed?.Invoke();
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

                    OnRegistrationSuccess?.Invoke();
                    HideAuthUI();
                    OnPlayPressed?.Invoke();
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

                    _isProcessing = false;
                    SetLoading(false);
                    SetStatusText(loginStatusText, "Login effettuato!", false);

                    OnLoginSuccess?.Invoke();
                    HideAuthUI();
                    OnPlayPressed?.Invoke();
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
        
        private void OnAuthReady()
        {
            Debug.Log("[AuthUIController] Auth ready, checking registration status...");

            // Gate UI: NON far partire il gioco automaticamente.
            // Anche se l'utente risulta già registrato, mostriamo comunque la schermata e lasciamo scegliere.
            ShowGuestPanel();
        }
        
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

        private void SetDimmerVisible(bool visible)
        {
            if (dimmerCanvasGroup == null) return;

            if (visible)
            {
                if (!dimmerCanvasGroup.gameObject.activeSelf)
                    dimmerCanvasGroup.gameObject.SetActive(true);

                dimmerCanvasGroup.alpha = dimmerAlpha;
                dimmerCanvasGroup.blocksRaycasts = true;
                dimmerCanvasGroup.interactable = true;
            }
            else
            {
                dimmerCanvasGroup.alpha = 0f;
                dimmerCanvasGroup.blocksRaycasts = false;
                dimmerCanvasGroup.interactable = false;

                if (dimmerCanvasGroup.gameObject.activeSelf)
                    dimmerCanvasGroup.gameObject.SetActive(false);
            }
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
