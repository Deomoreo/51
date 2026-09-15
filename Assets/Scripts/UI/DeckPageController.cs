using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Controller della pagina CARTE (DeckPage, navigata via bottom nav bar/swipe - non un
    /// pannello modale). Gestisce i 3 tab interni (Mazzi/Emoticon/Accusi) e la selezione
    /// dentro ciascuno. Niente Open/Close/CanvasGroup: l'attivazione della pagina la fa gia'
    /// PanelSwipeController/SwipeSectionNavigator, questo script vive dentro DeckPage e il
    /// suo Awake() gira la prima volta che la pagina si attiva.
    ///
    /// La selezione qui e' LIVE (nessun pulsante "conferma"): scegliere un mazzo aggiorna
    /// subito anche il ValueText del DeckSelector in Home, a differenza della scorciatoia
    /// rapida in Home (PanelMazzoController) che resta un pannello con conferma esplicita.
    /// </summary>
    public class DeckPageController : MonoBehaviour
    {
        /// <summary>
        /// Riferimenti di uno slot "equipaggiato" nella riga Emoticon (costruiti dal builder,
        /// passati al controller tramite SerializedObject - non serializzato di per se',
        /// solo un contenitore di appoggio usato a build-time).
        /// </summary>
        public struct EquippedSlotRefs
        {
            public Image Icon;
            public TMP_Text NameText;
            public Button RemoveButton;
            public GameObject FilledGroup;
            public GameObject EmptyGroup;
        }

        [Header("Tab strip (Mazzi / Emoticon / Accusi)")]
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private GameObject[] tabSelectedBgs;
        [SerializeField] private TMP_Text[] tabLabels;
        [SerializeField] private RectTransform[] tabContents;
        [Tooltip("RectTransform di ciascun tab (stesso ordine di tabButtons): il tab selezionato 'spunta' piu' in alto e piu' alto (geometria esatta fornita dall'utente, applicata qui a runtime - x e larghezza non cambiano mai, solo y/altezza).")]
        [SerializeField] private RectTransform[] tabRects;
        [SerializeField] private float tabNormalY = 224f;
        [SerializeField] private float tabNormalHeight = 56f;
        [SerializeField] private float tabSelectedY = 217f;
        [SerializeField] private float tabSelectedHeight = 69f;

        // Bug reale segnalato dall'utente: prima #0B2B22 (quasi nero) sul tab attivo, mentre nel
        // mockup TUTTE le label dei tab (attivo e inattivo) sono crema chiara con contorno nero
        // (stile cartoon, vedi outline aggiunto in DeckPageBuilder.CreateTabStrip) - stesso
        // colore ovunque, cambia solo lo sfondo pillola dietro.
        [SerializeField] private Color activeColor = new Color(0.980f, 0.957f, 0.878f, 1f);   // #FAF4E0 (UITheme.Cream)
        [SerializeField] private Color inactiveColor = new Color(0.843f, 0.894f, 0.949f, 1f); // #D7E4F2

        [Header("Mazzo")]
        [SerializeField] private SelectableToggleGroup deckGroup;
        [Tooltip("Le 3 Image del ventaglio anteprima, stesso ordine con cui vanno aggiornate quando cambia il mazzo selezionato.")]
        [SerializeField] private Image[] deckPreviewCardArt;
        [SerializeField] private TMP_Text deckNameText;
        [SerializeField] private TMP_Text deckSubtitleText;
        [Tooltip("Nomi dei soli mazzi SBLOCCATI, stesso ordine/indice degli item in deckGroup.")]
        [SerializeField] private string[] deckLabels;
        [SerializeField] private Sprite[] deckCardBackSprites;
        [Tooltip("ValueText del DeckSelector nella Home: la scelta su questa pagina e' live, si riflette subito li' senza bisogno di conferma.")]
        [SerializeField] private TMP_Text homeDeckSelectorValueText;

        [Header("Emoticon")]
        [SerializeField] private SelectableMultiToggleGroup emoticonGroup;
        [Tooltip("Nomi/sprite delle sole emoticon SBLOCCATE, stesso ordine/indice degli item in emoticonGroup.")]
        [SerializeField] private string[] emoticonNames;
        [SerializeField] private Sprite[] emoticonSprites;
        [Tooltip("Slot della riga 'EQUIPAGGIATE' in cima al tab, in ordine da sinistra a destra.")]
        [SerializeField] private Image[] equippedSlotIcons;
        [SerializeField] private TMP_Text[] equippedSlotNames;
        [SerializeField] private Button[] equippedSlotRemoveButtons;
        [SerializeField] private GameObject[] equippedSlotFilledGroup;
        [SerializeField] private GameObject[] equippedSlotEmptyGroup;

        [Header("Accuso")]
        [SerializeField] private SelectableToggleGroup accusoGroup;

        [Header("Reset tab al ritorno sulla pagina")]
        [Tooltip("PanelSwipeController.Start() attiva TUTTE le pagine una volta per sempre (serve per il drag che mostra le pagine adiacenti): DeckPage non si disattiva mai piu' dopo la prima volta, quindi OnEnable() non rifira' quando l'utente torna qui con lo swipe/bottom nav. Serve ascoltare OnPageChanged e confrontare l'indice.")]
        [SerializeField] private PanelSwipeController swipeController;
        [Tooltip("Indice di questa pagina nella lista 'pages' di swipeController, assegnato dal builder.")]
        [SerializeField] private int pageIndex = -1;

        private void Awake()
        {
            if (tabButtons != null)
            {
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    if (tabButtons[i] == null) continue;
                    int index = i;
                    tabButtons[i].onClick.AddListener(() => SelectTab(index));
                }
            }

            // Delegate diretto, non UnityEvent Editor-side: stesso motivo documentato in
            // SelectableToggleGroup/SelectableMultiToggleGroup.
            if (deckGroup != null) deckGroup.onSelectedRuntime = OnDeckSelected;
            if (emoticonGroup != null) emoticonGroup.onSelectionChangedRuntime = OnEmoticonSelectionChanged;

            SelectTab(0);
        }

        private void Start()
        {
            // SelectableMultiToggleGroup.Awake() applica gia' la selezione di default alla
            // GRIGLIA (checkmark) ma non invoca onSelectionChangedRuntime al primo giro (solo
            // su Toggle()) - senza questa chiamata esplicita la riga "EQUIPAGGIATE" in alto
            // resterebbe vuota finche' l'utente non tocca manualmente un'emoticon. Letta qui
            // (Start, non Awake): l'ordine Awake() tra componenti sibling NON e' garantito -
            // bug reale verificato via screenshot live, la riga restava vuota nonostante i
            // checkmark in griglia gia' corretti perche' DeckPageController.Awake() girava
            // prima di SelectableMultiToggleGroup.Awake() e leggeva SelectedIndices ancora
            // vuoto. Start() invece e' garantito girare dopo TUTTI gli Awake() della scena.
            if (emoticonGroup != null)
            {
                OnEmoticonSelectionChanged(new List<int>(emoticonGroup.SelectedIndices));
            }
        }

        private void OnEnable()
        {
            if (swipeController != null) swipeController.OnPageChanged += HandlePageChanged;
        }

        private void OnDisable()
        {
            if (swipeController != null) swipeController.OnPageChanged -= HandlePageChanged;
        }

        /// <summary>
        /// Ogni volta che si torna su questa pagina (swipe o tap sulla bottom nav bar),
        /// si riparte sempre dal tab Mazzi - non si resta sull'ultimo tab lasciato aperto.
        /// </summary>
        private void HandlePageChanged(int newIndex)
        {
            if (pageIndex >= 0 && newIndex == pageIndex)
            {
                SelectTab(0);
            }
        }

        public void SelectTab(int index)
        {
            for (int i = 0; i < (tabContents?.Length ?? 0); i++)
            {
                if (tabContents[i] != null) tabContents[i].gameObject.SetActive(i == index);
            }

            for (int i = 0; i < (tabSelectedBgs?.Length ?? 0); i++)
            {
                if (tabSelectedBgs[i] != null) tabSelectedBgs[i].SetActive(i == index);
            }

            for (int i = 0; i < (tabLabels?.Length ?? 0); i++)
            {
                if (tabLabels[i] != null) tabLabels[i].color = (i == index) ? activeColor : inactiveColor;
            }

            // Geometria esatta fornita dall'utente: il tab selezionato e' piu' alto e "spunta"
            // piu' in alto (y minore, altezza maggiore) - x e larghezza restano invariate.
            // anchoredPosition.y e' negativo perche' i tab usano pivot/anchor top-left
            // (SetTopLeft in DeckPageBuilder: anchoredPosition = (x0, -y0)).
            for (int i = 0; i < (tabRects?.Length ?? 0); i++)
            {
                if (tabRects[i] == null) continue;
                bool isSelected = i == index;
                var rt = tabRects[i];
                var pos = rt.anchoredPosition;
                pos.y = isSelected ? -tabSelectedY : -tabNormalY;
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, isSelected ? tabSelectedHeight : tabNormalHeight);
            }
        }

        private void OnDeckSelected(int index)
        {
            if (deckLabels == null || index < 0 || index >= deckLabels.Length) return;

            if (deckNameText != null)
            {
                deckNameText.text = deckLabels[index];
            }
            if (deckSubtitleText != null)
            {
                deckSubtitleText.text = "Mazzo standard · 40 carte";
            }

            var sprite = (deckCardBackSprites != null && index < deckCardBackSprites.Length)
                ? deckCardBackSprites[index]
                : null;
            if (sprite != null && deckPreviewCardArt != null)
            {
                foreach (var card in deckPreviewCardArt)
                {
                    if (card != null) card.sprite = sprite;
                }
            }

            if (homeDeckSelectorValueText != null)
            {
                homeDeckSelectorValueText.text = deckLabels[index];
            }
        }

        /// <summary>
        /// Ripopola la riga "EQUIPAGGIATE" in cima al tab Emoticon con le emoticon
        /// attualmente selezionate nella griglia (ordine crescente d'indice, stabile), e
        /// mostra "Slot libero" per gli slot avanzati. Il pulsante rimuovi di ogni slot
        /// pieno viene ri-agganciato ogni volta con l'indice corretto per quello slot.
        /// </summary>
        private void OnEmoticonSelectionChanged(List<int> selected)
        {
            if (equippedSlotFilledGroup == null) return;

            // NON riordinare per indice: "selected" e' gia' nell'ordine in cui l'utente ha
            // cliccato (vedi SelectableMultiToggleGroup) - il terzo slot deve riempirsi con
            // la terza emoticon scelta, non con quella di indice piu' basso rimasta libera.
            for (int slot = 0; slot < equippedSlotFilledGroup.Length; slot++)
            {
                bool filled = slot < selected.Count;
                if (equippedSlotFilledGroup[slot] != null) equippedSlotFilledGroup[slot].SetActive(filled);
                if (equippedSlotEmptyGroup != null && slot < equippedSlotEmptyGroup.Length && equippedSlotEmptyGroup[slot] != null)
                {
                    equippedSlotEmptyGroup[slot].SetActive(!filled);
                }

                if (!filled)
                {
                    continue;
                }

                int emoticonIndex = selected[slot];

                if (equippedSlotIcons != null && slot < equippedSlotIcons.Length && equippedSlotIcons[slot] != null
                    && emoticonSprites != null && emoticonIndex < emoticonSprites.Length)
                {
                    equippedSlotIcons[slot].sprite = emoticonSprites[emoticonIndex];
                }

                if (equippedSlotNames != null && slot < equippedSlotNames.Length && equippedSlotNames[slot] != null
                    && emoticonNames != null && emoticonIndex < emoticonNames.Length)
                {
                    equippedSlotNames[slot].text = emoticonNames[emoticonIndex];
                }

                if (equippedSlotRemoveButtons != null && slot < equippedSlotRemoveButtons.Length && equippedSlotRemoveButtons[slot] != null)
                {
                    var button = equippedSlotRemoveButtons[slot];
                    button.onClick.RemoveAllListeners();
                    int capturedIndex = emoticonIndex;
                    button.onClick.AddListener(() => emoticonGroup.Toggle(capturedIndex));
                }
            }
        }
    }
}
