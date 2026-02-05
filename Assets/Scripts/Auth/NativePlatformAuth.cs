using System;
using UnityEngine;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

#if UNITY_IOS
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Interfaces;
using AppleAuth.Native;
#endif

namespace Project51.Auth
{
    /// <summary>
    /// Gestisce l'autenticazione nativa con provider di piattaforma (Google/Apple).
    /// Fornisce auth code/token da usare con PlayFab per proteggere l'account.
    /// 
    /// SETUP RICHIESTO:
    /// 
    /// ANDROID (Google Play Games):
    /// 1. Installa package: com.google.play.games (Google Play Games plugin for Unity)
    /// 2. Configura Google Play Console: crea OAuth 2.0 client ID (web application)
    /// 3. In Unity: Window > Google Play Games > Setup > Android Setup
    /// 4. Inserisci Web Client ID (non Android client ID!)
    /// 5. Abilita "Request Server Auth Code" nelle impostazioni
    /// 
    /// iOS (Sign in with Apple):
    /// 1. Installa package: com.lupidan.apple-signin-unity (via OpenUPM o GitHub)
    /// 2. In Apple Developer Portal: abilita Sign in with Apple per l'App ID
    /// 3. In Xcode capabilities: aggiungi "Sign in with Apple"
    /// 4. Il plugin gestirà automaticamente l'identity token
    /// </summary>
    public class NativePlatformAuth : MonoBehaviour
    {
        public static NativePlatformAuth Instance { get; private set; }

#if UNITY_IOS
        private IAppleAuthManager _appleAuthManager;
#endif

        public bool IsGoogleAvailable { get; private set; }
        public bool IsAppleAvailable { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePlatformAuth();
        }

        private void InitializePlatformAuth()
        {
#if UNITY_ANDROID
            try
            {
                // Configura Google Play Games
                PlayGamesPlatform.DebugLogEnabled = Debug.isDebugBuild;
                
                PlayGamesPlatform.Activate();
                
                IsGoogleAvailable = true;
                Debug.Log("[NativePlatformAuth] Google Play Games initialized");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NativePlatformAuth] Google Play Games not available: {e.Message}");
                IsGoogleAvailable = false;
            }
#endif

#if UNITY_IOS
            try
            {
                // Apple Sign In è disponibile da iOS 13+
                if (AppleAuthManager.IsCurrentPlatformSupported)
                {
                    var deserializer = new PayloadDeserializer();
                    _appleAuthManager = new AppleAuthManager(deserializer);
                    IsAppleAvailable = true;
                    Debug.Log("[NativePlatformAuth] Apple Sign In initialized");
                }
                else
                {
                    Debug.LogWarning("[NativePlatformAuth] Apple Sign In not supported on this iOS version");
                    IsAppleAvailable = false;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NativePlatformAuth] Apple Sign In not available: {e.Message}");
                IsAppleAvailable = false;
            }
#endif
        }

        private void Update()
        {
#if UNITY_IOS
            // Apple Auth Manager richiede update ogni frame
            _appleAuthManager?.Update();
#endif
        }

        /// <summary>
        /// Richiede l'autenticazione Google e restituisce il Server Auth Code.
        /// </summary>
        /// <param name="onSuccess">Callback con il Server Auth Code.</param>
        /// <param name="onFailure">Callback in caso di errore.</param>
        public void RequestGoogleAuthCode(Action<string> onSuccess, Action<string> onFailure)
        {
#if UNITY_ANDROID
            if (!IsGoogleAvailable)
            {
                onFailure?.Invoke("Google Play Games not available");
                return;
            }

            Debug.Log("[NativePlatformAuth] Requesting Google Sign In...");
            
            Social.localUser.Authenticate((success) =>
            {
                if (success)
                {
                }
                else
                {
                    Debug.LogError("[NativePlatformAuth] Google Sign In failed");
                    onFailure?.Invoke("Google Sign In failed or cancelled");
                }
            });
#else
            onFailure?.Invoke("Google Sign In is only available on Android");
#endif
        }

        /// <summary>
        /// Richiede l'autenticazione Apple e restituisce l'Identity Token.
        /// </summary>
        /// <param name="onSuccess">Callback con l'Identity Token (JWT).</param>
        /// <param name="onFailure">Callback in caso di errore.</param>
        public void RequestAppleIdentityToken(Action<string> onSuccess, Action<string> onFailure)
        {
#if UNITY_IOS
            if (!IsAppleAvailable || _appleAuthManager == null)
            {
                onFailure?.Invoke("Apple Sign In not available");
                return;
            }

            Debug.Log("[NativePlatformAuth] Requesting Apple Sign In...");
            
            var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);
            
            _appleAuthManager.LoginWithAppleId(
                loginArgs,
                credential =>
                {
                    var appleIdCredential = credential as IAppleIDCredential;
                    if (appleIdCredential != null)
                    {
                        // Identity Token è un JWT che PlayFab può validare
                        string identityToken = System.Text.Encoding.UTF8.GetString(
                            appleIdCredential.IdentityToken, 
                            0, 
                            appleIdCredential.IdentityToken.Length
                        );
                        
                        Debug.Log("[NativePlatformAuth] Apple identity token obtained");
                        onSuccess?.Invoke(identityToken);
                    }
                    else
                    {
                        onFailure?.Invoke("Invalid Apple credential type");
                    }
                },
                error =>
                {
                    var authorizationErrorCode = error.GetAuthorizationErrorCode();
                    Debug.LogError($"[NativePlatformAuth] Apple Sign In failed: {authorizationErrorCode}");
                    
                    string errorMessage = authorizationErrorCode switch
                    {
                        AuthorizationErrorCode.Canceled => "Sign in cancelled",
                        AuthorizationErrorCode.Failed => "Sign in failed",
                        AuthorizationErrorCode.NotHandled => "Sign in not handled",
                        AuthorizationErrorCode.Unknown => "Unknown error",
                        _ => $"Error: {authorizationErrorCode}"
                    };
                    
                    onFailure?.Invoke(errorMessage);
                }
            );
#else
            onFailure?.Invoke("Apple Sign In is only available on iOS");
#endif
        }

        /// <summary>
        /// Richiede l'autenticazione appropriata per la piattaforma corrente.
        /// Android: Google, iOS: Apple.
        /// </summary>
        public void RequestPlatformAuth(Action<string, PlatformAuthType> onSuccess, Action<string> onFailure)
        {
#if UNITY_ANDROID
            RequestGoogleAuthCode(
                authCode => onSuccess?.Invoke(authCode, PlatformAuthType.Google),
                onFailure
            );
#elif UNITY_IOS
            RequestAppleIdentityToken(
                token => onSuccess?.Invoke(token, PlatformAuthType.Apple),
                onFailure
            );
#else
            onFailure?.Invoke("Platform authentication not available on this platform");
#endif
        }
    }

    public enum PlatformAuthType
    {
        None,
        Google,
        Apple
    }
}
