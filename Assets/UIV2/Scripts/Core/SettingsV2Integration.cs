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
        [SerializeField] private Button accountButton;
        [SerializeField] private AuthUIController authUI;
        [Header("Account -> Elimina account (H7)")]
        [SerializeField] private Button deleteAccountButton;
        [SerializeField] private DeleteAccountModalV2 deleteAccount;

        [Header("Sezione Account: solo dopo l'ingresso (ospite o login)")]
        [Tooltip("Finche' non si e' entrati (HasEntered falso) la riga Account non compare.")]
        [SerializeField] private StartScreenV2 startScreen;
        [Tooltip("La pagina UI51 (Safe/Page): la cornice che AnimatedModalV2 anima.")]
        [SerializeField] private RectTransform panelFrame;

        [Header("UI51")]
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
        [Tooltip("Riga \"Regole e tutorial\" (UI51 Fase 12): apre la pagina Regole sopra alle Impostazioni.")]
        [SerializeField] private Button rulesButton;
        [SerializeField] private Project51.Unity.UI.UI51RulesView rules;
        [Tooltip("Riga \"Cambia password\" (UI51 Fase 11): manda il link all'email dell'account e mostra \"Controlla la posta\".")]
        [SerializeField] private Button passwordButton;
        [SerializeField] private Project51.Unity.UI.UI51RecoveryView recovery;
        [Tooltip("Sezione PRIVACY E SOCIALE (2.56), solo con un account: riga Giocatori bloccati che apre la pagina.")]
        [SerializeField] private GameObject socialSection;
        [SerializeField] private Button blockedButton;
        [SerializeField] private Project51.Unity.UI.UI51BlockedView blocked;
        [SerializeField] private ScrollRect scroll;

        private AnimatedModalV2 panelMotion;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            closeButton.onClick.AddListener(Close);
            accountButton.onClick.AddListener(OpenAccount);
            GamePreferences.Changed += RefreshGraphicsChoice;
            if (deleteAccountButton != null) deleteAccountButton.onClick.AddListener(OpenDeleteAccount);
            if (sfxSwitch != null) sfxSwitch.onValueChanged.AddListener(GameAudioPreferences.SetEffectsEnabled);
            if (musicSwitch != null) musicSwitch.onValueChanged.AddListener(GameAudioPreferences.SetMusicEnabled);
            if (vibrationSwitch != null) vibrationSwitch.onValueChanged.AddListener(GamePreferences.SetVibrationEnabled);
            if (graphicsSwitch != null) graphicsSwitch.onValueChanged.AddListener(GamePreferences.SetReducedGraphics);
            if (logoutButtons != null)
                foreach (var b in logoutButtons) if (b != null) b.onClick.AddListener(Logout);
            if (rulesButton != null && rules != null) rulesButton.onClick.AddListener(() => rules.Open());
            if (blockedButton != null && blocked != null) blockedButton.onClick.AddListener(blocked.Open);
            if (passwordButton != null && recovery != null)
                passwordButton.onClick.AddListener(() => recovery.ChangePassword(AuthBootstrapper.Instance?.PlayFabAuth?.Email));
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
            if (socialSection != null) socialSection.SetActive(realAccount); // gli ospiti non bloccano (non hanno dove salvarlo)
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            if (emailLabel != null) emailLabel.text = MaskEmail(auth?.Email);
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

        /// <summary>Mostra o nasconde le righe Account / Elimina account.</summary>
        private void ApplyAccountLayout(bool showAccount, bool showDelete)
        {
            accountButton.gameObject.SetActive(showAccount);
            if (deleteAccountButton != null) deleteAccountButton.gameObject.SetActive(showDelete);
        }

        private void RefreshGraphicsChoice()
        {
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
            if (accountButton != null) accountButton.onClick.RemoveListener(OpenAccount);
            if (deleteAccountButton != null) deleteAccountButton.onClick.RemoveListener(OpenDeleteAccount);
            GamePreferences.Changed -= RefreshGraphicsChoice;
        }
    }
}
