using System;
using System.Collections;
using UnityEngine;
using Photon.Pun;

namespace Project51.Auth
{
    /// <summary>
    /// Bootstrapper principale per l'autenticazione.
    /// Singleton (DontDestroyOnLoad) che gestisce la macchina a stati di auth.
    /// 
    /// Flusso: Init ? PlayFabLoginGuest ? GetPhotonToken ? PhotonConnect ? Ready
    /// 
    /// CONFIGURAZIONE RICHIESTA:
    /// 
    /// 1. PLAYFAB:
    ///    - Imposta TitleId in PlayFabSharedSettings (Assets/PlayFabSDK/Shared/Public/Resources)
    ///    - O imposta via PlayFabSettings.TitleId nel codice
    /// 
    /// 2. PHOTON:
    ///    - Configura PhotonServerSettings (Assets/Photon/PhotonUnityNetworking/Resources)
    ///    - L'AppId si legge automaticamente da PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime
    /// 
    /// 3. PHOTON DASHBOARD (Authentication):
    ///    - URL: https://{PLAYFAB_TITLE_ID}.playfabapi.com/photon/authenticate
    ///    - Disabilita "Allow anonymous clients" (opzionale ma consigliato per sicurezza)
    /// 
    /// USO:
    /// 1. Aggiungi questo componente a un GameObject nella prima scena
    /// 2. Configura authUI se vuoi callback UI
    /// 3. Il bootstrap parte automaticamente in Start()
    /// 4. Ascolta OnAuthReady o OnAuthStateChanged per sapere quando procedere
    /// </summary>
    public class AuthBootstrapper : MonoBehaviour
    {
        public static AuthBootstrapper Instance { get; private set; }
        
        #region Serialized Fields
        
        [Header("Configuration")]
        [Tooltip("Photon App ID (PUN). Se vuoto, usa PhotonServerSettings.")]
        [SerializeField] private string photonAppIdOverride;
        
        [Header("Retry Settings")]
        [Tooltip("Numero massimo di tentativi per ogni step")]
        [SerializeField] private int maxRetries = 3;
        
        [Tooltip("Delay base per backoff esponenziale (secondi)")]
        [SerializeField] private float baseRetryDelay = 1f;
        
        [Tooltip("Delay massimo tra retry (secondi)")]
        [SerializeField] private float maxRetryDelay = 30f;
        
        [Header("UI Callback (Optional)")]
        [SerializeField] private MonoBehaviour authUIComponent;
        
        #endregion
        
        #region Public Properties
        
        public AuthState CurrentState { get; private set; } = AuthState.None;
        public bool IsReady => CurrentState == AuthState.Ready;
        public bool HasError => CurrentState == AuthState.Error;
        public string LastError { get; private set; }
        
        // Servizi
        public PlayFabAuthService PlayFabAuth { get; private set; }
        public ProfileService Profile { get; private set; }

        /// <summary>
        /// True se l'utente ha già fatto almeno un login su questo device (anche guest).
        /// Se true, mostra TapToEnter; se false, mostra auth UI per la prima scelta.
        /// </summary>
        public bool ShouldShowTapToEnter => PlayFabAuth != null && PlayFabAuth.HasEverLoggedIn;
        
        #endregion
        
        #region Events
        
        public event Action OnAuthReady;
        public event Action<AuthState> OnAuthStateChanged;
        public event Action<string> OnAuthError;
        
        #endregion
        
        #region Private Fields
        
        private IAuthUI _authUI;
        private PhotonAuthConnector _photonConnector;
        private int _currentRetryCount;
        private Coroutine _authCoroutine;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AuthBootstrapper] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Inizializza servizi
            PlayFabAuth = new PlayFabAuthService();
            Profile = new ProfileService();
            
            // Setup UI callback
            if (authUIComponent != null && authUIComponent is IAuthUI ui)
            {
                _authUI = ui;
            }
            
            // Trova o crea PhotonAuthConnector
            _photonConnector = FindObjectOfType<PhotonAuthConnector>();
            if (_photonConnector == null)
            {
                var go = new GameObject("PhotonAuthConnector");
                go.transform.SetParent(transform);
                _photonConnector = go.AddComponent<PhotonAuthConnector>();
            }
        }
        
        private void Start()
        {
            // If the user is not in a real-login state, treat guest as ephemeral: generate a new guest next launch.
            if (PlayFabAuth != null && !PlayFabAuth.HasRealLogin)
            {
                PlayFabAuth.ResetGuestDeviceId();
            }

            // Avvia il processo di autenticazione automaticamente
            StartAuthentication();
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Avvia o riavvia il processo di autenticazione.
        /// </summary>
        public void StartAuthentication()
        {
            if (_authCoroutine != null)
            {
                StopCoroutine(_authCoroutine);
            }
            
            _currentRetryCount = 0;
            _authCoroutine = StartCoroutine(AuthenticationFlow());
        }
        
        /// <summary>
        /// Riprova l'autenticazione dopo un errore.
        /// </summary>
        public void RetryAuthentication()
        {
            if (CurrentState == AuthState.Error)
            {
                StartAuthentication();
            }
        }

        public void LogoutAndRestart(bool clearRealAccountFlag = false)
        {
            try
            {
                if (PhotonNetwork.IsConnected)
                    PhotonNetwork.Disconnect();
            }
            catch { }

            // Clear cached/nicked values so a subsequent guest login does not reuse the previous account name.
            try
            {
                PhotonNetwork.NickName = string.Empty;
            }
            catch { }

            if (PlayFabAuth != null)
            {
                PlayFabAuth.Logout();
                if (clearRealAccountFlag)
                {
                    PlayFabAuth.ClearRealLoginFlag();
                    PlayFabAuth.ClearHasLoggedIn();
                    PlayFabAuth.ClearRegisteredFlag();
                }
            }

            StartAuthentication();
        }
        
        /// <summary>
        /// Registra un'interfaccia UI per i callback.
        /// </summary>
        public void RegisterAuthUI(IAuthUI ui)
        {
            _authUI = ui;
            
            // Notifica stato corrente
            _authUI?.OnAuthStateChanged(CurrentState);
            
            if (CurrentState == AuthState.Ready)
            {
                _authUI?.OnAuthReady();
                _authUI?.SetGuestBadge(!PlayFabAuth.IsAccountLinked);
                _authUI?.SetPlayerName(PlayFabAuth.GetBestDisplayName());
            }

            // Keep UI updated if the display name changes later (e.g. after registration/link).
            PlayFabAuth.OnDisplayNameChanged -= HandleDisplayNameChanged;
            PlayFabAuth.OnDisplayNameChanged += HandleDisplayNameChanged;
        }

        private void HandleDisplayNameChanged(string newName)
        {
            _authUI?.SetPlayerName(string.IsNullOrWhiteSpace(newName) ? PlayFabAuth.GetBestDisplayName() : newName);
        }
        
        /// <summary>
        /// Protegge l'account collegandolo al provider di piattaforma (Google/Apple).
        /// </summary>
        public void ProtectAccount(Action onSuccess = null, Action<string> onError = null)
        {
            var nativePlatformAuth = NativePlatformAuth.Instance;
            
            if (nativePlatformAuth == null)
            {
                // Crea il componente se non esiste
                var go = new GameObject("NativePlatformAuth");
                go.transform.SetParent(transform);
                nativePlatformAuth = go.AddComponent<NativePlatformAuth>();
            }
            
            _authUI?.ShowLoading(true, "Connecting to account...");
            
            nativePlatformAuth.RequestPlatformAuth(
                (token, authType) =>
                {
                    Debug.Log($"[AuthBootstrapper] Platform auth successful, type: {authType}");
                    
                    _authUI?.ShowLoading(true, "Linking account...");
                    
                    if (authType == PlatformAuthType.Google)
                    {
                        PlayFabAuth.LinkGoogleAccount(token, false,
                            () => OnAccountLinked(onSuccess),
                            error => OnAccountLinkFailed(error, onError)
                        );
                    }
                    else if (authType == PlatformAuthType.Apple)
                    {
                        PlayFabAuth.LinkAppleAccount(token, false,
                            () => OnAccountLinked(onSuccess),
                            error => OnAccountLinkFailed(error, onError)
                        );
                    }
                },
                error =>
                {
                    _authUI?.ShowLoading(false);
                    _authUI?.ShowError(error, true);
                    onError?.Invoke(error);
                }
            );
        }
        
        #endregion
        
        #region Authentication Flow
        
        private IEnumerator AuthenticationFlow()
        {
            Debug.Log("[AuthBootstrapper] Starting authentication flow...");
            
            SetState(AuthState.Initializing);
            _authUI?.ShowLoading(true, "Initializing...");
            
            // Step 1: Login PlayFab (Guest)
            SetState(AuthState.LoggingInPlayFab);
            _authUI?.ShowLoading(true, "Connecting to server...");
            
            bool playFabLoginDone = false;
            bool playFabLoginSuccess = false;
            string playFabError = null;
            
            PlayFabAuth.LoginAsGuest(
                playFabId =>
                {
                    playFabLoginSuccess = true;
                    playFabLoginDone = true;
                },
                error =>
                {
                    playFabError = error;
                    playFabLoginDone = true;
                }
            );
            
            // Attendi completamento
            while (!playFabLoginDone)
            {
                yield return null;
            }
            
            if (!playFabLoginSuccess)
            {
                yield return HandleError("PlayFab login failed", playFabError);
                yield break;
            }
            
            Debug.Log($"[AuthBootstrapper] PlayFab login successful: {PlayFabAuth.PlayFabId}");
            
            // Step 2: Get Photon Token
            SetState(AuthState.GettingPhotonToken);
            _authUI?.ShowLoading(true, "Authenticating...");
            
            string photonAppId = GetPhotonAppId();
            
            if (string.IsNullOrEmpty(photonAppId))
            {
                yield return HandleError("Configuration error", "Photon App ID not configured");
                yield break;
            }
            
            bool photonTokenDone = false;
            bool photonTokenSuccess = false;
            string photonTokenError = null;
            
            PlayFabAuth.GetPhotonAuthenticationToken(photonAppId,
                token =>
                {
                    photonTokenSuccess = true;
                    photonTokenDone = true;
                },
                error =>
                {
                    photonTokenError = error;
                    photonTokenDone = true;
                }
            );
            
            while (!photonTokenDone)
            {
                yield return null;
            }
            
            if (!photonTokenSuccess)
            {
                // In prototipo possiamo continuare anche senza Photon configurato.
                // Caso tipico: PlayFab add-on Photon non configurato => PhotonApplicationNotFound.
                if (!string.IsNullOrEmpty(photonTokenError) && photonTokenError.IndexOf("PhotonApplicationNotFound", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Debug.LogWarning("[AuthBootstrapper] Photon not configured on PlayFab (PhotonApplicationNotFound). Continuing without Photon for prototype.");

                    _authUI?.ShowLoading(false);
                    _authUI?.ShowError(
                        "Photon non è configurato su PlayFab (PhotonApplicationNotFound).\n" +
                        "Per il prototipo continuiamo senza multiplayer.\n\n" +
                        "Quando vorrai abilitarlo:\n" +
                        "1) PlayFab Game Manager > Add-ons > Photon > configura l'AppId\n" +
                        "2) Photon Dashboard > Authentication > imposta l'URL PlayFab /photon/authenticate",
                        canRetry: false);

                    // Segna Ready per permettere testing UI/guest/progressi anche senza networking.
                    SetState(AuthState.Ready);
                    OnAuthReady?.Invoke();
                    yield break;
                }

                yield return HandleError("Failed to get Photon token", photonTokenError);
                yield break;
            }
            
            Debug.Log("[AuthBootstrapper] Photon token received");
            
            // Step 3: Connect to Photon
            SetState(AuthState.ConnectingPhoton);
            _authUI?.ShowLoading(true, "Connecting to game server...");
            
            // Configura autenticazione custom
            _photonConnector.ConfigureCustomAuthentication(
                PlayFabAuth.PlayFabId,
                PlayFabAuth.PhotonCustomAuthToken
            );
            
            // Imposta nickname (sempre derivato dallo stato corrente, non da valori stale)
            string nickname = PlayFabAuth.GetBestDisplayName();
            
            bool photonConnectDone = false;
            bool photonConnectSuccess = false;
            string photonConnectError = null;
            
            Action onConnected = () =>
            {
                photonConnectSuccess = true;
                photonConnectDone = true;
            };
            
            Action<string> onConnectFailed = error =>
            {
                photonConnectError = error;
                photonConnectDone = true;
            };
            
            _photonConnector.OnConnectedToPhotonEvent += onConnected;
            _photonConnector.OnConnectionFailed += onConnectFailed;
            
            _photonConnector.ConnectToPhoton(nickname);
            
            // Timeout per la connessione
            float timeout = 30f;
            float elapsed = 0f;
            
            while (!photonConnectDone && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            _photonConnector.OnConnectedToPhotonEvent -= onConnected;
            _photonConnector.OnConnectionFailed -= onConnectFailed;
            
            if (!photonConnectSuccess)
            {
                string error = photonConnectError ?? "Connection timeout";
                yield return HandleError("Failed to connect to Photon", error);
                yield break;
            }
            
            Debug.Log("[AuthBootstrapper] Photon connected successfully");
            
            // Step 4: Load profile (opzionale, non blocca)
            Profile.LoadProfile();
            
            // Step 5: Check account link status
            PlayFabAuth.CheckAccountLinkStatus(isLinked =>
            {
                _authUI?.SetGuestBadge(!isLinked);
            });
            
            // READY!
            SetState(AuthState.Ready);
            _authUI?.ShowLoading(false);
            _authUI?.SetPlayerName(nickname);
            _authUI?.OnAuthReady();
            OnAuthReady?.Invoke();
            
            Debug.Log("[AuthBootstrapper] Authentication complete! Ready to play.");
        }
        
        private IEnumerator HandleError(string title, string details)
        {
            LastError = $"{title}: {details}";
            Debug.LogError($"[AuthBootstrapper] {LastError}");
            
            _currentRetryCount++;
            
            if (_currentRetryCount <= maxRetries)
            {
                // Retry con backoff esponenziale
                float delay = Mathf.Min(baseRetryDelay * Mathf.Pow(2, _currentRetryCount - 1), maxRetryDelay);
                
                Debug.Log($"[AuthBootstrapper] Retrying in {delay:F1}s (attempt {_currentRetryCount}/{maxRetries})");
                _authUI?.ShowLoading(true, $"Retrying in {delay:F0}s...");
                
                yield return new WaitForSeconds(delay);
                
                // Riavvia il flusso
                _authCoroutine = StartCoroutine(AuthenticationFlow());
            }
            else
            {
                // Troppi tentativi, mostra errore finale
                SetState(AuthState.Error);
                _authUI?.ShowLoading(false);
                _authUI?.ShowError(LastError, true);
                OnAuthError?.Invoke(LastError);
            }
        }
        
        private void OnAccountLinked(Action onSuccess)
        {
            _authUI?.ShowLoading(false);
            _authUI?.SetGuestBadge(false);
            
            Debug.Log("[AuthBootstrapper] Account protected successfully!");
            onSuccess?.Invoke();
        }
        
        private void OnAccountLinkFailed(string error, Action<string> onError)
        {
            _authUI?.ShowLoading(false);
            _authUI?.ShowError(error, true);
            onError?.Invoke(error);
        }
        
        private void SetState(AuthState newState)
        {
            if (CurrentState == newState) return;
            
            CurrentState = newState;
            Debug.Log($"[AuthBootstrapper] State changed to: {newState}");
            
            _authUI?.OnAuthStateChanged(newState);
            OnAuthStateChanged?.Invoke(newState);
        }
        
        private string GetPhotonAppId()
        {
            // Prima controlla override
            if (!string.IsNullOrEmpty(photonAppIdOverride))
            {
                return photonAppIdOverride;
            }
            
            // Altrimenti leggi da PhotonServerSettings
            try
            {
                return PhotonNetwork.PhotonServerSettings?.AppSettings?.AppIdRealtime;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthBootstrapper] Failed to get Photon App ID: {e.Message}");
                return null;
            }
        }
        
        #endregion
    }
}
