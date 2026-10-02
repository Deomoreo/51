using PlayFab;
using PlayFab.ClientModels;
using Project51.Core;
using Project51.UI51;
using Project51.UIV2.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 11: Password dimenticata (mockup PasswordDimenticata e PasswordInviata) sopra all'Accesso.
    /// PlayFab manda l'email di recupero col modello di AppConfig (vuoto = quello del titolo). La risposta è la stessa che
    /// l'indirizzo abbia un account o no: altrimenti chiunque scoprirebbe chi gioca qui. Reinvio dopo 30 secondi.
    /// Anche "Cambia password" delle Impostazioni (ChangePassword): PlayFab non cambia la password dentro l'app, si manda il link.
    /// Grafica: UI51AccountBuilder (Tools/UI51/Build Fase 11), con un suo canvas sopra all'Accesso e alle Impostazioni.
    /// </summary>
    public sealed class UI51RecoveryView : MonoBehaviour
    {
        [SerializeField] private Button back, send, toLogin, resend;
        [SerializeField] private TMP_InputField email;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite lockIcon, mailIcon;
        [SerializeField] private TMP_Text title, text, status, sentText, resendLabel;
        [SerializeField] private RectTransform form, sent, sentPanel;

        const float ResendSeconds = 30f;

        private bool sending, ownAccount;
        private string sentTo = string.Empty;
        private float resendAt;

        private void Awake()
        {
            back.onClick.AddListener(Close);
            toLogin.onClick.AddListener(Close);
            send.onClick.AddListener(() => Send(email.text));
            resend.onClick.AddListener(() => { if (Time.unscaledTime >= resendAt) Send(sentTo); });
        }

        /// <summary>Apre il modulo con l'email già scritta nell'Accesso, se è un'email.</summary>
        public void Open(string prefill)
        {
            ownAccount = false;
            gameObject.SetActive(true);
            email.text = AuthScreensV2.LooksLikeEmail(prefill) ? prefill : string.Empty;
            ShowSent(false);
            UIAnim.FadeIn((RectTransform)transform, 0.2f);
        }

        /// <summary>"Cambia password" (scelta dell'utente 01/10): il link parte subito verso l'email dell'account.</summary>
        public void ChangePassword(string accountEmail)
        {
            Open(accountEmail);
            ownAccount = true;
            if (AuthScreensV2.LooksLikeEmail(accountEmail)) Send(accountEmail);
        }

        public void Close() => gameObject.SetActive(false);

        private void Update()
        {
            if (!sent.gameObject.activeSelf) return;
            float wait = resendAt - Time.unscaledTime;
            resendLabel.text = wait > 0f ? $"Puoi reinviarla tra {Mathf.CeilToInt(wait)}s" : "Non è arrivata? Reinvia";
            resendLabel.color = wait > 0f ? UI51Tokens.CreamA(0.4f) : UI51Tokens.Gold;
        }

        private void ShowSent(bool on)
        {
            form.gameObject.SetActive(!on);
            sent.gameObject.SetActive(on);
            icon.sprite = on ? mailIcon : lockIcon;
            title.text = on ? "Controlla la posta" : "Password dimenticata?";
            text.text = on
                ? "Ti abbiamo mandato un link per scegliere una nuova password. Guarda anche nello spam."
                : "Scrivi l’email del tuo account: ti mandiamo un link per scegliere una nuova password.";
            status.text = string.Empty;
            if (on)
            {
                // Stessa frase neutra anche per Cambia password (scelta dell'utente 01/10): non dice mai se l'email e' registrata.
                sentText.text = $"Se <b>{SettingsV2Integration.MaskEmail(sentTo)}</b> è l’email di un account 51, il link è in arrivo.";
                var back = toLogin.GetComponentInChildren<TMP_Text>(true);
                if (back != null) back.text = ownAccount ? "CHIUDI" : "TORNA AL LOGIN";
                UIAnim.PopDialog(sentPanel);
            }
        }

        private void Send(string address)
        {
            if (sending) return;
            address = (address ?? string.Empty).Trim();
            if (!AuthScreensV2.LooksLikeEmail(address))
            {
                SetStatus("Scrivi l’email del tuo account.", true);
                return;
            }

            sending = true;
            SetStatus("Invio in corso...", false);
            var request = new SendAccountRecoveryEmailRequest { Email = address, TitleId = PlayFabSettings.TitleId };
            string template = AppConfig.RecoveryEmailTemplateId;
            if (!string.IsNullOrEmpty(template)) request.EmailTemplateId = template;

            PlayFabClientAPI.SendAccountRecoveryEmail(request, _ => Sent(address), error =>
            {
                Debug.LogWarning("[UI51RecoveryView] Recupero password: " + (error != null ? error.GenerateErrorReport() : "errore sconosciuto"));
                // Indirizzo sconosciuto o malformato: stessa risposta del caso riuscito. Gli altri errori non vanno a schermo così come sono.
                if (error != null && (error.Error == PlayFabErrorCode.AccountNotFound || error.Error == PlayFabErrorCode.InvalidEmailAddress))
                    Sent(address);
                else
                {
                    sending = false;
                    SetStatus("Non è stato possibile completare la richiesta. Riprova tra poco.", true);
                }
            });
        }

        private void Sent(string address)
        {
            sending = false;
            sentTo = address;
            resendAt = Time.unscaledTime + ResendSeconds;
            if (this != null && gameObject.activeInHierarchy) ShowSent(true);
        }

        private void SetStatus(string message, bool error)
        {
            status.text = message;
            status.color = error ? UI51Tokens.DangerText : UI51Tokens.CreamA(0.7f);
        }
    }
}
