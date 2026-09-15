using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Apre/chiude ModalitaPanelOverlay (con animazione fade+scale, invece dello scatto
    /// secco di un SetActive nudo), tiene traccia di quale modalita' e' selezionata e
    /// riflette la scelta sul ValueText del ModeSelector in Home.
    ///
    /// Vive su un GameObject SEMPRE ATTIVO (sibling dell'overlay, non dentro di esso): se
    /// stesse dentro l'overlay (disattivato di default), il suo Awake() non girerebbe mai
    /// finche' qualcun altro non lo attiva gia', e i listener sui bottoni (incluso quello
    /// su Home che deve APRIRE il pannello) non verrebbero mai agganciati. Per questo il
    /// wiring vive qui fuori.
    /// </summary>
    public class PanelModalitaController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton;
        [SerializeField] private Button playButton;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private SelectableToggleGroup modeRowGroup;

        [Header("Animazione apertura/chiusura")]
        [SerializeField] private float animDuration = 0.22f;
        [SerializeField] private Ease openEase = Ease.OutQuad;
        [SerializeField] private Ease closeEase = Ease.InQuad;
        // Round precedente: scale 0.85->1 con OutBack (overshoot) sul PanelFrame faceva
        // "pulsare" troppo il bordo dorato. Passato a OutQuad/InQuad (niente overshoot) e
        // scala piu' contenuta (0.94), ma il bordo dorato restava comunque troppo in vista
        // durante il pop. Tolto lo scale del tutto: ora e' un semplice fade, il modo piu'
        // "calmo" possibile di far comparire/sparire un dialog senza tornare a uno scatto
        // secco. Il campo resta per poterlo eventualmente riattivare in futuro.
        [SerializeField] private bool animateScale = false;
        [SerializeField] private float closedScale = 0.97f;

        [Header("Modalita' selezionata")]
        [Tooltip("Etichette delle 6 righe modalita' selezionabili, stesso ordine con cui sono state create (Row_1v1, Row_2v2, Row_1v3, Row_1v1Bot, Row_2v2Bot, Row_1v3Bot).")]
        [SerializeField] private string[] modeLabels;
        [SerializeField] private int selectedModeIndex;
        [SerializeField] private TMP_Text modeSelectorValueText;

        private Sequence _sequence;

        public int SelectedModeIndex => selectedModeIndex;
        public string SelectedModeLabel =>
            (modeLabels != null && selectedModeIndex >= 0 && selectedModeIndex < modeLabels.Length)
                ? modeLabels[selectedModeIndex]
                : null;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            if (openButton != null)
            {
                openButton.onClick.AddListener(Open);
            }

            if (playButton != null)
            {
                playButton.onClick.AddListener(PlaySelectedMode);
            }

            // Delegate diretto (vedi SelectableToggleGroup.onSelectedRuntime): niente
            // wiring Editor-side qui, e' una semplice assegnazione a runtime.
            if (modeRowGroup != null)
            {
                modeRowGroup.onSelectedRuntime = OnModeSelected;
            }
        }

        public void Open()
        {
            if (panelRoot == null) return;

            panelRoot.SetActive(true);

            // Sempre in cima quando si riapre: senza questo lo ScrollRect restava dov'era
            // stato lasciato l'ultima volta invece di ripartire dall'inizio del contenuto.
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }

            _sequence?.Kill();
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (panelFrame != null) panelFrame.localScale = Vector3.one * (animateScale ? closedScale : 1f);

            _sequence = DOTween.Sequence();
            if (canvasGroup != null) _sequence.Join(canvasGroup.DOFade(1f, animDuration));
            if (animateScale && panelFrame != null) _sequence.Join(panelFrame.DOScale(1f, animDuration).SetEase(openEase));
            _sequence.SetUpdate(true);
        }

        public void Close()
        {
            if (panelRoot == null) return;

            _sequence?.Kill();

            _sequence = DOTween.Sequence();
            if (canvasGroup != null) _sequence.Join(canvasGroup.DOFade(0f, animDuration));
            if (animateScale && panelFrame != null) _sequence.Join(panelFrame.DOScale(closedScale, animDuration).SetEase(closeEase));
            _sequence.SetUpdate(true).OnComplete(() => panelRoot.SetActive(false));
        }

        /// <summary>
        /// Agganciato a modeRowGroup.onSelectedRuntime in Awake().
        /// </summary>
        public void OnModeSelected(int index)
        {
            selectedModeIndex = index;

            if (modeSelectorValueText != null && modeLabels != null && index >= 0 && index < modeLabels.Length)
            {
                modeSelectorValueText.text = modeLabels[index];
            }
        }

        /// <summary>
        /// Agganciato al bottone GIOCA di Home. Per ora e' uno stub (solo log): avviare
        /// davvero una partita richiede capire come MatchmakingManager/NetworkGameController
        /// si aspettano di essere invocati, cosa non ancora esplorata in questa sessione -
        /// task separato, da non improvvisare alla cieca su codice di rete gia' esistente.
        /// </summary>
        public void PlaySelectedMode()
        {
            Debug.Log("[PanelModalitaController] Avvio modalita' (stub, non ancora collegato al matchmaking): " + SelectedModeLabel);
        }
    }
}
