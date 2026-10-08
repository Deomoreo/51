using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace Project51.Auth
{
    /// <summary>
    /// Gestisce la connessione a Photon PUN con Custom Authentication via PlayFab.
    /// 
    /// IMPORTANTE:
    /// - NON impostare AuthValues.Token direttamente, causa fallimento auth!
    /// - Usare AddAuthParameter per username e token
    /// - Il token va ottenuto da PlayFab tramite GetPhotonAuthenticationToken
    /// 
    /// CONFIGURAZIONE PHOTON DASHBOARD:
    /// 1. Vai su https://dashboard.photonengine.com
    /// 2. Seleziona la tua app PUN
    /// 3. Manage > Authentication
    /// 4. Aggiungi Custom Authentication:
    ///    - Authentication URL: https://{PLAYFAB_TITLE_ID}.playfabapi.com/photon/authenticate
    ///    - NON spuntare "Allow anonymous clients" (opzionale ma consigliato)
    /// </summary>
    public class PhotonAuthConnector : MonoBehaviourPunCallbacks
    {
        public static PhotonAuthConnector Instance { get; private set; }
        
        // Stato
        public bool IsConnecting { get; private set; }
        
        // Eventi
        public event Action OnConnectedToPhotonEvent;
        public event Action<string> OnConnectionFailed;
        
        // Configurazione
        [Header("Settings")]
        [Tooltip("Timeout in secondi per la connessione")]
        [SerializeField] private float connectionTimeout = 30f;
        
        private bool _isAuthConfigured;
        private float _connectionStartTime;

        // Stanze separate per protocollo: ConnectUsingSettings usa AppSettings.AppVersion (l'asset ha un valore vecchio), che Photon
        // manda all'autenticazione. Messa prima della prima scena, quindi prima di ogni connessione (qui, MatchmakingManager) e
        // valida anche per Reconnect / rientro, che riusano la stessa.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void MatchOnlySameAppVersion()
        {
            var settings = PhotonNetwork.PhotonServerSettings;
            if (settings != null) settings.AppSettings.AppVersion = AppVersion;
        }

        /// <summary>
        /// Protocollo multiplayer (#154): due build si incontrano (ricerca partita, stanze per codice, presenza in Photon Chat) solo se e'
        /// uguale. Si alza SOLO quando due build non possono piu' giocare insieme (regole, messaggi di rete, dati del CloudScript che la
        /// partita richiede); non segue bundleVersion ne' i numeri di build, cosi' le build compatibili restano insieme.
        /// Storia: TestFlight build 2 = "1.0.0" (Application.version, prima di questa costante); 3 = Build 3 (51 esatti -> 0, input e
        /// rientro B3/B7/B8, biglietti di partita #141).
        /// </summary>
        public const int ProtocolVersion = 3;

        /// <summary>Versione per Photon (PUN aggiunge "_" + la sua versione) e Photon Chat.</summary>
        public static string AppVersion => "p" + ProtocolVersion;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // Creato come figlio di AuthBootstrapper (gia' persistente): DDOL vale solo sulla radice.
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }
        
        private void Update()
        {
            // Check timeout
            if (IsConnecting && Time.time - _connectionStartTime > connectionTimeout)
            {
                IsConnecting = false;
                Debug.LogError("[PhotonAuth] Connection timeout");
                OnConnectionFailed?.Invoke("Connection timeout");
                PhotonNetwork.Disconnect();
            }
        }
        
        /// <summary>
        /// Configura l'autenticazione custom per Photon.
        /// Chiamare prima di ConnectToPhoton().
        /// </summary>
        /// <param name="playFabId">PlayFab ID dell'utente.</param>
        /// <param name="photonToken">Token ottenuto da PlayFab.GetPhotonAuthenticationToken.</param>
        public void ConfigureCustomAuthentication(string playFabId, string photonToken)
        {
            if (string.IsNullOrEmpty(playFabId) || string.IsNullOrEmpty(photonToken))
            {
                Debug.LogError("[PhotonAuth] Cannot configure auth: playFabId or token is null/empty");
                return;
            }
            
            // IMPORTANTE: Creare AuthenticationValues con CustomAuthenticationType.Custom
            var authValues = new AuthenticationValues
            {
                AuthType = CustomAuthenticationType.Custom
                // NON impostare Token qui! Usa AddAuthParameter
            };
            
            // Aggiungi i parametri richiesti dal Photon-PlayFab integration
            authValues.AddAuthParameter("username", playFabId);
            authValues.AddAuthParameter("token", photonToken);
            
            PhotonNetwork.AuthValues = authValues;
            _isAuthConfigured = true;
            
            Debug.Log($"[PhotonAuth] Custom authentication configured for user: {playFabId.Substring(0, 8)}...");
        }
        
        /// <summary>
        /// Connette a Photon Cloud usando le impostazioni di PhotonServerSettings.
        /// Richiede che ConfigureCustomAuthentication sia stato chiamato prima.
        /// </summary>
        /// <param name="nickname">Nickname del giocatore (opzionale).</param>
        public void ConnectToPhoton(string nickname = null)
        {
            if (PhotonNetwork.IsConnected)
            {
                Debug.Log("[PhotonAuth] Already connected to Photon");
                OnConnectedToPhotonEvent?.Invoke();
                return;
            }
            
            if (IsConnecting)
            {
                Debug.LogWarning("[PhotonAuth] Connection already in progress");
                return;
            }
            
            if (!_isAuthConfigured)
            {
                Debug.LogError("[PhotonAuth] Custom authentication not configured! Call ConfigureCustomAuthentication first.");
                OnConnectionFailed?.Invoke("Authentication not configured");
                return;
            }
            
            // Imposta nickname
            if (!string.IsNullOrEmpty(nickname))
            {
                PhotonNetwork.NickName = nickname;
            }
            
            // Configura settings ottimali per mobile
            ConfigurePhotonSettings();
            
            IsConnecting = true;
            _connectionStartTime = Time.time;
            
            Debug.Log($"[PhotonAuth] Connecting to Photon as '{PhotonNetwork.NickName}'...");
            
            bool result = PhotonNetwork.ConnectUsingSettings();
            
            if (!result)
            {
                IsConnecting = false;
                Debug.LogError("[PhotonAuth] ConnectUsingSettings returned false");
                OnConnectionFailed?.Invoke("Failed to start connection");
            }
        }
        
        /// <summary>
        /// Disconnette da Photon.
        /// </summary>
        public void Disconnect()
        {
            if (PhotonNetwork.IsConnected || IsConnecting)
            {
                PhotonNetwork.Disconnect();
            }
            IsConnecting = false;
            _isAuthConfigured = false;
        }
        
        private void ConfigurePhotonSettings()
        {
            // Ottimizzazioni per gioco mobile turn-based
            PhotonNetwork.SendRate = 20;
            PhotonNetwork.SerializationRate = 10;
            
            // Mantiene connessione in background (importante per mobile)
            PhotonNetwork.KeepAliveInBackground = 60f;
            
            // Auto sync scene per transizioni di scena
        PhotonNetwork.AutomaticallySyncScene = true;
        }
        
        #region PUN Callbacks
        
        public override void OnConnectedToMaster()
        {
            IsConnecting = false;
            Debug.Log("[PhotonAuth] Connected to Photon Master Server");
            OnConnectedToPhotonEvent?.Invoke();
        }
        
        public override void OnDisconnected(DisconnectCause cause)
        {
            bool wasConnecting = IsConnecting;
            IsConnecting = false;
            _isAuthConfigured = false;
            
            Debug.Log($"[PhotonAuth] Disconnected from Photon. Cause: {cause}");
            
            // Gestisci cause specifiche
            string errorMessage = cause switch
            {
                DisconnectCause.CustomAuthenticationFailed => "Authentication failed. Invalid PlayFab token.",
                DisconnectCause.InvalidAuthentication => "Invalid authentication credentials.",
                DisconnectCause.AuthenticationTicketExpired => "Authentication ticket expired. Please re-login.",
                DisconnectCause.MaxCcuReached => "Server full. Please try again later.",
                DisconnectCause.ServerTimeout => "Connection timeout.",
                DisconnectCause.ClientTimeout => "Connection timeout.",
                _ => $"Disconnected: {cause}"
            };
            
            // Fallimento: errori di autenticazione, o la rete che cade mentre ci si sta ancora collegando (senza questo chi aspetta
            // l'esito restava fermo fino al limite di 30 s). Il proprio timeout ha gia' avvisato e chiude con DisconnectByClientLogic.
            bool authCause = cause == DisconnectCause.CustomAuthenticationFailed ||
                cause == DisconnectCause.InvalidAuthentication ||
                cause == DisconnectCause.AuthenticationTicketExpired;
            if ((wasConnecting || authCause) &&
                cause != DisconnectCause.DisconnectByClientLogic && cause != DisconnectCause.ApplicationQuit)
            {
                OnConnectionFailed?.Invoke(errorMessage);
            }
        }
        
        public override void OnCustomAuthenticationFailed(string debugMessage)
        {
            IsConnecting = false;
            _isAuthConfigured = false;
            
            Debug.LogError($"[PhotonAuth] Custom authentication failed: {debugMessage}");
            OnConnectionFailed?.Invoke($"Authentication failed: {debugMessage}");
        }
        
        public override void OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
            Debug.Log("[PhotonAuth] Custom authentication response received");
            
            // Log data for debugging (in sviluppo)
            if (Debug.isDebugBuild && data != null)
            {
                foreach (var kvp in data)
                {
                    Debug.Log($"  {kvp.Key}: {kvp.Value}");
                }
            }
        }
        
        #endregion
    }
}
