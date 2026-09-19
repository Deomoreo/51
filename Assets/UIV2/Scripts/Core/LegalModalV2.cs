using Project51.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Finestra di lettura dei documenti legali (Termini di servizio e Privacy Policy). Una sola
    /// finestra per tutti i documenti: cambia il titolo e il testo dentro. Si appoggia ad
    /// AnimatedModalV2 per apertura, chiusura e velo, come gli altri pannelli V2.
    ///
    /// Sta su un Canvas suo con ordine alto perche' deve poter comparire anche sopra alla schermata
    /// di accesso, che mentre e' visibile si porta a 2000.
    /// Grafica costruita da Tools/UIV2/Build Auth Screens, insieme alle schermate che la aprono.
    /// </summary>
    public sealed class LegalModalV2 : MonoBehaviour
    {
        public AnimatedModalV2 Modal;
        public TMP_Text Title;
        public TMP_Text Body;
        public ScrollRect Scroll;
        [Tooltip("Apre la versione pubblica sul web, se l'indirizzo e' configurato in AppConfig.")]
        public Button OpenOnWeb;

        [Header("Documenti (Assets/Legal)")]
        public TextAsset Terms;
        public TextAsset Privacy;

        private string currentUrl;

        private void Awake()
        {
            if (OpenOnWeb != null) OpenOnWeb.onClick.AddListener(OpenCurrentUrl);
        }

        public void ShowTerms() => Show("TERMINI DI SERVIZIO", Terms, AppConfig.Terms);

        public void ShowPrivacy() => Show("PRIVACY POLICY", Privacy, AppConfig.Privacy);

        private void Show(string title, TextAsset document, string url)
        {
            if (Modal == null) return;

            if (Title != null) Title.text = title;
            if (Body != null) Body.text = document != null
                ? LegalDocuments.Format(document)
                : "Testo non disponibile in questa versione dell'app.";

            currentUrl = url;
            // Il pulsante "Apri sul sito" compare solo se c'e' davvero un indirizzo: un pulsante che
            // non porta da nessuna parte e' peggio che assente.
            if (OpenOnWeb != null) OpenOnWeb.gameObject.SetActive(!string.IsNullOrEmpty(url));

            if (UIV2ModalHost.Instance != null) UIV2ModalHost.Instance.Open(Modal);
            else Modal.Open();

            // Lo scroll deve partire dall'alto: senza questo si riapre dove l'avevi lasciato.
            if (Scroll != null)
            {
                Canvas.ForceUpdateCanvases();
                Scroll.verticalNormalizedPosition = 1f;
            }
        }

        private void OpenCurrentUrl()
        {
            if (!string.IsNullOrEmpty(currentUrl)) Application.OpenURL(currentUrl);
        }
    }
}
