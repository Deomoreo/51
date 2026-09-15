using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// Slot EQUIPAGGIATE del pannello EMOTICON: pieno (emoticon grande + nome + X rimuovi) o vuoto
    /// (bordo tratteggiato + "+" + "Slot libero"). Bind(null, index) = slot vuoto.
    /// </summary>
    public class EmoticonSlotView : MonoBehaviour
    {
        [SerializeField] private GameObject filledRoot;
        [SerializeField] private GameObject emptyRoot;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private Button removeButton;
        [SerializeField] private Button emptyButton;

        private CollectionItemViewData _item;
        private int _slotIndex;

        public CollectionItemViewData Item => _item;
        public event Action<CollectionItemViewData> OnRemovePressed;
        public event Action<int> OnEmptyPressed;

        private void Awake()
        {
            if (removeButton != null)
            {
                removeButton.onClick.AddListener(() =>
                {
                    if (_item != null) OnRemovePressed?.Invoke(_item);
                });
            }
            if (emptyButton != null) emptyButton.onClick.AddListener(() => OnEmptyPressed?.Invoke(_slotIndex));
        }

        public void Bind(CollectionItemViewData item, int slotIndex)
        {
            _item = item;
            _slotIndex = slotIndex;

            bool filled = item != null;
            if (filledRoot != null) filledRoot.SetActive(filled);
            if (emptyRoot != null) emptyRoot.SetActive(!filled);
            if (!filled) return;

            if (icon != null && item.Icon != null) icon.sprite = item.Icon;
            if (nameLabel != null) nameLabel.text = item.Title;
        }
    }
}
