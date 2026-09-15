using TMPro;
using UnityEngine;

namespace Project51.UIV2.Components
{
    public class UIV2SectionHeader : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;

        public void SetTitle(string title)
        {
            if (titleLabel != null) titleLabel.text = title;
        }
    }
}
