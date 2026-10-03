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
    /// 2. Il bootstrap parte automaticamente in Start()
    /// 3. Ascolta OnAuthReady per sapere quando procedere
    /// </summary>
    public class AuthBootstrapper : MonoBehaviour
    {
        public static AuthBootstrapper Instance { get; private set; }
        
        #region Serialized Fields
        
        [Header("Retry Settings")]
        [Tooltip("Numero massimo di tentativi per ogni step")]
        [SerializeField] private int maxRetries = 3;
        
        [Tooltip("Delay base per backoff esponenziale (secondi)")]
        [SerializeField] private float baseRetryDelay = 1f;
        
        [Tooltip("Delay massimo tra retry (secondi)")]
        [SerializeField] private float maxRetryDelay = 30f;
        
        #endregion
        
        #region Public Properties
        
        public AuthState CurrentState { get; private set; } = AuthState.None;
        public bool IsReady => CurrentState == AuthState.Ready;
        public bool HasError => CurrentState == AuthState.Error;
        public string LastError { get; private set; }
        
        // Servizi
        public PlayFabAuthService PlayFabAuth { get; private set; }
        public ProfileService Profile { get; private set; }
        
        #endregion
        
        #region Events
        
        public event Action OnAuthReady;
        
        #endregion
        
        #region Private Fields
        
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

            // Il proprio aspetto come proprieta' Photon, per gli altri al tavolo.
            Profile.OnProfileLoaded += PublishLook;
            Profile.OnProfileUpdated += PublishLook;               // editor profilo e livello dopo la partita
            PlayFabAuth.OnDisplayNameChanged += _ => PublishLook(); // "gioca come ospite" e logout alzano solo questo

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
        

        /// <summary>Aspetto vero del profilo solo con login vero e profilo caricato: stesso criterio al tavolo e in rete.</summary>
        public bool HasRealProfile => Profile != null && Profile.IsLoaded && PlayFabAuth != null && PlayFabAuth.HasRealLogin;

        /// <summary>
        /// Cornice, banner e livello come proprieta' del LocalPlayer Photon (id grezzi: li valida chi li legge,
        /// ProfileCosmetics.ReadLook). Fuori stanza restano in cache e partono col prossimo ingresso; in stanza arrivano
        /// subito. Ospite o profilo non caricato: chiavi tolte (null), perche' il LocalPlayer sopravvive a stanze e logout
        /// e porterebbe l'aspetto dell'account precedente. Rilanciato a ogni ingresso da MatchmakingManager.OnJoinedRoom.
        /// </summary>
        public void PublishLook()
        {
            var local = PhotonNetwork.LocalPlayer;
            if (local == null) return;
            // Entrando o uscendo dalla stanza Photon rifiuta l'invio con un errore in console: ci pensa il prossimo OnJoinedRoom.
            if (PhotonNetwork.CurrentRoom != null && !PhotonNetwork.InRoom) return;
            local.SetCustomProperties(HasRealProfile
                ? LookProps(Profile.FrameId, Profile.BannerId, Profile.XP, Profile.TotalGames, Profile.Wins, Profile.TotalScope, PlayFabAuth.PlayFabId)
                : LookProps(null, null, 0, 0, 0, 0, null, PlayFabAuth != null && PlayFabAuth.IsLoggedIn ? PlayFabAuth.PlayFabId : null));
        }

        /// <summary>
        /// Proprieta' Photon dell'aspetto e del profilo rapido. frame null = ospite o profilo non caricato: tutte le chiavi a null
        /// (Photon le toglie). Le legge ProfileCosmetics.ReadLook / ReadStats.
        /// </summary>
        public static ExitGames.Client.Photon.Hashtable LookProps(string frame, string banner, int xp, int games, int wins, int scope, string playFabId,
            string guestId = null)
        {
            bool real = frame != null;
            return new ExitGames.Client.Photon.Hashtable
            {
                { ProfileService.LookFrameKey, real ? frame : null },
                { ProfileService.LookBannerKey, real ? banner : null },
                { ProfileService.LookLevelKey, real ? (object)Project51.Core.PlayerXp.LevelOf(xp) : null },
                { ProfileService.LookGamesKey, real ? (object)games : null },
                { ProfileService.LookWinsKey, real ? (object)wins : null },
                { ProfileService.LookScopeKey, real ? (object)scope : null },
                { ProfileService.LookIdKey, real && !string.IsNullOrEmpty(playFabId) ? playFabId : null },
                // Ospite: solo l'ID della sessione, per poterlo segnalare (scelta dell'utente 01/10: anche gli ospiti si sanzionano).
                { ProfileService.LookGuestIdKey, !real && !string.IsNullOrEmpty(guestId) ? guestId : null },
            };
        }

        /// <summary>
        /// "Accedi" cambia l'account della sessione PlayFab: Photon va autenticato di nuovo con quello, altrimenti i webhook
        /// (controllo della sospensione, roster della partita) lavorano sull'account con cui si era avviata l'app.
        /// </summary>
        public void RebindPhoton()
        {
            if (FriendsChat.Instance != null) Destroy(FriendsChat.Instance.gameObject);
            RewardsService.Reset();
            ModerationService.Reset();
            if (PlayFabAuth == null || _photonConnector == null) return;
            // Subito fuori da Photon e senza le credenziali vecchie, compreso il biglietto che PhotonNetwork.Reconnect riusa (senza
            // l'indirizzo del master non riparte): finche' il token nuovo non arriva si resta scollegati, mai di nuovo con l'account di prima.
            PhotonNetwork.AuthValues = null;
            PhotonNetwork.NetworkingClient.MasterServerAddress = null;
            _photonConnector.Disconnect();
            if (_rebindCoroutine != null) StopCoroutine(_rebindCoroutine);
            _rebindCoroutine = StartCoroutine(ReconnectPhoton(int.MaxValue));
        }

        /// <summary>
        /// Un tentativo di Photon con l'account di adesso e un token nuovo da PlayFab (riconnessione della Home con il biglietto scaduto o
        /// di un altro account, partita cercata senza credenziali). Se ce n'e' gia' uno in corso (anche quello di RebindPhoton) salta la sua
        /// attesa tra un tentativo e l'altro (RIPROVA, tentativi della Home).
        /// </summary>
        public void ReconnectPhotonNow()
        {
            if (_rebindCoroutine != null) { _retryNow = true; return; }
            if (PlayFabAuth != null && _photonConnector != null)
                _rebindCoroutine = StartCoroutine(ReconnectPhoton(1));
        }

        private Coroutine _rebindCoroutine;
        private bool _retryNow;

        private bool PhotonOnThisAccount =>
            PhotonNetwork.IsConnectedAndReady && PlayFabAuth != null && PhotonNetwork.LocalPlayer.UserId == PlayFabAuth.PlayFabId;

        private static bool PhotonIdle =>
            PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Disconnected
            || PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.PeerCreated;

        private IEnumerator ReconnectPhoton(int attempts)
        {
            // Rete o PlayFab giu': si riprova sempre piu' piano finche' l'account resta dentro, mai con le credenziali vecchie. Finito
            // solo con Photon collegato proprio a questo account.
            for (int attempt = 0; attempt < attempts && PlayFabAuth.IsLoggedIn; attempt++)
            {
                if (attempt > 0)
                {
                    float wake = Time.unscaledTime + Mathf.Min(30f, 2f * attempt);
                    while (!_retryNow && Time.unscaledTime < wake) yield return null;
                }
                _retryNow = false;
                if (PhotonOnThisAccount) break;
                bool done = false, ok = false;
                PlayFabAuth.GetPhotonAuthenticationToken(GetPhotonAppId(), _ => ok = done = true,
                    error => { done = true; Debug.LogWarning("[AuthBootstrapper] Photon token after login failed: " + error); });
                while (!done) yield return null;
                if (!ok) continue;
                // Una connessione partita nel frattempo (Gioca online durante l'attesa) ha gia' le credenziali di questo account (RebindPhoton
                // ha tolto quelle vecchie): se ne aspetta l'esito invece di tagliarla.
                float until = Time.unscaledTime + 15f;
                while (!PhotonIdle && !PhotonNetwork.IsConnectedAndReady && Time.unscaledTime < until) yield return null;
                if (PhotonOnThisAccount) break;
                if (!PhotonIdle) _photonConnector.Disconnect(); // collegato con un altro account, o fermo a meta': prima fuori
                until = Time.unscaledTime + 5f;
                while (!PhotonIdle && Time.unscaledTime < until) yield return null;
                _photonConnector.ConfigureCustomAuthentication(PlayFabAuth.PlayFabId, PlayFabAuth.PhotonCustomAuthToken);
                _photonConnector.ConnectToPhoton(PlayFabAuth.GetBestDisplayName());
                // Esito: collegato, o di nuovo fuori (connessione fallita, o il limite di 30 s del connettore).
                while (PlayFabAuth.IsLoggedIn && !PhotonNetwork.IsConnectedAndReady && !PhotonIdle) yield return null;
                if (PhotonOnThisAccount) break;
            }
            _rebindCoroutine = null;
        }

        public void LogoutAndRestart(bool clearRealAccountFlag = false)
        {
            // Amici (Photon Chat): via la connessione col vecchio account; Ensure() ne crea una nuova al prossimo uso.
            if (FriendsChat.Instance != null) Destroy(FriendsChat.Instance.gameObject);
            RewardsService.Reset();
            ModerationService.Reset();
            if (_rebindCoroutine != null) { StopCoroutine(_rebindCoroutine); _rebindCoroutine = null; }
            PhotonNetwork.NetworkingClient.MasterServerAddress = null; // il biglietto dell'account vecchio non deve riportarlo dentro

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
                    PlayFabAuth.ClearRegisteredFlag();
                }
            }

            StartAuthentication();
        }
        
        #endregion
        
        #region Authentication Flow
        
        private IEnumerator AuthenticationFlow()
        {
            Debug.Log("[AuthBootstrapper] Starting authentication flow...");
            
            SetState(AuthState.Initializing);
            
            // Step 1: Login PlayFab (Guest)
            SetState(AuthState.LoggingInPlayFab);
            
            bool playFabLoginDone = false;
            bool playFabLoginSuccess = false;
            string playFabError = null;
            
            // Gia' dentro (riprova dopo un errore, RIPROVA del caricamento): si tiene l'account. Un accesso da ospite qui
            // sostituirebbe quello fatto con "Accedi". Dopo il logout la sessione non c'e' piu' e si entra come ospite.
            if (PlayFabAuth.IsLoggedIn)
            {
                playFabLoginSuccess = true;
                playFabLoginDone = true;
            }
            else
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
            
            Debug.Log("[AuthBootstrapper] PlayFab login successful");
            
            // Step 2: Get Photon Token
            SetState(AuthState.GettingPhotonToken);
            
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
                yield return HandleError("Failed to get Photon token", photonTokenError);
                yield break;
            }
            
            Debug.Log("[AuthBootstrapper] Photon token received");
            
            // Step 3: Connect to Photon
            SetState(AuthState.ConnectingPhoton);
            
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
            Profile.LoadProfile(PublishLook); // anche a caricamento fallito: toglie l'aspetto
            
            // READY!
            SetState(AuthState.Ready);
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
                
                yield return new WaitForSeconds(delay);
                
                // Riavvia il flusso
                _authCoroutine = StartCoroutine(AuthenticationFlow());
            }
            else
            {
                // Troppi tentativi, mostra errore finale
                SetState(AuthState.Error);
            }
        }
        
        private void SetState(AuthState newState)
        {
            if (CurrentState == newState) return;
            
            CurrentState = newState;
            Debug.Log($"[AuthBootstrapper] State changed to: {newState}");
        }
        
        private string GetPhotonAppId()
        {
            // Da PhotonServerSettings
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
