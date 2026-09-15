using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Variante multi-selezione (fino a maxSelected contemporanee) di SelectableToggleGroup:
    /// click su un item selezionato lo deseleziona, click su uno non selezionato lo aggiunge
    /// SOLO se non si e' gia' al massimo (altrimenti non fa nulla - nessun popup/errore, il
    /// feedback e' semplicemente che non si illumina). Usata per la scelta delle emoticon
    /// "equipaggiate" (max 3 usabili in partita, scelte tra quelle sbloccate).
    ///
    /// L'ORDINE conta (bug reale segnalato dall'utente): chi consuma SelectedIndices (es. la
    /// riga "EQUIPAGGIATE" in DeckPageController) deve poter riempire gli slot nell'ordine in
    /// cui l'utente ha effettivamente cliccato, non ordinato per indice di griglia - altrimenti
    /// il terzo elemento scelto puo' "saltare" in mezzo invece di andare nell'ultimo slot
    /// libero. Per questo _selected e' una List (ordine di inserimento), non piu' un HashSet.
    /// </summary>
    public class SelectableMultiToggleGroup : MonoBehaviour
    {
        [SerializeField] private List<SelectableToggleItem> items = new List<SelectableToggleItem>();
        [SerializeField] private int maxSelected = 3;
        [SerializeField] private List<int> defaultSelectedIndices = new List<int>();

        // Stesso motivo del campo analogo in SelectableToggleGroup: delegate C# assegnato a
        // runtime, mai un UnityEvent/AddPersistentListener Editor-side.
        public Action<List<int>> onSelectionChangedRuntime;

        private readonly List<int> _selected = new List<int>();

        private void Awake()
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;

                var button = item.GetComponent<Button>();
                if (button == null) continue;

                int index = i;
                button.onClick.AddListener(() => Toggle(index));
            }

            _selected.Clear();
            foreach (var idx in defaultSelectedIndices)
            {
                if (idx >= 0 && idx < items.Count && _selected.Count < maxSelected && !_selected.Contains(idx))
                {
                    _selected.Add(idx);
                }
            }

            ApplyVisualState();
        }

        public void Toggle(int index)
        {
            if (_selected.Contains(index))
            {
                _selected.Remove(index);
            }
            else if (_selected.Count < maxSelected)
            {
                _selected.Add(index);
            }

            ApplyVisualState();
            onSelectionChangedRuntime?.Invoke(new List<int>(_selected));
        }

        /// <summary>Ordine di INSERIMENTO (non ordinato per indice) - vedi nota di classe.</summary>
        public IReadOnlyList<int> SelectedIndices => _selected;

        private void ApplyVisualState()
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i]?.SetSelected(_selected.Contains(i));
            }
        }
    }
}
