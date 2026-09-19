using Project51.Auth;
using Project51.Core;
using Project51.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    public sealed class SettingsV2Integration : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dimmerButton;
        [SerializeField] private Button accountButton;
        [SerializeField] private SimpleToggleSwitch audioToggle;
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

        public const string AccountHeaderText = "ACCOUNT";
        public const string GeneralHeaderText = "GENERALE";

        private bool layoutCaptured;
        private float frameHeight, footerY, deleteY, accountStep, deleteStep;
        private float[] rowsY;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            closeButton.onClick.AddListener(Close);
            dimmerButton.onClick.AddListener(Close);
            accountButton.onClick.AddListener(OpenAccount);
            audioToggle.OnChanged += GameAudioPreferences.SetEnabled;
            if (deleteAccountButton != null) deleteAccountButton.onClick.AddListener(OpenDeleteAccount);
            Close();
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

            // Prima dell'ingresso non esiste un account da mostrare (la sessione ospite tecnica di
            // PlayFab resta invisibile). Eliminare l'account ha senso solo con un login vero: un
            // ospite non ci ha dato nessun dato.
            bool entered = startScreen == null || startScreen.HasEntered;
            bool realAccount = entered && auth != null && auth.IsLoggedIn && auth.HasRealLogin;
            ApplyAccountLayout(entered, realAccount);
            var footerText = footer != null ? footer.GetComponent<TMP_Text>() : null;
            if (footerText != null) footerText.text = "51Cirulla · v" + Application.version;
            panel.SetActive(true);
        }

        public void Close() => panel.SetActive(false);

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

        /// <summary>
        /// Ospite: la schermata Registrazione V2 (da li' si passa anche all'Accesso), come "Registrati
        /// per salvare" del Profilo. Con un login vero resta il pannello account di AuthUIController.
        /// </summary>
        private void OpenAccount()
        {
            Close();
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
        }
    }
}
