using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Components
{
    public class UIV2StatTile : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private TMP_Text captionLabel;

        public void SetStat(Sprite iconSprite, string value, string caption)
        {
            if (icon != null && iconSprite != null) icon.sprite = iconSprite;
            if (valueLabel != null && value != null) valueLabel.text = value;
            // null = mantieni la caption del prefab (stesso pattern null-guard dell'icona).
            if (captionLabel != null && caption != null) captionLabel.text = caption;
        }
    }
}
