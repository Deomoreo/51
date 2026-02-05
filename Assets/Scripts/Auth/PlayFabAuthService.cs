using System;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

namespace Project51.Auth
{
    /// <summary>
    /// Servizio per l'autenticazione PlayFab.
    /// Gestisce login guest, recupero token Photon e link account.
    /// 
    /// CONFIGURAZIONE RICHIESTA:
    /// 
    /// 1. PLAYFAB SETUP:
    ///    - Crea un titolo su PlayFab Game Manager (https://developer.playfab.com)
    ///    - Copia il Title ID e impostalo in PlayFabSettings (o via codice)
    ///    - In Settings > API Features: abilita "Allow client to post player statistics"
    /// 
    /// 2. PHOTON AUTHENTICATION SETUP (su Photon Dashboard):
    ///    - Vai su https://dashboard.photonengine.com
    ///    - Seleziona la tua app > Manage > Authentication
    ///    - Aggiungi Custom Authentication Provider:
    ///      - Type: Custom
    ///      - Authentication URL: https://{YOUR_PLAYFAB_TITLE_ID}.playfabapi.com/photon/authenticate
    ///      - NON spuntare "Allow anonymous clients"
    ///    - Salva le modifiche
    /// 
    /// 3. UNITY SETUP:
    ///    - Importa PlayFab SDK via Package Manager o .unitypackage
    ///    - Configura PlayFabSharedSettings asset con il tuo Title ID
    /// 
    /// SICUREZZA:
    /// - I token PlayFab/Photon NON vengono salvati in chiaro su disco
    /// - SessionTicket è mantenuto solo in memoria
    /// - DeviceUniqueIdentifier è sufficientemente sicuro per guest login
    /// - Per dati sensibili usa PlayFab Player Data con permesso "Private"
    /// </summary>
    public class PlayFabAuthService
    {
        // Costanti
        private const string DEVICE_ID_KEY = "Project51_DeviceId";
        private const string GUEST_NICKNAME_PREFIX = "Guest_";
        private const string IS_REGISTERED_KEY = "Project51_IsRegistered";
        
        // Stato
        public string PlayFabId { get; private set; }
        public string SessionTicket { get; private set; }
        public string PhotonCustomAuthToken { get; private set; }
        public string DisplayName { get; private set; }
        public event Action<string> OnDisplayNameChanged;
        public bool IsLoggedIn => !string.IsNullOrEmpty(SessionTicket);
        public bool IsAccountLinked { get; private set; }
        
        /// <summary>
        /// True se l'utente ha registrato username/email/password (non solo guest).
        /// Salvato in PlayerPrefs per persistenza.
        /// </summary>
        public bool IsRegistered
        {
            get => PlayerPrefs.GetInt(IS_REGISTERED_KEY, 0) == 1;
            private set
            {
                PlayerPrefs.SetInt(IS_REGISTERED_KEY, value ? 1 : 0);
                PlayerPrefs.Save();
                OnRegistrationStatusChanged?.Invoke(value);
            }
        } // Closing brace for IsRegistered property

        public string GetBestDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(DisplayName))
                return DisplayName;

            if (!string.IsNullOrWhiteSpace(PlayFabId))
                return $"{GUEST_NICKNAME_PREFIX}{PlayFabId.Substring(0, Math.Min(8, PlayFabId.Length))}";

            string deviceId = GetOrCreateDeviceId();
            if (!string.IsNullOrWhiteSpace(deviceId))
                return $"{GUEST_NICKNAME_PREFIX}{deviceId.Substring(0, Math.Min(8, deviceId.Length))}";

            return $"{GUEST_NICKNAME_PREFIX}Player";
        }
        
        // Eventi
        public event Action<string> OnLoginSuccess;
        public event Action<string> OnLoginError;
        public event Action<string> OnPhotonTokenReceived;
        public event Action<string> OnPhotonTokenError;
        public event Action<bool> OnAccountLinkStatusChanged;
        public event Action<bool> OnRegistrationStatusChanged;
        
        /// <summary>
        /// Esegue il login guest usando CustomID.
        /// Crea automaticamente un nuovo account se non esiste.
        /// </summary>
        /// <param name="onSuccess">Callback con PlayFabId.</param>
        /// <param name="onError">Callback con messaggio di errore.</param>
        public void LoginAsGuest(Action<string> onSuccess = null, Action<string> onError = null)
        {
            string deviceId = GetOrCreateDeviceId();
            
            Debug.Log($"[PlayFabAuth] Attempting guest login with device ID: {deviceId.Substring(0, 8)}...");
            
            var request = new LoginWithCustomIDRequest
            {
                CustomId = deviceId,
                CreateAccount = true, // Crea account se non esiste
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true,
                    GetUserAccountInfo = true
                }
            };
            
            PlayFabClientAPI.LoginWithCustomID(request,
                result => OnLoginSuccessInternal(result, onSuccess),
                error => OnLoginErrorInternal(error, onError)
            );
        }
        
        /// <summary>
        /// Ottiene il token di autenticazione Photon da PlayFab.
        /// Chiamare dopo un login riuscito.
        /// </summary>
        /// <param name="photonAppId">L'AppId di Photon PUN (NON Chat o Voice).</param>
        /// <param name="onSuccess">Callback con il token.</param>
        /// <param name="onError">Callback con messaggio di errore.</param>
        public void GetPhotonAuthenticationToken(string photonAppId, Action<string> onSuccess = null, Action<string> onError = null)
        {
            if (!IsLoggedIn)
            {
                string error = "Cannot get Photon token: not logged in to PlayFab";
                Debug.LogError($"[PlayFabAuth] {error}");
                onError?.Invoke(error);
                OnPhotonTokenError?.Invoke(error);
                return;
            }
            
            Debug.Log($"[PlayFabAuth] Requesting Photon authentication token...");
            
            var request = new GetPhotonAuthenticationTokenRequest
            {
                PhotonApplicationId = photonAppId
            };
            
            PlayFabClientAPI.GetPhotonAuthenticationToken(request,
                result =>
                {
                    PhotonCustomAuthToken = result.PhotonCustomAuthenticationToken;
                    Debug.Log("[PlayFabAuth] Photon token received successfully");
                    onSuccess?.Invoke(PhotonCustomAuthToken);
                    OnPhotonTokenReceived?.Invoke(PhotonCustomAuthToken);
                },
                error =>
                {
                    string errorMsg = $"Failed to get Photon token: {error.ErrorMessage}";
                    Debug.LogError($"[PlayFabAuth] {errorMsg}");
                    onError?.Invoke(errorMsg);
                    OnPhotonTokenError?.Invoke(errorMsg);
                }
            );
        }
        
        /// <summary>
        /// Collega l'account guest a Google (solo Android).
        /// </summary>
        /// <param name="serverAuthCode">Il Server Auth Code da Google Play Games.</param>
        /// <param name="forceLink">Se true, sovrascrive eventuali link esistenti.</param>
        /// <param name="onSuccess">Callback su successo.</param>
        /// <param name="onError">Callback con messaggio di errore.</param>
        public void LinkGoogleAccount(string serverAuthCode, bool forceLink, Action onSuccess = null, Action<string> onError = null)
        {
            if (!IsLoggedIn)
            {
                onError?.Invoke("Not logged in");
                return;
            }
            
            Debug.Log("[PlayFabAuth] Linking Google account...");
            
            var request = new LinkGoogleAccountRequest
            {
                ServerAuthCode = serverAuthCode,
                ForceLink = forceLink
            };
            
            PlayFabClientAPI.LinkGoogleAccount(request,
                result =>
                {
                    Debug.Log("[PlayFabAuth] Google account linked successfully");
                    IsAccountLinked = true;
                    OnAccountLinkStatusChanged?.Invoke(true);
                    onSuccess?.Invoke();
                },
                error =>
                {
                    string errorMsg = error.ErrorMessage;
                    
                    // Gestione errori specifici
                    if (error.Error == PlayFabErrorCode.LinkedAccountAlreadyClaimed)
                    {
                        errorMsg = "This Google account is already linked to another player. Use ForceLink to override.";
                    }
                    else if (error.Error == PlayFabErrorCode.AccountAlreadyLinked)
                    {
                        errorMsg = "This PlayFab account already has a Google account linked.";
                    }
                    
                    Debug.LogError($"[PlayFabAuth] Google link failed: {errorMsg}");
                    onError?.Invoke(errorMsg);
                }
            );
        }
        
        /// <summary>
        /// Collega l'account guest ad Apple (solo iOS).
        /// </summary>
        /// <param name="identityToken">L'Identity Token (JWT) da Apple Sign In.</param>
        /// <param name="forceLink">Se true, sovrascrive eventuali link esistenti.</param>
        /// <param name="onSuccess">Callback su successo.</param>
        /// <param name="onError">Callback con messaggio di errore.</param>
        public void LinkAppleAccount(string identityToken, bool forceLink, Action onSuccess = null, Action<string> onError = null)
        {
            if (!IsLoggedIn)
            {
                onError?.Invoke("Not logged in");
                return;
            }
            
            Debug.Log("[PlayFabAuth] Linking Apple account...");
            
            var request = new LinkAppleRequest
            {
                IdentityToken = identityToken,
                ForceLink = forceLink
            };
            
            PlayFabClientAPI.LinkApple(request,
                result =>
                {
                    Debug.Log("[PlayFabAuth] Apple account linked successfully");
                    IsAccountLinked = true;
                    OnAccountLinkStatusChanged?.Invoke(true);
                    onSuccess?.Invoke();
                },
                error =>
                {
                    string errorMsg = error.ErrorMessage;
                    
                    if (error.Error == PlayFabErrorCode.LinkedAccountAlreadyClaimed)
                    {
                        errorMsg = "This Apple account is already linked to another player.";
                    }
                    else if (error.Error == PlayFabErrorCode.AccountAlreadyLinked)
                    {
                        errorMsg = "This PlayFab account already has an Apple account linked.";
                    }
                    
                    Debug.LogError($"[PlayFabAuth] Apple link failed: {errorMsg}");
                    onError?.Invoke(errorMsg);
                }
            );
        }
        
        /// <summary>
        /// Aggiorna il display name del giocatore su PlayFab.
        /// </summary>
        /// <param name="newName">Nuovo nome da impostare.</param>
        /// <param name="onSuccess">Callback su successo.</param>
        /// <param name="onError">Callback con messaggio di errore.</param>
        public void UpdateDisplayName(string newName, Action onSuccess = null, Action<string> onError = null)
        {
            if (!IsLoggedIn)
            {
                onError?.Invoke("Not logged in");
                return;
            }
            
            var request = new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = newName
            };
            
            PlayFabClientAPI.UpdateUserTitleDisplayName(request,
                result =>
                {
                    DisplayName = result.DisplayName;
                    Debug.Log($"[PlayFabAuth] Display name updated to: {DisplayName}");
                    OnDisplayNameChanged?.Invoke(DisplayName);
                    onSuccess?.Invoke();
                },
                error =>
                {
                    Debug.LogError($"[PlayFabAuth] Failed to update display name: {error.ErrorMessage}");
                    onError?.Invoke(error.ErrorMessage);
                }
            );
        }
        
        /// <summary>
        /// Verifica se l'account ha provider collegati (Google/Apple).
        /// </summary>
        public void CheckAccountLinkStatus(Action<bool> onComplete)
        {
            if (!IsLoggedIn)
            {
                onComplete?.Invoke(false);
                return;
            }
            
            var request = new GetAccountInfoRequest();
            
            PlayFabClientAPI.GetAccountInfo(request,
                result =>
                {
                    var accountInfo = result.AccountInfo;

                    // Nel Client SDK, UserAccountInfo non espone una lista generica di linked accounts.
                    // Si deduce lo stato di link dai campi specifici del provider.
                    IsAccountLinked =
                        accountInfo != null &&
                        (accountInfo.GooglePlayGamesInfo != null || accountInfo.AppleAccountInfo != null);
                    
                    Debug.Log($"[PlayFabAuth] Account linked status: {IsAccountLinked}");
                    OnAccountLinkStatusChanged?.Invoke(IsAccountLinked);
                    onComplete?.Invoke(IsAccountLinked);
                },
                error =>
                {
                    Debug.LogWarning($"[PlayFabAuth] Failed to check link status: {error.ErrorMessage}");
                    onComplete?.Invoke(false);
                }
            );
        }
        
        /// <summary>
        /// Effettua il logout. Mantiene il device ID per futuro login.
        /// </summary>
        public void Logout()
        {
            PlayFabId = null;
            SessionTicket = null;
            PhotonCustomAuthToken = null;
            DisplayName = null;
            OnDisplayNameChanged?.Invoke(null);
            IsAccountLinked = false;
            
            // NON cancelliamo il device ID, così il prossimo login riprende lo stesso account guest
            // NON cancelliamo IsRegistered, rimane per identificare se era già registrato
            
            Debug.Log("[PlayFabAuth] Logged out");
        }
        
        #region Email/Password Registration (Protect Account)
        
        /// <summary>
        /// Registra l'account guest con username, email e password (AddUsernamePassword).
        /// L'utente deve già essere loggato come guest.
        /// </summary>
        /// <param name="username">Username (3-20 caratteri, alfanumerico).</param>
        /// <param name="email">Email valida.</param>
        /// <param name="password">Password (6-100 caratteri).</param>
        /// <param name="onSuccess">Callback su successo.</param>
        /// <param name="onError">Callback con messaggio di errore user-friendly.</param>
        public void RegisterWithUsernameEmailPassword(string username, string email, string password, 
            Action onSuccess = null, Action<string> onError = null)
        {
            if (!IsLoggedIn)
            {
                onError?.Invoke("Devi prima effettuare il login guest");
                return;
            }
            
            // Validazione base
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 20)
            {
                onError?.Invoke("Username deve essere tra 3 e 20 caratteri");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                onError?.Invoke("Inserisci un'email valida");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                onError?.Invoke("La password deve essere almeno 6 caratteri");
                return;
            }
            
            Debug.Log($"[PlayFabAuth] Registering account with username: {username}");
            
            var request = new AddUsernamePasswordRequest
            {
                Username = username,
                Email = email,
                Password = password
            };
            
            PlayFabClientAPI.AddUsernamePassword(request,
                result =>
                {
                    Debug.Log($"[PlayFabAuth] Account registered successfully! Username: {result.Username}");
                    
                    IsRegistered = true;
                    DisplayName = username;
                    
                    // Aggiorna display name su PlayFab
                    UpdateDisplayName(username);
                    
                    onSuccess?.Invoke();
                },
                error =>
                {
                    string errorMsg = GetUserFriendlyError(error);
                    Debug.LogError($"[PlayFabAuth] Registration failed: {error.ErrorMessage}");
                    onError?.Invoke(errorMsg);
                }
            );
        }
        
        /// <summary>
        /// Login con email e password (per utenti già registrati).
        /// Sostituisce la sessione guest corrente con quella dell'account registrato.
        /// </summary>
        /// <param name="email">Email dell'account.</param>
        /// <param name="password">Password dell'account.</param>
        /// <param name="onSuccess">Callback con PlayFabId su successo.</param>
        /// <param name="onError">Callback con messaggio di errore user-friendly.</param>
        public void LoginWithEmail(string email, string password, 
            Action<string> onSuccess = null, Action<string> onError = null)
        {
            // Validazione base
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                onError?.Invoke("Inserisci un'email valida");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(password))
            {
                onError?.Invoke("Inserisci la password");
                return;
            }
            
            Debug.Log($"[PlayFabAuth] Attempting email login for: {email}");
            
            var request = new LoginWithEmailAddressRequest
            {
                Email = email,
                Password = password,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true,
                    GetUserAccountInfo = true
                }
            };
            
            PlayFabClientAPI.LoginWithEmailAddress(request,
                result =>
                {
                    // Aggiorna stato come in LoginAsGuest
                    PlayFabId = result.PlayFabId;
                    SessionTicket = result.SessionTicket;
                    
                    var profile = result.InfoResultPayload?.PlayerProfile;
                    var accountInfo = result.InfoResultPayload?.AccountInfo;
                    
                    DisplayName = profile?.DisplayName ?? accountInfo?.Username ?? "Player";
                    OnDisplayNameChanged?.Invoke(DisplayName);
                    
                    IsAccountLinked = accountInfo != null &&
                        (accountInfo.GooglePlayGamesInfo != null || accountInfo.AppleAccountInfo != null);
                    
                    // L'utente ha fatto login con email => è registrato
                    IsRegistered = true;
                    
                    Debug.Log($"[PlayFabAuth] Email login successful! PlayFabId: {PlayFabId}");
                    
                    onSuccess?.Invoke(PlayFabId);
                    OnLoginSuccess?.Invoke(PlayFabId);
                },
                error =>
                {
                    string errorMsg = GetUserFriendlyError(error);
                    Debug.LogError($"[PlayFabAuth] Email login failed: {error.ErrorMessage}");
                    onError?.Invoke(errorMsg);
                    OnLoginError?.Invoke(errorMsg);
                }
            );
        }
        
        /// <summary>
        /// Controlla se l'utente corrente ha già registrato email/password.
        /// Utile dopo un guest login per sapere se mostrare opzione di registrazione.
        /// </summary>
        public void CheckRegistrationStatus(Action<bool> onComplete)
        {
            if (!IsLoggedIn)
            {
                onComplete?.Invoke(false);
                return;
            }
            
            var request = new GetAccountInfoRequest();
            
            PlayFabClientAPI.GetAccountInfo(request,
                result =>
                {
                    var accountInfo = result.AccountInfo;
                    
                    // Se ha PrivateInfo con Email, è registrato
                    bool hasEmail = accountInfo?.PrivateInfo?.Email != null;
                    bool hasUsername = !string.IsNullOrEmpty(accountInfo?.Username);
                    
                    IsRegistered = hasEmail || hasUsername;
                    
                    Debug.Log($"[PlayFabAuth] Registration status: {IsRegistered} (Email: {hasEmail}, Username: {hasUsername})");
                    onComplete?.Invoke(IsRegistered);
                },
                error =>
                {
                    Debug.LogWarning($"[PlayFabAuth] Failed to check registration status: {error.ErrorMessage}");
                    onComplete?.Invoke(IsRegistered); // Usa cache locale
                }
            );
        }
        
        /// <summary>
        /// Converte errori PlayFab in messaggi user-friendly in italiano.
        /// </summary>
        private string GetUserFriendlyError(PlayFabError error)
        {
            switch (error.Error)
            {
                case PlayFabErrorCode.InvalidEmailAddress:
                    return "Email non valida";
                case PlayFabErrorCode.InvalidPassword:
                    return "Password non corretta";
                case PlayFabErrorCode.InvalidEmailOrPassword:
                    return "Email o password non corretti";
                case PlayFabErrorCode.EmailAddressNotAvailable:
                    return "Questa email è già in uso";
                case PlayFabErrorCode.UsernameNotAvailable:
                    return "Questo username è già in uso";
                case PlayFabErrorCode.InvalidUsername:
                    return "Username non valido (usa solo lettere e numeri)";
                case PlayFabErrorCode.AccountNotFound:
                    return "Account non trovato";
                case PlayFabErrorCode.AccountBanned:
                    return "Account sospeso";
                case PlayFabErrorCode.InvalidParams:
                    return "Dati inseriti non validi";
                case PlayFabErrorCode.ServiceUnavailable:
                    return "Servizio temporaneamente non disponibile, riprova";
                default:
                    return $"Errore: {error.ErrorMessage}";
            }
        }
        
        #endregion
        
        // TODO: [GOOGLE PLAY GAMES] Quando avremo Google Play Console (25$):
        // 1. Installare com.google.play.games via Package Manager
        // 2. Configurare OAuth 2.0 Web Client ID nella Google Play Console
        // 3. In Unity: Window > Google Play Games > Setup > Android Setup
        // 4. PlayFab Dashboard: Add-ons > Google > Inserisci Client ID e Secret
        // 5. Implementare:
        //    - PlayGamesPlatform.Instance.Authenticate()
        //    - PlayGamesPlatform.Instance.RequestServerSideAccess(true, code => {...})
        //    - Chiamare PlayFabClientAPI.LoginWithGooglePlayGamesServices con ServerAuthCode
        //    - Oppure LinkGoogleAccount se già loggato guest
        
        // TODO: [APPLE SIGN IN] Quando avremo Apple Developer Program (99$/anno):
        // 1. Installare com.lupidan.apple-signin-unity via OpenUPM
        // 2. Apple Developer Portal: Abilita "Sign in with Apple" per l'App ID
        // 3. Xcode Capabilities: Aggiungi "Sign in with Apple"
        // 4. PlayFab Dashboard: Add-ons > Apple > Configura con Bundle ID
        // 5. Implementare:
        //    - IAppleAuthManager.LoginWithAppleId con credentialState check
        //    - Ottenere IdentityToken (JWT)
        //    - Chiamare PlayFabClientAPI.LoginWithApple o LinkApple
        
        #region Private Methods
        
        private void OnLoginSuccessInternal(LoginResult result, Action<string> onSuccess)
        {
            PlayFabId = result.PlayFabId;
            SessionTicket = result.SessionTicket;
            
            // Estrai info dal profilo se disponibili
            var profile = result.InfoResultPayload?.PlayerProfile;
            var accountInfo = result.InfoResultPayload?.AccountInfo;
            
            DisplayName = profile?.DisplayName;
            
            // Se non ha display name, ne generiamo uno guest
            if (string.IsNullOrEmpty(DisplayName))
            {
                DisplayName = GUEST_NICKNAME_PREFIX + PlayFabId.Substring(0, 6).ToUpper();
                
                // Aggiorna su PlayFab (fire and forget)
                UpdateDisplayName(DisplayName);
            }
            
            // Controlla se ha account collegati
            // Nel Client SDK, UserAccountInfo non ha una lista Link generica: usa info provider.
            IsAccountLinked =
                accountInfo != null &&
                (accountInfo.GooglePlayGamesInfo != null || accountInfo.AppleAccountInfo != null);
            
            bool isNewAccount = result.NewlyCreated;
            
            Debug.Log($"[PlayFabAuth] Login successful! PlayFabId: {PlayFabId}, NewAccount: {isNewAccount}, DisplayName: {DisplayName}");
            
            onSuccess?.Invoke(PlayFabId);
            OnLoginSuccess?.Invoke(PlayFabId);
        }
        
        private void OnLoginErrorInternal(PlayFabError error, Action<string> onError)
        {
            string errorMessage;

            if (error.Error == PlayFabErrorCode.AccountNotFound &&
                !string.IsNullOrEmpty(error.ErrorMessage) &&
                error.ErrorMessage.IndexOf("Player creations have been disabled", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                errorMessage =
                    "Impossibile creare l'account guest: la creazione giocatori è disabilitata in PlayFab per questa API.\n" +
                    "Apri PlayFab Game Manager > Settings > API Features e abilita la creazione account da Client (LoginWithCustomID CreateAccount=true).";
            }
            else
            {
                errorMessage = $"PlayFab login failed: {error.ErrorMessage}";
            }
            
            // Dettagli aggiuntivi per debug
            if (error.ErrorDetails != null)
            {
                foreach (var detail in error.ErrorDetails)
                {
                    errorMessage += $"\n  {detail.Key}: {string.Join(", ", detail.Value)}";
                }
            }
            
            Debug.LogError($"[PlayFabAuth] {errorMessage}");
            
            onError?.Invoke(errorMessage);
            OnLoginError?.Invoke(errorMessage);
        }
        
        /// <summary>
        /// Ottiene o crea un Device ID persistente.
        /// Usa SystemInfo.deviceUniqueIdentifier se disponibile,
        /// altrimenti genera un GUID salvato in PlayerPrefs.
        /// </summary>
        private string GetOrCreateDeviceId()
        {
            // Prima prova a recuperare un ID già salvato
            string savedId = PlayerPrefs.GetString(DEVICE_ID_KEY, null);
            
            if (!string.IsNullOrEmpty(savedId))
            {
                return savedId;
            }
            
            // Prova SystemInfo.deviceUniqueIdentifier
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            
            // Su alcune piattaforme/dispositivi potrebbe non essere disponibile
            if (string.IsNullOrEmpty(deviceId) || deviceId == SystemInfo.unsupportedIdentifier)
            {
                // Fallback: genera un GUID
                deviceId = Guid.NewGuid().ToString();
                Debug.Log("[PlayFabAuth] Generated new device GUID (deviceUniqueIdentifier not available)");
            }
            
            // Salva per persistenza
            PlayerPrefs.SetString(DEVICE_ID_KEY, deviceId);
            PlayerPrefs.Save();
            
            return deviceId;
        }
        
        #endregion
    }
}
