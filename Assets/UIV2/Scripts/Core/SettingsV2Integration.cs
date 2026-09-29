using Project51.Auth;
using Project51.Core;
using Project51.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UI51Toggle = Project51.UI51.UI51Toggle;

namespace Project51.UIV2.Core
{
    public sealed class SettingsV2Integration : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dimmerButton;
        [SerializeField] private Button accountButton;
        [SerializeField] private SimpleToggleSwitch audioToggle;
        [SerializeField] private SimpleToggleSwitch musicToggle;
        [SerializeField] private SimpleToggleSwitch vibrationToggle;
        [SerializeField] private SimpleToggleSwitch reducedGraphicsToggle;
        [SerializeField] private TMP_Text accountName;
        [SerializeField] private TMP_Text accountSubtitle;
        [SerializeField] private AuthUIController authUI;
        [Header("Account -> Elimina account (H7)")]
        [SerializeField] private Button deleteAccountButton;
        [SerializeField] private DeleteAccountModalV2 deleteAccount;

        [Header("Sezione Account: solo dopo l'ingresso (ospite o login)")]
        [Tooltip("Finche' non si e' entrati (HasEntered falso) la riga Account non compare.")]
        [SerializeField] private StartScreenV2 startScreen;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private TMP_Text accountHeader;
        [Tooltip("Righe sotto ad Account (Lingua, Supporto): salgono quando Account e' nascosto.")]
        [SerializeField] private RectTransform[] rowsAfterAccount;
        [SerializeField] private RectTransform footer;

        [Header("UI51 (opzionali): vuoti = aspetto classico")]
        [SerializeField] private UI51Toggle sfxSwitch;
        [SerializeField] private UI51Toggle musicSwitch;
        [SerializeField] private UI51Toggle vibrationSwitch;
        [SerializeField] private UI51Toggle graphicsSwitch;
        [Tooltip("Intestazione ACCOUNT e i suoi pannelli: solo dopo l'ingresso.")]
        [SerializeField] private GameObject accountSection;
        [Tooltip("Login vero: email, Esci, Elimina account.")]
        [SerializeField] private GameObject accountGroup;
        [Tooltip("Ospite: invito a registrarsi e uscita dalla sessione.")]
        [SerializeField] private GameObject guestGroup;
        [SerializeField] private TMP_Text emailLabel;
        [Tooltip("Esci (account) ed Esci dalla sessione ospite.")]
        [SerializeField] private Button[] logoutButtons;
        [SerializeField] private Button privacyButton;
        [SerializeField] private Button termsButton;
        [SerializeField] private LegalModalV2 legal;

        public const string AccountHeaderText = "ACCOUNT";
        public const string GeneralHeaderText = "GENERALE";

        private bool layoutCaptured;
        private float frameHeight, footerY, deleteY, accountStep, deleteStep;
        private float[] rowsY;
        private AnimatedModalV2 panelMotion;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            closeButton.onClick.AddListener(Close);
            dimmerButton.onClick.AddListener(Close);
            accountButton.onClick.AddListener(OpenAccount);
            audioToggle.OnChanged += GameAudioPreferences.SetEnabled;
            EnsureMusicToggle();
            if (musicToggle != null) musicToggle.OnChanged += GameAudioPreferences.SetMusicEnabled;
            EnsureVibrationToggle();
            if (vibrationToggle != null) vibrationToggle.OnChanged += GamePreferences.SetVibrationEnabled;
            EnsureReducedGraphicsToggle();
            if (reducedGraphicsToggle != null) reducedGraphicsToggle.OnChanged += GamePreferences.SetReducedGraphics;
            GamePreferences.Changed += RefreshGraphicsChoice;
            if (deleteAccountButton != null) deleteAccountButton.onClick.AddListener(OpenDeleteAccount);
            if (sfxSwitch != null) sfxSwitch.onValueChanged.AddListener(GameAudioPreferences.SetEffectsEnabled);
            if (musicSwitch != null) musicSwitch.onValueChanged.AddListener(GameAudioPreferences.SetMusicEnabled);
            if (vibrationSwitch != null) vibrationSwitch.onValueChanged.AddListener(GamePreferences.SetVibrationEnabled);
            if (graphicsSwitch != null) graphicsSwitch.onValueChanged.AddListener(GamePreferences.SetReducedGraphics);
            if (logoutButtons != null)
                foreach (var b in logoutButtons) if (b != null) b.onClick.AddListener(Logout);
            if (legal != null)
            {
                if (privacyButton != null) privacyButton.onClick.AddListener(legal.ShowPrivacy);
                if (termsButton != null) termsButton.onClick.AddListener(legal.ShowTerms);
            }
            panel.SetActive(false);
        }

        private void Start()
        {
            // Dopo un'eliminazione riuscita MainMenu viene ricaricata: la conferma compare qui.
            if (deleteAccount != null) deleteAccount.ShowDeletedIfPending();
        }

        public void Open()
        {
            var auth = AuthBootstrapper.Instance?.PlayFabAuth;
            accountName.text = auth?.GetBestDisplayName() ?? "Ospite";
            accountSubtitle.text = auth != null && auth.HasRealLogin ? "Gestisci account" : "Accedi o registrati";
            audioToggle.SetOn(GameAudioPreferences.Enabled);
            if (musicToggle != null) musicToggle.SetOn(GameAudioPreferences.MusicChoice);
            if (vibrationToggle != null) vibrationToggle.SetOn(GamePreferences.VibrationEnabled);
            if (sfxSwitch != null)
            {
                GameAudioPreferences.FoldMasterIntoChannels();
                sfxSwitch.SetIsOn(GameAudioPreferences.EffectsChoice, false, false);
            }
            if (musicSwitch != null) musicSwitch.SetIsOn(GameAudioPreferences.MusicChoice, false, false);
            if (vibrationSwitch != null) vibrationSwitch.SetIsOn(GamePreferences.VibrationEnabled, false, false);
            RefreshGraphicsChoice();

            // Prima dell'ingresso non esiste un account da mostrare (la sessione ospite tecnica di
            // PlayFab resta invisibile). Eliminare l'account ha senso solo con un login vero: un
            // ospite non ci ha dato nessun dato.
            bool entered = startScreen == null || startScreen.HasEntered;
            bool realAccount = entered && auth != null && auth.IsLoggedIn && auth.HasRealLogin;
            ApplyAccountLayout(entered, realAccount);
            if (accountSection != null) accountSection.SetActive(entered);
            if (accountGroup != null) accountGroup.SetActive(realAccount);
            if (guestGroup != null) guestGroup.SetActive(entered && !realAccount);
            if (emailLabel != null) emailLabel.text = MaskEmail(auth?.Email);
            var footerText = footer != null ? footer.GetComponent<TMP_Text>() : null;
            if (footerText != null) footerText.text = "51Cirulla · v" + Application.version;
            if (panelMotion == null)
            {
                panelMotion = panel.GetComponent<AnimatedModalV2>();
                if (panelMotion == null) panelMotion = panel.AddComponent<AnimatedModalV2>();
                panelMotion.Group = panel.GetComponent<CanvasGroup>();
                if (panelMotion.Group == null) panelMotion.Group = panel.AddComponent<CanvasGroup>();
                panelMotion.Frame = panelFrame;
                panelMotion.HandleEscape = false; // this controller gives Delete Account first refusal
            }
            panelMotion.Open();
        }

        public void Close()
        {
            if (panelMotion != null) panelMotion.Close();
            else panel.SetActive(false);
        }

        /// <summary>
        /// Nasconde le righe Account / Elimina account e ricompatta la sezione: le righe sotto salgono,
        /// la cornice si accorcia (restando centrata) e il pie' di pagina la segue.
        /// </summary>
        private void ApplyAccountLayout(bool showAccount, bool showDelete)
        {
            var accountRow = (RectTransform)accountButton.transform;
            var deleteRow = deleteAccountButton != null ? (RectTransform)deleteAccountButton.transform : null;
            accountRow.gameObject.SetActive(showAccount);
            if (deleteRow != null) deleteRow.gameObject.SetActive(showDelete);
            if (accountHeader != null) accountHeader.text = showAccount ? AccountHeaderText : GeneralHeaderText;
            if (panelFrame == null || rowsAfterAccount == null || rowsAfterAccount.Length == 0) return;

            if (!layoutCaptured)
            {
                layoutCaptured = true;
                frameHeight = panelFrame.sizeDelta.y;
                footerY = footer != null ? footer.anchoredPosition.y : 0f;
                rowsY = new float[rowsAfterAccount.Length];
                for (int i = 0; i < rowsY.Length; i++) rowsY[i] = rowsAfterAccount[i].anchoredPosition.y;
                accountStep = accountRow.anchoredPosition.y - rowsY[0];
                deleteY = deleteRow != null ? deleteRow.anchoredPosition.y : 0f;
                deleteStep = deleteRow != null ? rowsY[rowsY.Length - 1] - deleteY : 0f;
            }

            float up = showAccount ? 0f : accountStep;
            for (int i = 0; i < rowsY.Length; i++) SetY(rowsAfterAccount[i], rowsY[i] + up);
            if (deleteRow != null) SetY(deleteRow, deleteY + up);

            float shrink = up + (showDelete ? 0f : deleteStep);
            panelFrame.sizeDelta = new Vector2(panelFrame.sizeDelta.x, frameHeight - shrink);
            if (footer != null) SetY(footer, footerY + shrink);
        }

        private static void SetY(RectTransform rect, float y) => rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);

        private void RefreshGraphicsChoice()
        {
            if (reducedGraphicsToggle != null) reducedGraphicsToggle.SetOn(GamePreferences.ReducedGraphics);
            if (graphicsSwitch != null) graphicsSwitch.SetIsOn(GamePreferences.ReducedGraphics, false);
        }

        /// <summary>"giocatore@mail.com" -> "g•••••@mail.com"; vuoto se l'email non e' nota.</summary>
        public static string MaskEmail(string email)
        {
            int at = string.IsNullOrEmpty(email) ? -1 : email.IndexOf('@');
            return at < 1 ? "" : email[0] + "•••••" + email.Substring(at);
        }

        // Esci: stessa uscita del pannello account (AuthScreensV2.Logout). Si ricarica la scena perche'
        // senza ricarica la Home resta "entrata" e Indietro ci riporta dentro come ospite non scelto.
        private void Logout()
        {
            if (panelMotion != null) panelMotion.CloseImmediate();
            else panel.SetActive(false);
            var bootstrapper = AuthBootstrapper.Instance;
            if (bootstrapper != null) bootstrapper.LogoutAndRestart(clearRealAccountFlag: true);
            if (!AppLoading.LoadScene(AppFlowManager.SCENE_MAIN_MENU)) SceneManager.LoadScene(AppFlowManager.SCENE_MAIN_MENU);
        }

        public void EnsureReducedGraphicsToggle()
        {
            if (reducedGraphicsToggle == null && panelFrame != null)
                reducedGraphicsToggle = panelFrame.Find("Row_AnimazioniVeloci")?.GetComponentInChildren<SimpleToggleSwitch>(true);
            if (reducedGraphicsToggle == null) return;
            var row = reducedGraphicsToggle.transform.parent;
            row.gameObject.SetActive(true);
            row.Find("Title").GetComponent<TMP_Text>().text = "Grafica ridotta";
            row.Find("Subtitle").GetComponent<TMP_Text>().text = "Meno effetti e animazioni più brevi";
            reducedGraphicsToggle.GetComponent<Button>().interactable = true;
            var group = row.GetComponent<CanvasGroup>();
            if (group != null) { group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true; }
            RefreshGraphicsChoice();
        }

        /// <summary>Stessa scelta Musica delle Impostazioni al tavolo (GameAudioPreferences.MusicKey).</summary>
        public void EnsureMusicToggle()
        {
            if (musicToggle == null && panelFrame != null)
                musicToggle = panelFrame.Find("Row_Musica")?.GetComponentInChildren<SimpleToggleSwitch>(true);
            if (musicToggle == null) return;
            var row = musicToggle.transform.parent;
            row.gameObject.SetActive(true);
            var subtitle = row.Find("Subtitle")?.GetComponent<TMP_Text>();
            if (subtitle != null) subtitle.text = "Musica di sottofondo";
            musicToggle.GetComponent<Button>().interactable = true;
            var group = row.GetComponent<CanvasGroup>();
            if (group != null) { group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true; }
            musicToggle.SetOn(GameAudioPreferences.MusicChoice);
        }

        public void EnsureVibrationToggle()
        {
            if (vibrationToggle == null && panelFrame != null)
                vibrationToggle = panelFrame.Find("Row_Vibrazione")?.GetComponentInChildren<SimpleToggleSwitch>(true);
            if (vibrationToggle == null) return;
            vibrationToggle.transform.parent.gameObject.SetActive(true);
            var subtitle = vibrationToggle.transform.parent.Find("Subtitle")?.GetComponent<TMP_Text>();
            if (subtitle != null) subtitle.text = "Tocchi e momenti importanti";
            vibrationToggle.GetComponent<Button>().interactable = true;
            var group = vibrationToggle.transform.parent.GetComponent<CanvasGroup>();
            if (group != null) { group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true; }
            vibrationToggle.SetOn(GamePreferences.VibrationEnabled);
        }



        /// <summary>
        /// Ospite: la schermata Registrazione V2 (da li' si passa anche all'Accesso), come "Registrati
        /// per salvare" del Profilo. Con un login vero resta il pannello account di AuthUIController.
        /// </summary>
        private void OpenAccount()
        {
            if (panelMotion != null) panelMotion.CloseImmediate();
            else panel.SetActive(false);
            if (authUI == null) return;
            var auth = AuthBootstrapper.Instance?.PlayFabAuth;
            authUI.ShowAuthUI();
            if (auth == null || !auth.HasRealLogin) authUI.ShowRegisterPanel();
        }

        // Le Impostazioni restano aperte sotto: ANNULLA riporta qui.
        private void OpenDeleteAccount()
        {
            if (deleteAccount != null) deleteAccount.Begin();
        }

        private void Update()
        {
            // Indietro chiude prima la finestra di eliminazione, se aperta sopra.
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape) && (deleteAccount == null || !deleteAccount.gameObject.activeSelf)) Close();
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (dimmerButton != null) dimmerButton.onClick.RemoveListener(Close);
            if (accountButton != null) accountButton.onClick.RemoveListener(OpenAccount);
            if (deleteAccountButton != null) deleteAccountButton.onClick.RemoveListener(OpenDeleteAccount);
            if (audioToggle != null) audioToggle.OnChanged -= GameAudioPreferences.SetEnabled;
            if (musicToggle != null) musicToggle.OnChanged -= GameAudioPreferences.SetMusicEnabled;
            if (vibrationToggle != null) vibrationToggle.OnChanged -= GamePreferences.SetVibrationEnabled;
            if (reducedGraphicsToggle != null) reducedGraphicsToggle.OnChanged -= GamePreferences.SetReducedGraphics;
            GamePreferences.Changed -= RefreshGraphicsChoice;
        }
    }
}
