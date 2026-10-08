using System;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

namespace Project51.Auth
{
    /// <summary>
    /// Servizio per l'autenticazione PlayFab.
    /// Gestisce login guest, login con email/nome utente e recupero token Photon.
    /// 
    /// CONFIGURAZIONE RICHIESTA:
    /// 
    /// 1. PLAYFAB SETUP:
    ///    - Crea un titolo su PlayFab Game Manager (https://developer.playfab.com)
    ///    - Copia il Title ID e impostalo in PlayFabSettings (o via codice)
    ///    - In Settings > API Features: "Allow client to post player statistics" deve restare SPENTO
    ///      (le statistiche le scrive solo il server, CloudScript premioPartita)
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
        private const string GUEST_NICKNAME_PREFIX = "Ospite ";
        
        // Stato
        public string PlayFabId { get; private set; }
        public string SessionTicket { get; private set; }
        public string PhotonCustomAuthToken { get; private set; }
        public string DisplayName { get; private set; }
        public event Action<string> OnDisplayNameChanged;
        public bool IsLoggedIn => !string.IsNullOrEmpty(SessionTicket);
        /// <summary>Email dell'account con login vero (null per l'ospite): le Impostazioni la mostrano mascherata.</summary>
        public string Email { get; private set; }
        
        /// <summary>
        /// Vero solo dopo "Accedi" o "Registrati" in questa esecuzione (B9): la sessione di prima e' sempre un ospite usa e getta.
        /// Prima era un flag salvato sul telefono, letto come se descrivesse la sessione in corso.
        /// </summary>
        public bool HasRealLogin { get; private set; }

        /// <summary>B14 (S1): id nuovo a ogni login vero (Accedi, Registrati); il server tiene l'ultimo, gli altri telefoni escono.</summary>
        public string SessionId { get; private set; }

        // B9 (S3): CustomId dell'ospite nascosto, solo in memoria e nuovo dopo ogni uscita: non puo' mai essere un account vero
        // (prima un id salvato poteva riportare dentro l'account registrato da quella sessione, o l'account del dispositivo).
        private string sessionGuestId;

        /// <summary>
        /// Registrazione riuscita dall'interfaccia: da qui l'account e' un login vero, con email nota. B13 (S2): il nome scelto vale
        /// subito (e' lo Username, unico, quello che il prossimo login mostrerebbe), senza aspettare UpdateDisplayName.
        /// </summary>
        public void MarkRegistered(string email, string username = null)
        {
            HasRealLogin = true;
            SessionId = Guid.NewGuid().ToString("N");
            Email = email;
            if (string.IsNullOrWhiteSpace(username)) return;
            DisplayName = username;
            OnDisplayNameChanged?.Invoke(DisplayName);
        }

        /// <summary>
        /// Forces the current session to use a guest identity.
        /// Clears DisplayName so GetBestDisplayName() returns "Ospite XXXX".
        /// Also clears HasRealLogin.
        /// Call this when the user explicitly chooses "Play as Guest".
        /// </summary>
        public void ForceGuestIdentity()
        {
            DisplayName = null;
            HasRealLogin = false;
            OnDisplayNameChanged?.Invoke(null);
            Debug.Log($"[PlayFabAuth] Forced guest identity. Name will be: {GetBestDisplayName()}");
        }

        public string GetBestDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(DisplayName))
                return DisplayName;

            if (!string.IsNullOrWhiteSpace(PlayFabId))
                return GUEST_NICKNAME_PREFIX + ShortId(PlayFabId);

            return GUEST_NICKNAME_PREFIX.TrimEnd();
        }

        // Nome ospite leggibile: "Ospite 66B9" invece di "Guest_66B9B973".
        private static string ShortId(string id) => id.Replace("-", "").Substring(0, Math.Min(4, id.Replace("-", "").Length)).ToUpperInvariant();
        
        // Eventi
        public event Action<string> OnLoginSuccess;
        
        /// <summary>
        /// Esegue il login guest usando CustomID: un account usa e getta, lo stesso fino alla prossima uscita (Logout).
        /// </summary>
        /// <param name="onSuccess">Callback con PlayFabId.</param>
        /// <param name="onError">Callback con messaggio di errore.</param>
        public void LoginAsGuest(Action<string> onSuccess = null, Action<string> onError = null)
        {
            if (sessionGuestId == null) sessionGuestId = Guid.NewGuid().ToString("N");
            Debug.Log("[PlayFabAuth] Attempting guest login");

            var request = new LoginWithCustomIDRequest
            {
                CustomId = sessionGuestId,
                CreateAccount = true
            };
            
            PlayFabClientAPI.LoginWithCustomID(request,
                result => OnLoginSuccessInternal(result, onSuccess),
                error => OnLoginErrorInternal(error, onError)
            );
        }

        private void OnLoginSuccessInternal(LoginResult result, Action<string> onSuccess)
        {
            PlayFabId = result.PlayFabId;
            SessionTicket = result.SessionTicket;
            DisplayName = null;
            Email = null;
            HasRealLogin = false;

            OnDisplayNameChanged?.Invoke(DisplayName);

            Debug.Log("[PlayFabAuth] Guest login successful");
            onSuccess?.Invoke(PlayFabId);
            OnLoginSuccess?.Invoke(PlayFabId);
        }

        private void OnLoginErrorInternal(PlayFabError error, Action<string> onError)
        {
            string errorMsg = GetUserFriendlyError(error);
            Debug.LogError($"[PlayFabAuth] Guest login failed: {error.ErrorMessage}");
            onError?.Invoke(errorMsg);
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
                },
                error =>
                {
                    string errorMsg = $"Failed to get Photon token: {error.ErrorMessage}";
                    Debug.LogError($"[PlayFabAuth] {errorMsg}");
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
        /// Effettua il logout. Il prossimo LoginAsGuest e' un ospite nuovo; l'SDK dimentica il biglietto del vecchio account.
        /// </summary>
        public void Logout()
        {
            HasRealLogin = false; // prima dell'evento: chi lo ascolta (PublishLook) non deve vedere l'account vecchio come vero
            SessionId = null;
            sessionGuestId = null;
            PlayFabClientAPI.ForgetAllCredentials();
            PlayFabId = null;
            SessionTicket = null;
            PhotonCustomAuthToken = null;
            DisplayName = null;
            Email = null;
            OnDisplayNameChanged?.Invoke(null);
            Debug.Log("[PlayFabAuth] Logged out");
        }
        
        #region Email/Password Login
        
        /// <summary>
        /// Login con email (o nome utente scelto alla registrazione) e password, per utenti gia' registrati.
        /// Sostituisce la sessione guest corrente con quella dell'account registrato.
        /// </summary>
        /// <param name="email">Email dell'account, oppure il suo nome utente (senza @).</param>
        /// <param name="password">Password dell'account.</param>
        /// <param name="onSuccess">Callback con PlayFabId su successo.</param>
        /// <param name="onError">Callback con messaggio di errore user-friendly.</param>
        public void LoginWithEmail(string email, string password, 
            Action<string> onSuccess = null, Action<string> onError = null)
        {
            // Validazione base
            if (string.IsNullOrWhiteSpace(email))
            {
                onError?.Invoke("Inserisci l'email o il nome utente");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(password))
            {
                onError?.Invoke("Inserisci la password");
                return;
            }
            
            // B23 (I1): dopo troppi tentativi si aspetta (il tempo detto da PlayFab, altrimenti 60 s) senza chiamare il server.
            string key = email.Trim().ToLowerInvariant();
            int wait = Mathf.Max(SecondsLeft(key), SecondsLeft(""));
            if (wait > 0) { onError?.Invoke($"Troppi tentativi: riprova tra {wait} s"); return; }

            bool byEmail = email.Contains("@");
            Debug.Log($"[PlayFabAuth] Attempting {(byEmail ? "email" : "username")} login");
            
            var info = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true,
                GetUserAccountInfo = true
            };
            
            // Secondo giro 08/10 (scelta dell'utente): l'account entra solo se non e' gia' in uso su un altro telefono. Se lo e', si
            // torna all'ospite di prima e chi era dentro resta dentro.
            Action<LoginResult> success = result =>
            {
                string session = Guid.NewGuid().ToString("N");
                ModerationService.Claim(session, (busy, seconds) =>
                {
                    if (!busy) { EnterAccount(result, session, byEmail ? email : null, onSuccess); return; }
                    Debug.Log("[PlayFabAuth] Account in uso su un altro dispositivo: login rifiutato");
                    RestoreGuest(() => onError?.Invoke(ModerationService.BusyText(seconds)));
                });
            };
            Action<PlayFabError> failure = error =>
            {
                Debug.LogError($"[PlayFabAuth] Login failed: {error.ErrorMessage}");
                // Troppe password sbagliate: per quell'account. Troppe richieste dal telefono: per tutti, il tempo che dice PlayFab.
                if (error.Error == PlayFabErrorCode.FailedLoginAttemptRateLimitExceeded)
                    loginBlockedUntil[key] = Time.unscaledTime + (error.RetryAfterSeconds ?? 60);
                else if (error.Error == PlayFabErrorCode.APIClientRequestRateLimitExceeded || error.Error == PlayFabErrorCode.APIConcurrentRequestLimitExceeded)
                    loginBlockedUntil[""] = Time.unscaledTime + (error.RetryAfterSeconds ?? 10);
                onError?.Invoke(GetUserFriendlyError(error));
            };

            if (byEmail)
                PlayFabClientAPI.LoginWithEmailAddress(new LoginWithEmailAddressRequest
                    { Email = email, Password = password, InfoRequestParameters = info }, success, failure);
            else
                PlayFabClientAPI.LoginWithPlayFab(new LoginWithPlayFabRequest
                    { Username = email, Password = password, InfoRequestParameters = info }, success, failure);
        }

        /// <summary>Login rifiutato (account in uso): l'SDK torna sull'ospite di prima (stesso CustomId), per l'app non e' mai cambiato.</summary>
        private void RestoreGuest(Action done)
        {
            if (sessionGuestId == null) sessionGuestId = Guid.NewGuid().ToString("N");
            PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest { CustomId = sessionGuestId, CreateAccount = true }, r =>
            {
                PlayFabId = r.PlayFabId;
                SessionTicket = r.SessionTicket;
                done();
            }, e =>
            {
                Debug.LogWarning("[PlayFabAuth] Ritorno all'ospite fallito: " + e.ErrorMessage);
                done();
            });
        }

        private void EnterAccount(LoginResult result, string session, string typedEmail, Action<string> onSuccess)
        {
            // Aggiorna stato come in LoginAsGuest
            PlayFabId = result.PlayFabId;
            SessionTicket = result.SessionTicket;
            
            var profile = result.InfoResultPayload?.PlayerProfile;
            var accountInfo = result.InfoResultPayload?.AccountInfo;
            
            DisplayName = profile?.DisplayName ?? accountInfo?.Username ?? "Player";
            Email = accountInfo?.PrivateInfo?.Email ?? typedEmail;
            HasRealLogin = true; // prima dell'evento (S4): chi lo ascolta vede gia' l'account vero
            SessionId = session;
            OnDisplayNameChanged?.Invoke(DisplayName);
            // B13 (N2): account registrato senza nome visibile (aggiornamento fallito alla registrazione): lo si ripara ora.
            if (string.IsNullOrEmpty(profile?.DisplayName) && !string.IsNullOrEmpty(accountInfo?.Username))
                UpdateDisplayName(accountInfo.Username, null, e => Debug.LogWarning("[PlayFabAuth] Display name repair failed: " + e));
            
            Debug.Log("[PlayFabAuth] Login successful");
            
            // Prima l'evento: HomeConnectionWatcher deve far partire il rientro in partita prima che onSuccess entri in Home
            // (li' un segno di partita in corso senza rientro conta come abbandono).
            OnLoginSuccess?.Invoke(PlayFabId);
            onSuccess?.Invoke(PlayFabId);
        }
        
        private readonly System.Collections.Generic.Dictionary<string, float> loginBlockedUntil = new System.Collections.Generic.Dictionary<string, float>();

        private int SecondsLeft(string key) =>
            loginBlockedUntil.TryGetValue(key, out float until) ? Mathf.CeilToInt(until - Time.unscaledTime) : 0;

        /// <summary>
        /// Converte errori PlayFab in messaggi user-friendly in italiano. B23 (I1): credenziali sbagliate o account inesistente danno lo
        /// stesso messaggio (non si scopre se un account esiste); mai il testo inglese di PlayFab (resta nel log).
        /// </summary>
        public static string GetUserFriendlyError(PlayFabError error)
        {
            switch (error.Error)
            {
                case PlayFabErrorCode.InvalidEmailAddress:
                    return "Email non valida";
                case PlayFabErrorCode.InvalidPassword:
                case PlayFabErrorCode.InvalidEmailOrPassword:
                case PlayFabErrorCode.InvalidUsernameOrPassword:
                case PlayFabErrorCode.AccountNotFound:
                    return "Email/nome utente o password non corretti";
                case PlayFabErrorCode.FailedLoginAttemptRateLimitExceeded:
                    return $"Troppi tentativi con la password sbagliata: riprova tra {error.RetryAfterSeconds ?? 60} s o usa Password dimenticata?";
                case PlayFabErrorCode.APIClientRequestRateLimitExceeded:
                case PlayFabErrorCode.APIConcurrentRequestLimitExceeded:
                    return "Troppe richieste ravvicinate: riprova tra qualche secondo";
                case PlayFabErrorCode.ConnectionError:
                    return "Connessione assente: controlla la rete";
                case PlayFabErrorCode.EmailAddressNotAvailable:
                    return "Questa email è già in uso";
                case PlayFabErrorCode.UsernameNotAvailable:
                    return "Questo username è già in uso";
                case PlayFabErrorCode.InvalidUsername:
                    return "Username non valido (usa solo lettere e numeri)";
                case PlayFabErrorCode.AccountBanned:
                    return "Account sospeso";
                case PlayFabErrorCode.InvalidParams:
                    return "Dati inseriti non validi";
                case PlayFabErrorCode.ServiceUnavailable:
                    return "Servizio temporaneamente non disponibile, riprova";
                default:
                    return "Qualcosa non ha funzionato. Riprova.";
            }
        }
        
        #endregion
        
        // TODO Google/Apple: login di piattaforma NON implementato (NativePlatformAuth e i metodi Link* tolti in Fase 10).
        // Per aggiungerlo servono il plugin Google Play Games (RequestServerSideAccess -> LoginWithGooglePlayGamesServices o
        // LinkGoogleAccount) e Sign in with Apple (LoginWithApple o LinkApple), piu' la configurazione su Play Console,
        // Apple Developer e PlayFab Add-ons.
        
    }
}
