using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Project51.Auth
{
    /// <summary>
    /// Controller UI minimale per la schermata di login/gate.
    /// Mostra stato autenticazione, errori e pulsante "Proteggi Account".
    /// 
    /// Implementa IAuthUI per ricevere callback dal AuthBootstrapper.
    /// 
    /// SETUP:
    /// 1. Crea un Canvas con questo componente
    /// 2. Collega i riferimenti UI (loading panel, error panel, ecc.)
    /// 3. Questo viene automaticamente nascosto quando l'auth è completata
    /// </summary>
    public class LoginGateUI : MonoBehaviour, IAuthUI
    {
        #region Serialized Fields
        
        [Header("Main Panels")]
        [SerializeField] private CanvasGroup mainCanvasGroup;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject errorPanel;
        [SerializeField] private GameObject readyPanel;
        
        [Header("Loading UI")]
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private SpriteRenderer loadingSpinner;
        
        [Header("Error UI")]
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private Button retryButton;
        
        [Header("Ready/Profile UI")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private GameObject guestBadge;
        [SerializeField] private Button protectAccountButton;
        [SerializeField] private TextMeshProUGUI protectAccountButtonText;
        [SerializeField] private Button continueButton;
        
        [Header("Status Indicator")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private SpriteRenderer statusIcon;
        [SerializeField] private Color statusConnecting = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color statusReady = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color statusError = new Color(0.8f, 0.2f, 0.2f);
        
        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.3f;
        
        #endregion
        
        #region Private Fields
        
        private AuthBootstrapper _authBootstrapper;
        private bool _isProtecting;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Setup button listeners
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }
            
            if (protectAccountButton != null)
            {
                protectAccountButton.onClick.AddListener(OnProtectAccountClicked);
            }
            
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
            
            // Nascondi pannelli inizialmente
            ShowPanel(loadingPanel);
            HidePanel(errorPanel);
            HidePanel(readyPanel);
        }
        
        private void Start()
        {
            // Trova e registra con AuthBootstrapper
            _authBootstrapper = AuthBootstrapper.Instance;
            
            if (_authBootstrapper != null)
            {
                _authBootstrapper.RegisterAuthUI(this);
            }
            else
            {
                Debug.LogError("[LoginGateUI] AuthBootstrapper not found! Make sure it exists in the scene.");
            }
            
            // Spinner rotation
            if (loadingSpinner != null)
            {
                loadingSpinner.transform.DORotate(new Vector3(0, 0, -360), 1f, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Restart)
                    .SetEase(Ease.Linear);
            }
        }
        
        private void OnDestroy()
        {
            if (loadingSpinner != null)
            {
                loadingSpinner.transform.DOKill();
            }
        }
        
        #endregion
        
        #region IAuthUI Implementation
        
        public void ShowLoading(bool show, string message = null)
        {
            if (show)
            {
                ShowPanel(loadingPanel);
                HidePanel(errorPanel);
                HidePanel(readyPanel);
                
                if (loadingText != null && !string.IsNullOrEmpty(message))
                {
                    loadingText.text = message;
                }
            }
            else
            {
                HidePanel(loadingPanel);
            }
            
            UpdateStatus(message ?? "Loading...", statusConnecting);
        }
        
        public void ShowError(string errorMessage, bool canRetry = true)
        {
            HidePanel(loadingPanel);
            ShowPanel(errorPanel);
            HidePanel(readyPanel);
            
            if (errorText != null)
            {
                errorText.text = errorMessage;
            }
            
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(canRetry);
            }
            
            UpdateStatus("Error", statusError);
        }
        
        public void SetGuestBadge(bool isGuest)
        {
            if (guestBadge != null)
            {
                guestBadge.SetActive(isGuest);
            }
            
            if (protectAccountButton != null)
            {
                protectAccountButton.gameObject.SetActive(isGuest);
            }
            
            // Aggiorna testo bottone in base alla piattaforma
            if (protectAccountButtonText != null)
            {
#if UNITY_ANDROID
                protectAccountButtonText.text = "Link Google Account";
#elif UNITY_IOS
                protectAccountButtonText.text = "Sign in with Apple";
#else
                protectAccountButtonText.text = "Protect Account";
#endif
            }
        }
        
        public void SetPlayerName(string playerName)
        {
            if (playerNameText != null)
            {
                playerNameText.text = playerName ?? "Player";
            }
        }
        
        public void OnAuthReady()
        {
            HidePanel(loadingPanel);
            HidePanel(errorPanel);
            ShowPanel(readyPanel);
            
            UpdateStatus("Ready", statusReady);
            
            // Auto-hide dopo un breve delay se configurato
            // Oppure l'utente clicca "Continue"

            if (_authBootstrapper != null)
            {
                SetGuestBadge(!_authBootstrapper.PlayFabAuth.IsAccountLinked);
                SetPlayerName(_authBootstrapper.PlayFabAuth.GetBestDisplayName());
            }
        }
        
        public void OnAuthStateChanged(AuthState newState)
        {
            // Log per debug
            Debug.Log($"[LoginGateUI] Auth state: {newState}");
            
            // Aggiorna status text
            string statusMessage = newState switch
            {
                AuthState.Initializing => "Initializing...",
                AuthState.LoggingInPlayFab => "Connecting to server...",
                AuthState.GettingPhotonToken => "Authenticating...",
                AuthState.ConnectingPhoton => "Connecting to game...",
                AuthState.Ready => "Ready!",
                AuthState.Error => "Error",
                _ => "..."
            };
            
            Color statusColor = newState switch
            {
                AuthState.Ready => statusReady,
                AuthState.Error => statusError,
                _ => statusConnecting
            };
            
            UpdateStatus(statusMessage, statusColor);
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnRetryClicked()
        {
            if (_authBootstrapper != null)
            {
                _authBootstrapper.RetryAuthentication();
            }
        }
        
        private void OnProtectAccountClicked()
        {
            if (_isProtecting || _authBootstrapper == null) return;
            
            _isProtecting = true;
            
            _authBootstrapper.ProtectAccount(
                onSuccess: () =>
                {
                    _isProtecting = false;
                    SetGuestBadge(false);
                    
                    // Mostra feedback positivo
                    if (statusText != null)
                    {
                        statusText.text = "Account protected!";
                    }
                },
                onError: error =>
                {
                    _isProtecting = false;
                    ShowError(error, false);
                    
                    // Torna a ready dopo un po'
                    DOVirtual.DelayedCall(3f, () =>
                    {
                        if (_authBootstrapper != null && _authBootstrapper.IsReady)
                        {
                            OnAuthReady();
                        }
                    });
                }
        );
        }
        
        private void OnContinueClicked()
        {
            // Nascondi questo UI e prosegui al gioco
            if (mainCanvasGroup != null)
            {
                DOTween.To(() => mainCanvasGroup.alpha, x => mainCanvasGroup.alpha = x, 0f, fadeDuration)
                    .OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                    });
            }
            else
            {
                gameObject.SetActive(false);
            }
            
            // Qui puoi invocare un evento o caricare la prossima scena
            // Es: SceneManager.LoadScene("MainMenu");
        }
        
        #endregion
        
        #region Private Methods
        
        
        private void ShowPanel(GameObject panel)
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }
        }
        
        private void HidePanel(GameObject panel)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
        
        private void UpdateStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
            
            if (statusIcon != null)
            {
                statusIcon.color = color;
            }
        }
        
        #endregion
    }
}
