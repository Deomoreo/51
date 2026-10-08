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
    /// quando sono aperte dalla schermata iniziale (1500). Grafica: Tools/UI51/Build Fase 4 (Collezione, Profilo, Impostazioni).
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

        [Header("UI51 (opzionali): una sola conferma, scrivendo ELIMINA")]
        public TMP_InputField ConfirmInput;
        [Tooltip("Etichetta + campo: visibili solo nella domanda.")]
        public GameObject ConfirmGroup;
        [Tooltip("Fondo del pulsante Elimina: si attenua finche' la parola non e' scritta.")]
        public Graphic DangerFill;

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
        public const string ConfirmWord = "ELIMINA";
        public const string TypedBody = "Perderai per sempre livello, esperienza, collezione, trofei e statistiche. L'operazione non si può annullare.";

        /// <summary>Vero se il testo scritto e' la parola di conferma (spazi e maiuscole non contano).</summary>
        public static bool IsConfirmWord(string typed) =>
            string.Equals((typed ?? "").Trim(), ConfirmWord, System.StringComparison.OrdinalIgnoreCase);

        private void Awake()
        {
            Cancel.onClick.AddListener(Close);
            Danger.onClick.AddListener(Advance);
            Ok.onClick.AddListener(Acknowledge);
            if (ConfirmInput != null) ConfirmInput.onValueChanged.AddListener(_ => RefreshDanger());
            // Qui sale la finestra intera (anche i pulsanti, LateUpdate), non solo il campo.
            var style = ConfirmInput != null ? ConfirmInput.GetComponent<Project51.UI51.UI51Input>() : null;
            if (style != null) style.LiftPanel = false;
        }

        /// <summary>Primo passo: dalla voce "Elimina account" delle Impostazioni.</summary>
        public void Begin()
        {
            if (AccountDeletionService.IsBusy) { Show(Step.Busy, ConfirmTitle, BusyBody); return; }
            if (ConfirmInput != null)
            {
                // La parola scritta vale da seconda conferma: si parte gia' dall'ultimo passo.
                ConfirmInput.text = "";
                Show(Step.Confirm, AskTitle, TypedBody);
                return;
            }
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
                if (ConfirmInput != null && !IsConfirmWord(ConfirmInput.text)) return;
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
            if (DangerLabel != null) DangerLabel.text = ConfirmInput != null ? "Elimina" : next == Step.Ask ? "CONTINUA" : "ELIMINA ACCOUNT";
            if (ConfirmGroup != null) ConfirmGroup.SetActive(next == Step.Confirm);
            RefreshDanger();
            // Durante l'attesa la finestra non si chiude: l'esito deve arrivare a chi l'ha chiesto.
            if (Modal.CloseButton != null) Modal.CloseButton.interactable = !busy;
            if (Modal.Dimmer != null) Modal.Dimmer.interactable = !busy;

            if (!Modal.IsOpen)
            {
                if (UIV2ModalHost.Instance != null) UIV2ModalHost.Instance.Open(Modal);
                else Modal.Open();
            }
        }

        // Con il campo di conferma, Elimina si accende solo quando la parola e' scritta.
        private void RefreshDanger()
        {
            if (ConfirmInput == null) return;
            bool ready = step != Step.Confirm || IsConfirmWord(ConfirmInput.text);
            Danger.interactable = ready && step != Step.Busy;
            if (DangerFill != null) DangerFill.color = new Color(1f, 1f, 1f, ready ? 1f : 0.25f);
            if (DangerLabel != null) DangerLabel.alpha = ready ? 1f : 0.45f;
        }

        private void Close()
        {
            if (step == Step.Busy) return;
            Modal.Close();
        }

        private static readonly Vector3[] corners = new Vector3[4];
        private float lift;

        // Tastiera del telefono: la finestra sale quanto basta e torna giu' quando la tastiera si chiude.
        // Nell'Editor la tastiera non esiste (altezza 0): nessun effetto. Altezza come i campi (UI51Input.KeyboardHeight, anche Android).
        private void LateUpdate()
        {
            if (ConfirmInput == null || Modal == null || Modal.Frame == null || Modal.Group == null) return;
            if (Modal.Group.alpha < 1f) return; // apertura o chiusura in corso: la posizione e' di AnimatedModalV2
            var frame = Modal.Frame;
            float target = 0f;
            float keyboard = Project51.UI51.UI51Input.KeyboardHeight(ConfirmInput.isFocused);
            if (keyboard > 0f && frame.rect.height > 0f)
            {
                var canvas = frame.GetComponentInParent<Canvas>();
                var cam = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                frame.GetWorldCorners(corners);
                float bottom = RectTransformUtility.WorldToScreenPoint(cam, corners[0]).y;
                float top = RectTransformUtility.WorldToScreenPoint(cam, corners[1]).y;
                float unit = (top - bottom) / frame.rect.height;
                target = Project51.UI51.UI51Input.KeyboardLift(keyboard, bottom - lift * unit, top - lift * unit, UnityEngine.Screen.safeArea.yMax, unit);
            }
            if (Mathf.Abs(target - lift) < 0.5f) return;
            // Toccare un pulsante chiude la tastiera: se la finestra scendesse sotto il dito, il tocco andrebbe perso.
            if (target < lift && Input.touchCount > 0) return;
            frame.anchoredPosition += Vector2.up * (target - lift);
            lift = target;
        }

        // AnimatedModalV2 rimette la finestra al suo posto quando si spegne: qui si azzera solo il conto.
        private void OnDisable() => lift = 0f;
    }
}
