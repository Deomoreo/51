using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project51.Unity;
using Project51.UIV2.Components;
using Project51.Core;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Pulsanti fissi Emoji/Accuso in basso a destra, sopra la mano locale
    /// (Assets/UI_SPEC_Tavolo.md, sezione 9). Emoji apre il pannello emoticon. Accuso dichiara
    /// l'accuso manuale del giocatore locale durante la finestra aperta da TurnController
    /// (TryDeclareLocalManualAccuso). Durante la finestra il pulsante pulsa con il bagliore, un anello
    /// si svuota con il tempo rimasto e sopra la mano compare l'avviso: sempre, anche senza accuso in
    /// mano, per non rivelare nulla a chi guarda. Grafica: UI51TableBuilder (S6).
    /// </summary>
    public class TableActionButtonsController : MonoBehaviour
    {
        [SerializeField] private Button emojiButton;
        [SerializeField] private Button accusoButton;
        [SerializeField] private TMP_Text accusoCountdownText;

        [Header("Finestra accuso")]
        [SerializeField] private GameObject accusoCountdownBadge;
        [SerializeField] private RingArcGraphic accusoRing;
        [SerializeField] private CanvasGroup accusoPrompt;
        [SerializeField] private float pulsesPerSecond = 1.6f;

        [Header("UI51 (mockup Partita)")]
        [Tooltip("Anello d'oro che pulsa dietro al medaglione mentre la finestra e' aperta. Collegato = niente pulsazione di scala.")]
        [SerializeField] private Project51.UI51.UI51Shape accusoPulse;
        [Tooltip("Riflesso che attraversa il medaglione mentre la finestra e' aperta.")]
        [SerializeField] private RectTransform accusoShine;

        private TurnController turnController;
        private bool wasWindowOpen;
        private bool pressedThisWindow;
        private float shakeUntil;

        private void Awake()
        {
            if (emojiButton != null) emojiButton.onClick.AddListener(OnEmojiClicked);
            if (accusoButton != null) accusoButton.onClick.AddListener(OnAccusoClicked);
            SetWindowVisuals(false);
        }

        private void Update()
        {
            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
            }

            bool windowOpen = turnController != null && turnController.IsAccusoWindowOpen;
            if (windowOpen != wasWindowOpen)
            {
                wasWindowOpen = windowOpen;
                pressedThisWindow = false;
                SetWindowVisuals(windowOpen);
            }

            if (!windowOpen) return;

            float remaining = turnController.AccusoWindowSecondsRemaining;
            float total = Mathf.Max(0.01f, turnController.ManualAccusoWindowSeconds);
            if (accusoCountdownText != null) accusoCountdownText.text = Mathf.CeilToInt(remaining).ToString();
            if (accusoRing != null)
            {
                accusoRing.Arc = Mathf.Clamp01(remaining / total);
                accusoRing.SetVerticesDirty();
            }

            bool reduced = GamePreferences.ReducedGraphics;
            float wave = reduced ? 0f : (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * pulsesPerSecond) + 1f) * 0.5f;
            if (accusoButton != null && !pressedThisWindow)
            {
                float shake = !reduced && Time.unscaledTime < shakeUntil ? Mathf.Sin(Time.unscaledTime * 60f) * 6f : 0f;
                // Col medaglione UI51 pulsa l'anello (SetCalling): la scala resta a UI51Press.
                if (accusoPulse == null) accusoButton.transform.localScale = Vector3.one * (1f + 0.1f * wave);
                accusoButton.transform.localRotation = Quaternion.Euler(0f, 0f, shake);
            }
            if (accusoPrompt != null)
            {
                accusoPrompt.alpha = Mathf.MoveTowards(accusoPrompt.alpha, pressedThisWindow ? 0f : 1f, Time.unscaledDeltaTime * 4f);
            }
        }

        private void SetWindowVisuals(bool open)
        {
            if (accusoCountdownBadge != null) accusoCountdownBadge.SetActive(open);
            else if (accusoCountdownText != null) accusoCountdownText.gameObject.SetActive(open);
            if (accusoRing != null) accusoRing.gameObject.SetActive(open);
            if (accusoPrompt != null)
            {
                accusoPrompt.gameObject.SetActive(open);
                accusoPrompt.alpha = 0f;
            }
            if (accusoButton != null)
            {
                accusoButton.transform.localScale = Vector3.one;
                accusoButton.transform.localRotation = Quaternion.identity;
            }
            SetCalling(open);
        }

        /// <summary>Medaglione UI51 che chiama: anello che pulsa (1.6 s, 10 px, .6) e riflesso, solo senza grafica ridotta.</summary>
        private void SetCalling(bool on)
        {
            if (accusoPulse != null)
            {
                if (on) Project51.UI51.UIAnim.Pulse(accusoPulse, 10f, 1.6f, 0.6f);
                else Project51.UI51.UIAnim.Stop(accusoPulse);
            }
            if (accusoShine != null)
            {
                accusoShine.gameObject.SetActive(on && Project51.UI51.UIAnim.DecorativeLoops);
                if (on) Project51.UI51.UIAnim.Shine(accusoShine);
            }
        }

        private void OnEmojiClicked()
        {
            Project51.Core.GamePresentation.RequestEmoticons();
        }

        private void OnAccusoClicked()
        {
            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
            }

            if (turnController == null || !turnController.IsAccusoWindowOpen || pressedThisWindow) return;

            bool declared = turnController.TryDeclareLocalManualAccuso();
            // Gia' contato (fallback del Master arrivato prima del tocco, finestra riaperta da un rientro):
            // come un accuso riuscito, niente scossa d'errore.
            var state = turnController.GameState;
            if (!declared && state != null) declared = state.Players[GameModeService.Current.LocalPlayerIndex].AccusiPoints > 0;
            if (declared)
            {
                // Dichiarato: il pugno parte da GameSocialV2, il pulsante smette di chiamare.
                pressedThisWindow = true;
                accusoButton.transform.localScale = Vector3.one;
                accusoButton.transform.localRotation = Quaternion.identity;
                SetCalling(false);
            }
            else
            {
                // Niente accuso in mano: breve scossa del pulsante.
                shakeUntil = Time.unscaledTime + 0.35f;
                GameAudio.PlayUi(SoundId.UiError);
            }
        }
    }
}
