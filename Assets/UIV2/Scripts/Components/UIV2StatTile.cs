using TMPro;
using UnityEngine;

namespace Project51.UIV2.Components
{
    public class UIV2StatTile : MonoBehaviour
    {
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private TMP_Text captionLabel;

        public void SetStat(string value, string caption)
        {
            if (valueLabel != null && value != null) valueLabel.text = value;
            // null = mantieni la caption del prefab.
            if (captionLabel != null && caption != null) captionLabel.text = caption;
        }
    }
}
