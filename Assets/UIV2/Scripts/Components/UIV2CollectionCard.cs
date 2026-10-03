using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Data;
using Project51.UIV2.Animations;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Carta della griglia EMOTICON (UI51 Collection_EmoticonCard): icona, nome, segno di "equipaggiata"
    /// col numero d'ordine. I campi di stile sono opzionali (false/vuoti = nome e colori del prefab).
    /// </summary>
    public class UIV2CollectionCard : UIV2AnimatedComponent
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private GameObject equippedBadge;

        [Header("Stile (opzionale)")]
        [SerializeField] private Button button;
        [SerializeField] private bool overrideNameColors;
        [SerializeField] private Color nameColor = Color.white;
        [SerializeField] private Color equippedNameColor = Color.white;
        [SerializeField] private Color lockedNameColor = Color.white;
        [SerializeField] private string lockedTitleText;
        [SerializeField] private TMP_Text orderLabel;

        private CollectionItemViewData _data;

        public CollectionItemViewData Data => _data;
        public event Action<CollectionItemViewData> OnClicked;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            if (GetComponent<UIV2ButtonFeedback>() == null) PlayPress();
            OnClicked?.Invoke(_data);
        }

        public void Bind(CollectionItemViewData data)
        {
            _data = data;
            if (data == null) return;

            bool equipped = data.Unlocked && data.Equipped;

            if (nameLabel != null)
            {
                nameLabel.text = !data.Unlocked && !string.IsNullOrEmpty(lockedTitleText) ? lockedTitleText : data.Title;
                if (overrideNameColors)
                {
                    nameLabel.color = !data.Unlocked ? lockedNameColor : (equipped ? equippedNameColor : nameColor);
                }
            }

            if (orderLabel != null) orderLabel.text = data.Order > 0 ? data.Order.ToString() : string.Empty;

            if (data.Unlocked)
            {
                if (icon != null)
                {
                    icon.gameObject.SetActive(true);
                    if (data.Icon != null) icon.sprite = data.Icon;
                    icon.color = data.TintColor;
                }
                if (equippedBadge != null) equippedBadge.SetActive(data.Equipped);
            }
            else
            {
                if (icon != null) icon.gameObject.SetActive(false);
                if (equippedBadge != null) equippedBadge.SetActive(false);
            }
        }
    }
}
