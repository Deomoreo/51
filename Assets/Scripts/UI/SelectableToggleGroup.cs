using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Gruppo a selezione singola per SelectableToggleItem (righe modalita' o pillole
    /// difficolta'): al click su un Button di un item, deseleziona tutti gli altri e
    /// seleziona quello cliccato. Un solo componente riusato per entrambi i casi d'uso.
    /// </summary>
    public class SelectableToggleGroup : MonoBehaviour
    {
        [SerializeField] private List<SelectableToggleItem> items = new List<SelectableToggleItem>();
        [SerializeField] private int defaultSelectedIndex;

        // Delegate C# semplice invece di un UnityEvent+UnityEventTools.AddPersistentListener
        // (usato in un round precedente): quel wiring "Editor-side" e' un sospetto concreto
        // per una regressione dove TUTTA la selezione (righe E pillole) ha smesso di
        // funzionare - se AddPersistentListener falliva a build-time, interrompeva
        // silenziosamente il resto dello script che stava ancora wireando i gruppi. Un
        // campo Action normale, assegnato a runtime da chi lo usa (PanelModalitaController),
        // non passa mai dalla serializzazione Editor: molto meno rischioso.
        public Action<int> onSelectedRuntime;

        private void Awake()
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;

                var button = item.GetComponent<Button>();
                if (button == null) continue;

                int index = i;
                button.onClick.AddListener(() => Select(index));
            }

            Select(defaultSelectedIndex);
        }

        public void Select(int index)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].SetSelected(i == index);
                }
            }

            onSelectedRuntime?.Invoke(index);
        }
    }
}
