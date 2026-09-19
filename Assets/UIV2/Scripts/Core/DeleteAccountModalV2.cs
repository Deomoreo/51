using Project51.Auth;
using Project51.Core;
using Project51.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Impostazioni -> Account -> Elimina account (H7): doppia conferma, attesa ed esito in una sola
    /// finestra, basata su AnimatedModalV2 e aperta tramite UIV2ModalHost come gli altri modal V2.
    /// Nessuna logica di backend qui: il pulsante finale chiama AccountDeletionService, che risponde
    /// solo con un esito. Gli errori a schermo sono testi fissi, mai messaggi grezzi del server.
    ///
    /// Canvas proprio con ordine 2200 (come LegalModalV2) per stare sopra alle Impostazioni, anche
    /// quando sono aperte dalla schermata iniziale (1500). Grafica: Tools/UIV2/Build Delete Account.
    /// </summary>
    public sealed class DeleteAccountModalV2 : MonoBehaviour
    {
        public AnimatedModalV2 Modal;
        public TMP_Text Title;
        public TMP_Text Body;
        public Button Cancel;
        public Button Danger;
        public TMP_Text DangerLabel;
        public Button Ok;
        [Tooltip("Avviso per domande ed errori, spunta per l'eliminazione riuscita.")]
        public Image Icon;
        public Sprite WarnIcon;
        public Sprite DoneIcon;

        private enum Step { Ask, Confirm, Busy, Message }
        private Step step;

        public const string AskTitle = "Eliminare l'account?";
        public const string AskBody = "L'eliminazione è permanente. Verranno rimossi l'account e i dati associati che non devono essere conservati per obblighi di legge.";
        public const string ConfirmTitle = "Conferma eliminazione";
        public const string ConfirmBody = "Questa azione non si può annullare: perderai per sempre progressi, statistiche e collezione.";
        public const string BusyBody = "Eliminazione in corso…";
        public const string NotAvailableBody = "La cancellazione dell'account non è ancora disponibile in questa build.";
        public const string NotSignedInBody = "Non risulti collegato al tuo account. Controlla la connessione, accedi di nuovo e riprova.";
        public const string FailedBody = "Non è stato possibile completare l'eliminazione. Controlla la connessione e riprova tra poco.";
        public const string DeletedBody = "Il tuo account è stato eliminato.";

        private void Awake()
        {
            Cancel.onClick.AddListener(Close);
            Danger.onClick.AddListener(Advance);
            Ok.onClick.AddListener(Acknowledge);
        }

        /// <summary>Primo passo: dalla voce "Elimina account" delle Impostazioni.</summary>
        public void Begin()
        {
            if (AccountDeletionService.IsBusy) { Show(Step.Busy, ConfirmTitle, BusyBody); return; }
            Show(Step.Ask, AskTitle, AskBody);
        }

        /// <summary>Conferma dopo il ricaricamento della schermata iniziale, se l'account e' stato eliminato.</summary>
        public void ShowDeletedIfPending()
        {
            if (AccountDeletionService.ConsumeDeletedNotice()) Show(Step.Message, "Account eliminato", DeletedBody, done: true);
        }

        private void Advance()
        {
            if (step == Step.Ask)
            {
                Show(Step.Confirm, ConfirmTitle, ConfirmBody);
            }
            else if (step == Step.Confirm)
            {
                Show(Step.Busy, ConfirmTitle, BusyBody);
                AccountDeletionService.RequestDeletion(OnResult);
            }
        }

        private void OnResult(AccountDeletionOutcome outcome)
        {
            if (this == null) return; // scena cambiata mentre si aspettava il backend
            switch (outcome)
            {
                case AccountDeletionOutcome.Deleted:
                    // Logout e pulizia sono gia' fatti: si torna alla schermata iniziale, che mostra la
                    // conferma (ShowDeletedIfPending). Nessuno stato del vecchio account resta in Home.
                    ReloadStartScreen();
                    return;
                case AccountDeletionOutcome.NotAvailable:
                    Show(Step.Message, "Non disponibile", NotAvailableBody);
                    break;
                case AccountDeletionOutcome.NotSignedIn:
                    Show(Step.Message, "Accesso richiesto", NotSignedInBody);
                    break;
                default:
                    Show(Step.Message, "Qualcosa è andato storto", FailedBody);
                    break;
            }
        }

        private void Acknowledge() => Close();

        private static void ReloadStartScreen()
        {
            if (!AppLoading.LoadScene(AppFlowManager.SCENE_MAIN_MENU)) SceneManager.LoadScene(AppFlowManager.SCENE_MAIN_MENU);
        }

        private void Show(Step next, string title, string body, bool done = false)
        {
            step = next;
            Title.text = title;
            Body.text = body;
            if (Icon != null && WarnIcon != null && DoneIcon != null) Icon.sprite = done ? DoneIcon : WarnIcon;

            bool asking = next == Step.Ask || next == Step.Confirm;
            bool busy = next == Step.Busy;
            Cancel.gameObject.SetActive(asking || busy);
            Danger.gameObject.SetActive(asking || busy);
            Ok.gameObject.SetActive(next == Step.Message);
            Cancel.interactable = Danger.interactable = !busy;
            if (DangerLabel != null) DangerLabel.text = next == Step.Ask ? "CONTINUA" : "ELIMINA ACCOUNT";
            // Durante l'attesa la finestra non si chiude: l'esito deve arrivare a chi l'ha chiesto.
            if (Modal.CloseButton != null) Modal.CloseButton.interactable = !busy;
            if (Modal.Dimmer != null) Modal.Dimmer.interactable = !busy;

            if (!Modal.IsOpen)
            {
                if (UIV2ModalHost.Instance != null) UIV2ModalHost.Instance.Open(Modal);
                else Modal.Open();
            }
        }

        private void Close()
        {
            if (step == Step.Busy) return;
            Modal.Close();
        }
    }
}
