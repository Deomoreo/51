using PlayFab;
using PlayFab.ClientModels;
using Project51.Auth;
using Project51.Core;
using Project51.Unity;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Collegamenti in piu' delle schermate Accesso (mockup 23) e Registrazione (mockup 24). La
    /// logica PlayFab resta dov'era (AuthUIController per accesso e registrazione,
    /// PlayFabAuthService per l'ospite): qui stanno solo i pezzi che il controller storico non
    /// prevedeva, perche' nel suo flusso vivevano su un pannello separato o non esistevano -
    /// indietro alla schermata iniziale, accesso come ospite, rimandi fra le due schermate,
    /// password dimenticata, conferma password, forza della password e accettazione dei Termini.
    /// Grafica costruita da Tools/UIV2/Build Auth Screens.
    /// </summary>
    public sealed class AuthScreensV2 : MonoBehaviour
    {
        public AuthUIController Auth;
        public StartScreenV2 StartScreen;

        [Header("Accesso (mockup 23)")]
        public Button LoginBack;
        public Button LoginGuest;
        public Button LoginToRegister;
        public Button LoginForgot;
        public TMP_InputField LoginEmail;
        public TMP_Text LoginStatus;
        [Tooltip("\"oppure\" + ACCEDI COME OSPITE: spariscono dopo l'ingresso, quando si e' gia' ospite.")]
        public GameObject[] LoginGuestOnly;
        [Tooltip("Righe sotto al pulsante ospite (Non hai un account? / REGISTRATI): salgono al suo posto.")]
        public RectTransform[] LoginBelowGuest;
        public float LoginGuestHiddenShift = 224f;

        [Header("Registrazione (mockup 24)")]
        public GameObject RegisterPanel;
        public Button RegisterBack;
        public Button RegisterToLogin;
        public Button RegisterSubmit;
        public TMP_InputField RegisterUsername;
        public TMP_InputField RegisterEmail;
        public TMP_InputField RegisterPassword;
        public TMP_InputField RegisterConfirm;
        public TMP_Text RegisterStatus;
        [Tooltip("Quadratino dei Termini: il segno di spunta si accende e si spegne al tocco.")]
        public Button TermsToggle;
        public GameObject TermsCheck;
        [Tooltip("Le due parole cliccabili della riga Termini: aprono i documenti da leggere.")]
        public Button TermsLink;
        public Button PrivacyLink;

        [Header("Il tuo account (solo login vero)")]
        public GameObject AccountPanel;
        [Tooltip("Nome grande in alto; mostra lo stesso valore di AccountUsername.")]
        public TMP_Text AccountName;
        public TMP_Text AccountStatus;
        public TMP_Text AccountUsername;
        public TMP_Text AccountEmail;
        public TMP_Text AccountId;
        public Button AccountLogout;

        [Header("Documenti legali")]
        public LegalModalV2 Legal;
        [Tooltip("Le quattro tacche di 'Sicurezza password', da sinistra a destra.")]
        public Image[] StrengthBars;
        [Tooltip("Bagliore dietro REGISTRATI: si spegne quando il pulsante non e' ancora premibile.")]
        public Image RegisterGlow;

        /// <summary>Lunghezza minima accettata da PlayFab.</summary>
        public const int MinimumPasswordLength = 6;

        private static readonly Color StatusError = new Color32(240, 120, 138, 255);
        private static readonly Color StatusInfo = new Color32(150, 200, 170, 255);
        private static readonly Color StrengthOn = new Color32(74, 222, 128, 255);
        private static readonly Color StrengthOff = new Color32(30, 42, 68, 255);

        /// <summary>Opacita' del bagliore quando il pulsante e' premibile (come la costruisce il builder).</summary>
        private const float GlowAlpha = 150f / 255f;

        private bool sendingRecovery;
        private bool termsAccepted;
        private bool registerWasOpen;
        private bool guestRowsHidden;
        private bool accountWasOpen;
        private bool loggingOut;

        private void Awake()
        {
            if (LoginBack != null) LoginBack.onClick.AddListener(Back);
            if (LoginGuest != null) LoginGuest.onClick.AddListener(Guest);
            if (LoginToRegister != null) LoginToRegister.onClick.AddListener(GoToRegister);
            if (LoginForgot != null) LoginForgot.onClick.AddListener(ForgotPassword);

            if (AccountLogout != null) AccountLogout.onClick.AddListener(Logout);

            if (RegisterBack != null) RegisterBack.onClick.AddListener(Back);
            if (RegisterToLogin != null) RegisterToLogin.onClick.AddListener(GoToLogin);
            if (TermsToggle != null) TermsToggle.onClick.AddListener(ToggleTerms);
            // I due link stanno sopra alla riga che accetta i Termini: toccarli apre il documento,
            // non spunta la casella. Leggere non e' accettare.
            if (TermsLink != null) TermsLink.onClick.AddListener(() => { if (Legal != null) Legal.ShowTerms(); });
            if (PrivacyLink != null) PrivacyLink.onClick.AddListener(() => { if (Legal != null) Legal.ShowPrivacy(); });
            foreach (var field in new[] { RegisterUsername, RegisterEmail, RegisterPassword, RegisterConfirm })
            {
                if (field != null) field.onValueChanged.AddListener(_ => RefreshRegisterForm());
            }

            ClearStatus();
            ResetRegisterForm();
        }

        /// <summary>
        /// Il pannello registrazione viene acceso da AuthUIController, che non sa dell'accettazione
        /// dei Termini: la spunta va rimessa a zero ogni volta che la schermata si riapre, perche'
        /// l'accettazione deve essere un gesto dell'utente, non un residuo della volta prima.
        /// </summary>
        private void Update()
        {
            RefreshGuestRows();
            bool accountOpen = AccountPanel != null && AccountPanel.activeInHierarchy;
            if (accountOpen != accountWasOpen)
            {
                accountWasOpen = accountOpen;
                if (accountOpen) FillAccount();
            }
            bool open = RegisterPanel != null && RegisterPanel.activeInHierarchy;
            if (open == registerWasOpen) return;
            registerWasOpen = open;
            if (open) ResetRegisterForm();
        }

        /// <summary>
        /// Chi e' gia' entrato come ospite non deve poter "rientrare come ospite" dall'Accesso aperto
        /// dal Profilo o dalle Opzioni: il pulsante e la riga "oppure" spariscono e il rimando alla
        /// registrazione sale al loro posto. Accedere con un account resta possibile.
        /// </summary>
        private void RefreshGuestRows()
        {
            bool hide = StartScreen != null && StartScreen.HasEntered;
            if (hide == guestRowsHidden) return;
            guestRowsHidden = hide;
            if (LoginGuestOnly != null)
                foreach (var go in LoginGuestOnly) if (go != null) go.SetActive(!hide);
            if (LoginBelowGuest != null)
                foreach (var rect in LoginBelowGuest)
                    if (rect != null) rect.anchoredPosition += new Vector2(0f, hide ? LoginGuestHiddenShift : -LoginGuestHiddenShift);
        }

        // ------------------------------------------------------------------
        // Il tuo account
        // ------------------------------------------------------------------

        private const string Missing = "—";

        /// <summary>
        /// Dati dell'account: nome e ID dalla sessione, nome utente ed email da PlayFab
        /// (GetAccountInfo), che li conosce anche se l'accesso e' avvenuto su un altro dispositivo.
        /// </summary>
        private void FillAccount()
        {
            var auth = AuthBootstrapper.Instance?.PlayFabAuth;
            if (AccountStatus != null) AccountStatus.text = "Account registrato";
            SetAccountName(auth?.GetBestDisplayName() ?? Missing);
            if (AccountId != null) AccountId.text = string.IsNullOrEmpty(auth?.PlayFabId) ? Missing : auth.PlayFabId;
            if (AccountEmail != null) AccountEmail.text = "…";
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                if (AccountEmail != null) AccountEmail.text = Missing;
                return;
            }
            PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(),
                result =>
                {
                    if (this == null) return;
                    var info = result.AccountInfo;
                    if (AccountEmail != null) AccountEmail.text = string.IsNullOrEmpty(info?.PrivateInfo?.Email) ? Missing : info.PrivateInfo.Email;
                    if (!string.IsNullOrEmpty(info?.Username)) SetAccountName(info.Username);
                },
                error =>
                {
                    if (this == null) return;
                    if (AccountEmail != null) AccountEmail.text = Missing;
                    if (Debug.isDebugBuild) Debug.LogWarning("[AuthScreensV2] GetAccountInfo fallita: " + error.GenerateErrorReport());
                });
        }

        private void SetAccountName(string value)
        {
            if (AccountUsername != null) AccountUsername.text = value;
            if (AccountName != null) AccountName.text = value;
        }

        /// <summary>
        /// Esci dall'account: chiude la sessione (e Photon), dimentica il login e torna alla schermata
        /// iniziale, dove si sceglie di nuovo fra ospite, accesso e registrazione. Il vecchio
        /// AuthUIController.Logout riportava invece al pannello Ospite legacy.
        /// </summary>
        private void Logout()
        {
            if (loggingOut) return;
            loggingOut = true;
            var bootstrapper = AuthBootstrapper.Instance;
            if (bootstrapper != null) bootstrapper.LogoutAndRestart(clearRealAccountFlag: true);
            if (!AppLoading.LoadScene(AppFlowManager.SCENE_MAIN_MENU)) SceneManager.LoadScene(AppFlowManager.SCENE_MAIN_MENU);
        }

        // ------------------------------------------------------------------
        // Navigazione
        // ------------------------------------------------------------------

        /// <summary>
        /// Indietro: chiude l'UI di accesso e scopre quello che c'era sotto. Dalla schermata
        /// iniziale torna alla schermata iniziale, dal Profilo torna al Profilo - non serve
        /// distinguere i due casi perche' il pannello e' su un Canvas sopra a entrambi.
        /// </summary>
        private void Back()
        {
            ClearStatus();
            if (Auth != null) Auth.HideAuthUI();
        }

        /// <summary>
        /// "ACCEDI COME OSPITE": stessa identita' temporanea del pulsante Ospite della schermata
        /// iniziale. Se il gioco e' gia' entrato (accesso aperto dal Profilo) non si rientra in
        /// Home: si chiude e basta, altrimenti si ripeterebbe l'ingresso a partita in corso.
        /// </summary>
        private void Guest()
        {
            ClearStatus();
            if (StartScreen != null && !StartScreen.HasEntered)
            {
                StartScreen.PlayAsGuest();
                return;
            }
            if (Auth != null) Auth.HideAuthUI();
        }

        private void GoToRegister()
        {
            ClearStatus();
            if (Auth != null) Auth.ShowRegisterPanel();
        }

        private void GoToLogin()
        {
            ClearStatus();
            if (Auth != null) Auth.ShowLoginPanel();
        }

        // ------------------------------------------------------------------
        // Accesso
        // ------------------------------------------------------------------

        /// <summary>
        /// Risposta uguale sia che l'email esista sia che non esista: se dicessimo "questo indirizzo
        /// non e' registrato" regaleremmo a chiunque un modo per scoprire chi ha un account qui.
        /// </summary>
        private const string RecoverySent =
            "Se l'indirizzo è associato a un account 51, riceverai un'email con le istruzioni per reimpostare la password.";

        /// <summary>
        /// Password dimenticata: PlayFab manda l'email di recupero all'indirizzo gia' scritto nel
        /// primo campo, usando il modello configurato in AppConfig (vuoto = modello predefinito del
        /// titolo). Gli errori del servizio finiscono nel log per noi, mai a schermo per l'utente:
        /// un messaggio tecnico di PlayFab non lo aiuta e racconta piu' del dovuto.
        /// </summary>
        private void ForgotPassword()
        {
            if (sendingRecovery) return;

            string email = LoginEmail != null ? (LoginEmail.text ?? string.Empty).Trim() : string.Empty;
            if (!LooksLikeEmail(email))
            {
                SetStatus(LoginStatus, "Scrivi prima la tua email nel primo campo.", true);
                return;
            }

            sendingRecovery = true;
            SetStatus(LoginStatus, "Invio in corso...", false);
            var request = new SendAccountRecoveryEmailRequest { Email = email, TitleId = PlayFabSettings.TitleId };
            string template = AppConfig.RecoveryEmailTemplateId;
            if (!string.IsNullOrEmpty(template)) request.EmailTemplateId = template;

            PlayFabClientAPI.SendAccountRecoveryEmail(request,
                _ =>
                {
                    sendingRecovery = false;
                    SetStatus(LoginStatus, RecoverySent, false);
                },
                error =>
                {
                    sendingRecovery = false;
                    Debug.LogWarning("[AuthScreensV2] Recupero password: " + (error != null ? error.GenerateErrorReport() : "errore sconosciuto"));
                    // Indirizzo sconosciuto o malformato: stessa risposta del caso riuscito.
                    bool aboutTheAddress = error != null
                        && (error.Error == PlayFabErrorCode.AccountNotFound || error.Error == PlayFabErrorCode.InvalidEmailAddress);
                    SetStatus(LoginStatus,
                        aboutTheAddress ? RecoverySent : "Non è stato possibile completare la richiesta. Riprova tra poco.",
                        !aboutTheAddress);
                });
        }

        // ------------------------------------------------------------------
        // Registrazione
        // ------------------------------------------------------------------

        private void ToggleTerms()
        {
            termsAccepted = !termsAccepted;
            if (TermsCheck != null) TermsCheck.SetActive(termsAccepted);
            RefreshRegisterForm();
        }

        private void ResetRegisterForm()
        {
            termsAccepted = false;
            if (TermsCheck != null) TermsCheck.SetActive(false);
            // AuthUIController svuota i tre campi che conosce; la conferma e' nostra e senza questo
            // restava scritta dalla volta prima, sotto un campo password ormai vuoto.
            if (RegisterConfirm != null) RegisterConfirm.SetTextWithoutNotify(string.Empty);
            ClearStatus();
            RefreshRegisterForm();
        }

        /// <summary>
        /// Tacche di sicurezza, stato del pulsante REGISTRATI e motivo per cui non si puo' ancora
        /// premere. Il pulsante e' l'unico agganciato a AuthUIController: spegnendolo si evita che
        /// parta la chiamata a PlayFab con una conferma sbagliata o senza i Termini accettati.
        /// </summary>
        private void RefreshRegisterForm()
        {
            string username = Text(RegisterUsername);
            string email = Text(RegisterEmail);
            string password = RegisterPassword != null ? RegisterPassword.text ?? string.Empty : string.Empty;
            string confirm = RegisterConfirm != null ? RegisterConfirm.text ?? string.Empty : string.Empty;

            UpdateStrengthBars(password);

            bool complete = username.Length >= 3 && LooksLikeEmail(email)
                && password.Length >= MinimumPasswordLength && confirm == password && termsAccepted;
            if (RegisterSubmit != null) RegisterSubmit.interactable = complete;
            // Il bagliore segue il pulsante: acceso dietro a un pulsante spento sembrerebbe un invito
            // a premere qualcosa che non risponde.
            if (RegisterGlow != null)
            {
                var glow = RegisterGlow.color;
                glow.a = complete ? GlowAlpha : 0f;
                RegisterGlow.color = glow;
            }

            // Il messaggio compare solo quando c'e' qualcosa da dire: un modulo ancora vuoto non
            // deve gia' sembrare sbagliato.
            if (complete) { Clear(RegisterStatus); return; }
            if (username.Length > 0 && username.Length < 3) { SetStatus(RegisterStatus, "Il nome utente deve avere almeno 3 caratteri.", true); return; }
            if (email.Length > 0 && !LooksLikeEmail(email)) { SetStatus(RegisterStatus, "Controlla l'indirizzo email.", true); return; }
            if (password.Length > 0 && password.Length < MinimumPasswordLength) { SetStatus(RegisterStatus, "La password deve avere almeno " + MinimumPasswordLength + " caratteri.", true); return; }
            if (confirm.Length > 0 && confirm != password) { SetStatus(RegisterStatus, "Le due password non coincidono.", true); return; }
            if (!termsAccepted && username.Length > 0 && password.Length > 0) { SetStatus(RegisterStatus, "Accetta i Termini per continuare.", false); return; }
            Clear(RegisterStatus);
        }

        private void UpdateStrengthBars(string password)
        {
            if (StrengthBars == null) return;
            int score = Strength(password);
            for (int i = 0; i < StrengthBars.Length; i++)
            {
                if (StrengthBars[i] != null) StrengthBars[i].color = i < score ? StrengthOn : StrengthOff;
            }
        }

        /// <summary>Quante tacche accendere: da 0 (vuota o troppo corta) a 4.</summary>
        public static int Strength(string password)
        {
            if (string.IsNullOrEmpty(password)) return 0;
            bool letter = false, digit = false, other = false;
            foreach (char c in password)
            {
                if (char.IsLetter(c)) letter = true;
                else if (char.IsDigit(c)) digit = true;
                else other = true;
            }
            int score = 0;
            if (password.Length >= MinimumPasswordLength) score++;
            if (password.Length >= 10) score++;
            if (letter && digit) score++;
            if (other) score++;
            return score;
        }

        /// <summary>Controllo minimo: una chiocciola in mezzo e un punto dopo, col resto attorno.</summary>
        public static bool LooksLikeEmail(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            int at = value.IndexOf('@');
            if (at <= 0) return false;
            int dot = value.IndexOf('.', at);
            return dot > at + 1 && dot < value.Length - 1;
        }

        // ------------------------------------------------------------------

        private static string Text(TMP_InputField field) => field != null ? (field.text ?? string.Empty).Trim() : string.Empty;

        /// <summary>
        /// AuthUIController spegne l'oggetto del messaggio quando lo svuota e lo riaccende quando
        /// scrive: qui si segue la stessa convenzione, altrimenti un messaggio scritto dopo una
        /// pulizia resterebbe invisibile.
        /// </summary>
        private void SetStatus(TMP_Text label, string message, bool isError)
        {
            if (label == null) return;
            label.text = message;
            label.color = isError ? StatusError : StatusInfo;
            label.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        private void ClearStatus()
        {
            Clear(LoginStatus);
            Clear(RegisterStatus);
        }

        private static void Clear(TMP_Text label)
        {
            if (label == null) return;
            label.text = string.Empty;
            label.gameObject.SetActive(false);
        }
    }
}
