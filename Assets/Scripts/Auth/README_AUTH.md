# Sistema di Autenticazione PlayFab + Photon PUN

Questo sistema implementa login/registrazione per un gioco mobile Unity con:
- **PlayFab** come backend per identity + profilo/statistiche
- **Photon PUN** per networking/matchmaking real-time

## Obiettivo del prototipo (fase 0 / tester)

In questa fase vogliamo un sistema **testabile subito** su mobile, **senza** dipendenze da:

- Google Play Console (25$)
- Apple Developer Program (99$/anno)

Strategia:

- **Guest login PlayFab sempre disponibile** (device `CustomId` persistente)
- Possibilità di **“proteggere l’account”** tramite **registrazione PlayFab** con `username/email/password` (senza provider esterni)
- EXP/progressi **salvati in locale** (PlayerPrefs) con regola gameplay:
  - se non sei registrato: l’EXP va in **`pendingExp`** (così non perdi nulla, ma incentiva la registrazione)
  - quando ti registri / fai login: `pendingExp` viene automaticamente riscattata

## Architettura

```
AuthBootstrapper (Singleton, DontDestroyOnLoad)
    ?
    ??? PlayFabAuthService    - Login guest, token Photon, link account
    ?
    ??? PhotonAuthConnector   - Custom Auth Photon, connessione
    ?
    ??? ProfileService        - Profilo utente, statistiche
    ?
    ??? NativePlatformAuth    - Google/Apple Sign-In nativo

UI prototipo (fase 0):

```
AuthUIController (MonoBehaviour)
    - Pannello Guest/Start (Gioca + CTA registrazione/login)
    - Pannello Registrazione (username/email/password)
    - Pannello Login (email/password)
    - Messaggi stato/errore

PlayerProgressLocal (MonoBehaviour, DontDestroyOnLoad)
    - Exp/Level/PendingExp su PlayerPrefs
    - ClaimPendingExp quando l’utente diventa registrato
```
```

## Flusso di Autenticazione

```
1. Init
   ?
2. PlayFabLoginGuest (LoginWithCustomID, CreateAccount=true)
   ?
3. GetPhotonToken (GetPhotonAuthenticationToken)
   ?
4. PhotonConnect (Custom Auth con username + token)
   ?
5. Ready ?

### Flusso prototipo (Guest + Protezione account con email/password)

```
All'avvio:
  1) Guest login PlayFab (CustomId persistente)
  2) (Opzionale) Photon token + connect (se usi già AuthBootstrapper)
  3) Mostra UI minima:
      - "Gioca" (guest)
      - "Registrati" (AddUsernamePassword)
      - "Ho già un account" (LoginWithEmailAddress)

Durante il gameplay:
  - PlayerProgressLocal.TryAddExp(amount)
      - se registrato: aggiunge EXP
      - se guest: accumula pendingExp

Dopo registrazione/login:
  - IsRegistered = true (PlayerPrefs)
  - ClaimPendingExp() (converte pendingExp in EXP reale)
```
```

## Configurazione

### 1. PlayFab Setup

1. Crea un titolo su [PlayFab Game Manager](https://developer.playfab.com)
2. Copia il **Title ID** (es: `ABCD1`)
3. In Unity: `Assets/PlayFabSDK/Shared/Public/Resources/PlayFabSharedSettings.asset`
   - Inserisci il Title ID
4. In PlayFab Dashboard:
   - Settings > API Features > ? "Allow client to post player statistics"

#### Installazione PlayFab Unity SDK (runtime)

Nel progetto deve esserci il **PlayFab Client SDK runtime** (non solo le `PlayFabEditorExtensions`).

Metodo consigliato (UPM - Git URL):

1. Unity: `Window` > `Package Manager`
2. `+` > `Add package from git URL...`
3. Inserisci:
   - `https://github.com/PlayFab/UnitySDK.git?path=/Packages/PlayFabSDK`

Se dopo l'installazione vedi solo `Assets/PlayFabEditorExtensions` ma non hai i namespace `PlayFab.ClientModels` / la classe `PlayFabClientAPI`, allora il runtime SDK non è stato installato correttamente.

### 1b. Note importanti per questa fase (NO Google/Apple)

- **Non serve** configurare Google Play Games Services.
- **Non serve** Apple Sign In.
- L’unica cosa necessaria è che **PlayFab TitleId** sia configurato correttamente (via `PlayFabSharedSettings.asset`).
- La registrazione avviene con PlayFab client API `AddUsernamePassword`.

### 2. Photon Dashboard Setup

1. Vai su [Photon Dashboard](https://dashboard.photonengine.com)
2. Seleziona la tua app PUN > **Manage** > **Authentication**
3. Clicca **Custom Authentication** e configura:

| Campo | Valore |
|-------|--------|
| Authentication URL | `https://{YOUR_TITLE_ID}.playfabapi.com/photon/authenticate` |
| Allow anonymous clients | ? **Disabilitato** (consigliato) |

> **IMPORTANTE**: Sostituisci `{YOUR_TITLE_ID}` con il tuo PlayFab Title ID in minuscolo!

4. Salva le modifiche

### 3. Unity Setup

1. **PlayFab SDK**: Import via Package Manager o [download](https://docs.microsoft.com/en-us/gaming/playfab/sdks/unity3d/)

2. **Photon PUN 2**: Import da Asset Store

3. **PhotonServerSettings**: `Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset`
   - Inserisci il tuo Photon AppId

### 4. Setup Piattaforma (per "Proteggi Account")

#### Android - Google Play Games

1. Installa `com.google.play.games` via Package Manager
2. Google Play Console:
   - Crea credenziali OAuth 2.0 (tipo: Web Application)
   - Copia il **Web Client ID**
3. Unity: Window > Google Play Games > Setup > Android Setup
   - Inserisci Web Client ID
   - ? Request Server Auth Code
4. PlayFab Dashboard:
   - Add-ons > Google > Inserisci Google Client ID e Secret

#### iOS - Apple Sign In

1. Installa `com.lupidan.apple-signin-unity` via OpenUPM:
   ```
   openupm add com.lupidan.apple-signin-unity
   ```
2. Apple Developer Portal:
   - Abilita "Sign in with Apple" per il tuo App ID
3. Xcode Capabilities:
   - Aggiungi "Sign in with Apple"
4. PlayFab Dashboard:
   - Add-ons > Apple > Configura con il tuo Bundle ID

## Uso nel Progetto

### Setup Scena

1. Crea un GameObject vuoto chiamato `AuthSystem`
2. Aggiungi il componente `AuthBootstrapper`
3. (Opzionale) Crea UI e aggiungi `LoginGateUI` (UI esistente, legata al flow PlayFab+Photon)
4. (Per prototipo Guest/Register/Login) Crea un Canvas e aggiungi `AuthUIController`
5. (Per EXP locali) Crea un GameObject `ProgressLocal` e aggiungi `PlayerProgressLocal`.

> Nota: `PlayerProgressLocal` e `AuthBootstrapper` sono `DontDestroyOnLoad`. Mettili nella prima scena.

### Codice

```csharp
// Aspettare che l'auth sia pronta
void Start()
{
    if (AuthBootstrapper.Instance.IsReady)
    {
        OnAuthReady();
    }

### UI Prototipo (Guest/Login/Register con email/password)

File: `Assets/Scripts/Auth/AuthUIController.cs`

Questa UI è pensata per la fase 0:

- usa `UnityEngine.UI.InputField` e `UnityEngine.UI.Text` (no TMP obbligatorio)
- chiama direttamente PlayFab (`AddUsernamePassword` e `LoginWithEmailAddress`)
- salva `IsRegistered` in PlayerPrefs (`Project51_IsRegistered`)

#### Setup gerarchia consigliata

```
Canvas
  AuthUIController
  GuestPanel
    - PlayAsGuestButton
    - ShowRegisterButton
    - ShowLoginButton
    - GuestInfoText

  RegisterPanel
    - UsernameInput (InputField)
    - EmailInput (InputField)
    - PasswordInput (InputField)
    - RegisterButton
    - BackButton
    - StatusText

  LoginPanel
    - EmailInput (InputField)
    - PasswordInput (InputField)
    - LoginButton
    - BackButton
    - StatusText

  LoadingOverlay (opzionale)
    - LoadingText
```

#### Collegamenti in Inspector (`AuthUIController`)

- Panels:
  - `guestPanel` -> GameObject `GuestPanel`
  - `loginPanel` -> GameObject `LoginPanel`
  - `registerPanel` -> GameObject `RegisterPanel`
  - `mainCanvasGroup` (opzionale) -> CanvasGroup del Canvas

- Guest Panel:
  - `playAsGuestButton`
  - `showRegisterButton`
  - `showLoginButton`
  - `guestInfoText`

- Register Panel:
  - `registerUsernameInput`
  - `registerEmailInput`
  - `registerPasswordInput`
  - `registerButton`
  - `registerBackButton`
  - `registerStatusText`

- Login Panel:
  - `loginEmailInput`
  - `loginPasswordInput`
  - `loginButton`
  - `loginBackButton`
  - `loginStatusText`

- Loading:
  - `loadingOverlay` (opzionale)
  - `loadingText` (opzionale)

#### Esempio: avviare il gioco quando l’utente preme “Gioca”

Aggiungi uno script tipo `GameLaunchController` e collegalo:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project51.Auth
{
    public class AuthPlayButtonExample : MonoBehaviour
    {
        [SerializeField] private AuthUIController authUI;
        [SerializeField] private string sceneToLoad = "MainMenu";

        private void Awake()
        {
            if (authUI != null)
            {
                authUI.OnPlayPressed += () => SceneManager.LoadScene(sceneToLoad);
            }
        }
    }
}
```

> L’auth guest avviene già nel tuo `AuthBootstrapper`. Questa UI serve solo per (a) far entrare il tester e (b) registrare/login.
    else
    {
        AuthBootstrapper.Instance.OnAuthReady += OnAuthReady;
    }
}

void OnAuthReady()
{
    // Ora puoi usare Photon normalmente
    string playFabId = AuthBootstrapper.Instance.PlayFabAuth.PlayFabId;
    string nickname = AuthBootstrapper.Instance.PlayFabAuth.DisplayName;
    
    // Carica profilo
    var profile = AuthBootstrapper.Instance.Profile;
    Debug.Log($"Level: {profile.Level}, Wins: {profile.Wins}");
}
```

### Proteggere l'Account

```csharp
// Da UI o codice
AuthBootstrapper.Instance.ProtectAccount(
    onSuccess: () => Debug.Log("Account protetto!"),
    onError: error => Debug.LogError(error)
);

#### Proteggere l’account (fase 0, senza Google/Apple)

In questa fase la protezione avviene con:

- `PlayFabClientAPI.AddUsernamePassword` (username/email/password)

Questa feature è implementata in:

- `PlayFabAuthService.RegisterWithUsernameEmailPassword(...)`
- `AuthUIController` (via bottone “Registrati”)
```

### Aggiornare Statistiche

```csharp
var profile = AuthBootstrapper.Instance.Profile;

// Dopo una vittoria
profile.RecordGameResult(isWin: true, xpGained: 25);

// Aggiorna nickname
profile.UpdateNickname("NuovoNome");
```

## Sicurezza

### ? Best Practices Implementate

- **No password custom**: Usiamo solo CustomID (device) e provider OAuth
- **Token in memoria**: SessionTicket e PhotonToken non salvati su disco
- **Device ID persistente**: Salvato in PlayerPrefs (solo per identificare il device)
- **PlayFab valida i token**: La validazione avviene server-side

### ?? Raccomandazioni

1. **Non salvare mai token in chiaro** su disco o in log di produzione
2. **Usa HTTPS** (già gestito da PlayFab/Photon)
3. **Valida sempre server-side** per dati sensibili (punteggi, acquisti)
4. **Player Data Private** per dati sensibili dell'utente
5. **Rate limiting** su PlayFab per prevenire abuse

## Struttura File

```
Assets/Scripts/Auth/
??? AuthState.cs              - Enum stati macchina
??? IAuthUI.cs                - Interfaccia UI callback
??? AuthBootstrapper.cs       - Singleton principale, state machine
??? PlayFabAuthService.cs     - Login PlayFab, token Photon, link
??? PhotonAuthConnector.cs    - Connessione Photon con Custom Auth
??? ProfileService.cs         - Gestione profilo e statistiche
??? NativePlatformAuth.cs     - Google/Apple Sign-In nativo
??? LoginGateUI.cs            - UI controller esempio
??? AuthUIController.cs        - UI prototipo Guest/Login/Register (email/password)
??? PlayerProgressLocal.cs     - EXP/level/pendingExp locale su PlayerPrefs
??? README_AUTH.md            - Questa documentazione
```

## Troubleshooting

### "Custom authentication failed"

1. Verifica che l'URL su Photon Dashboard sia corretto
2. Verifica che il Title ID sia in minuscolo nell'URL
3. Controlla che PlayFab Add-on per Photon sia configurato

### "Server auth code is empty" (Android)

1. Verifica di usare il **Web Client ID** (non Android)
2. Assicurati che Google Play Games sia configurato correttamente
3. Il device deve avere Google Play Services aggiornato

### "Apple Sign In not available"

1. Richiede iOS 13+
2. Verifica le capabilities in Xcode
3. Il simulatore potrebbe non supportare Sign In

### Token scaduto / Disconnessione

Il sistema ha retry automatico con backoff esponenziale. Se persistono problemi:

1. Verifica connessione internet
2. Controlla status PlayFab/Photon
3. Il token Photon dura ~24h, viene rinnovato al reconnect

### Registrazione fallisce: email/username già in uso

Cause tipiche:

- Email già associata a un altro account
- Username già preso

Cosa vedere:

- `AuthUIController` mostra un messaggio user-friendly (es: "Questa email è già in uso")
- Se vuoi più dettaglio, controlla la Console (log PlayFab)

### EXP non aumenta

Se stai testando come guest:

- l’EXP NON va in `progress_exp`
- va in `progress_pendingExp`

Soluzione:

- registrati o fai login, poi `ClaimPendingExp()` viene eseguito automaticamente

## Integrazione con NetworkManager Esistente

Il tuo `NetworkManager` esistente può continuare a funzionare. Modifica solo:

```csharp
// In NetworkManager.cs - ConnectToPhoton()
public void ConnectToPhoton(string nickname = null)
{
    // Verifica che l'auth sia pronta
    if (!AuthBootstrapper.Instance.IsReady)
    {
        Debug.LogWarning("Auth not ready, waiting...");
        AuthBootstrapper.Instance.OnAuthReady += () => ConnectToPhoton(nickname);
        return;
    }
    
    // Il resto del codice rimane uguale
    // PhotonNetwork.AuthValues è già configurato da AuthBootstrapper
    ...
}
```

Oppure lascia che `AuthBootstrapper` gestisca la connessione iniziale e usa `NetworkManager` solo per matchmaking/room.

## TODO Futuro (non implementato ora)

### Google Play Games Services

Quando avremo Google Play Console:

- ottenere `ServerAuthCode` da GPGS
- fare `PlayFabClientAPI.LoginWithGooglePlayGamesServices` o `LinkGoogleAccount`

Punti già predisposti:

- TODO in `PlayFabAuthService`

### Sign in with Apple

Quando avremo Apple Developer:

- ottenere `IdentityToken` (JWT)
- fare `PlayFabClientAPI.LoginWithApple` o `LinkAppleAccount`

Punti già predisposti:

- TODO in `PlayFabAuthService`
