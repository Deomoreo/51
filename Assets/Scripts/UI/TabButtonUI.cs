using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    public class TabButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Color normalColor = Color.gray;
        [SerializeField] private Color selectedColor = Color.white;

        public void SetSelected(bool isSelected)
        {
            if (icon != null)
            {
                icon.color = isSelected ? selectedColor : normalColor;
            }
        }
    }
}