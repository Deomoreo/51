using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project51.Unity;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Pulsanti fissi Emoji/Accuso in basso a destra, sopra la mano locale
    /// (Assets/UI_SPEC_Tavolo.md, sezione 9). L'Emoji non e' ancora collegato a un'azione
    /// reale (il pannello di selezione non esiste, sezione 10). Accuso dichiara l'accuso
    /// manuale del giocatore locale durante la finestra aperta da TurnController
    /// (TryDeclareLocalManualAccuso) e mostra il conto alla rovescia della finestra.
    /// </summary>
    public class TableActionButtonsController : MonoBehaviour
    {
        [SerializeField] private Button emojiButton;
        [SerializeField] private Button accusoButton;
        [SerializeField] private TMP_Text accusoCountdownText;

        private TurnController turnController;

        private void Awake()
        {
            if (emojiButton != null) emojiButton.onClick.AddListener(OnEmojiClicked);
            if (accusoButton != null) accusoButton.onClick.AddListener(OnAccusoClicked);
        }

        private void Update()
        {
            if (accusoCountdownText == null) return;

            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
            }

            bool windowOpen = turnController != null && turnController.IsAccusoWindowOpen;
            accusoCountdownText.gameObject.SetActive(windowOpen);
            if (windowOpen)
            {
                accusoCountdownText.text = Mathf.CeilToInt(turnController.AccusoWindowSecondsRemaining).ToString();
            }
        }

        private void OnEmojiClicked()
        {
            Debug.Log("[TableActionButtonsController] Emoji: pannello di selezione non ancora implementato.");
        }

        private void OnAccusoClicked()
        {
            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
            }

            bool declared = turnController != null && turnController.TryDeclareLocalManualAccuso();
            Debug.Log(declared
                ? "[TableActionButtonsController] Accuso dichiarato manualmente."
                : "[TableActionButtonsController] Accuso: nessuna dichiarazione valida (finestra chiusa o mano senza accuso).");
        }
    }
}
