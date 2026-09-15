using System;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// Casella trofeo del PROFILO V2: sbloccato = bordo oro + icona piena, bloccato = bordo blu,
    /// fondo scuro e icona scurita (stessa icona, solo tinta - nessuna arte "bloccata" separata).
    /// </summary>
    public class ProfileTrophyView : MonoBehaviour
    {
        [SerializeField] private Image border;
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private Image fill;
        [SerializeField] private Image icon;
        [SerializeField] private Button button;

        [Header("Stili (campionati da 17_profilo.png)")]
        [SerializeField] private Color unlockedBorderColor = new Color32(232, 178, 74, 255);
        [SerializeField] private Color lockedBorderColor = new Color32(70, 102, 142, 255);
        [SerializeField] private float unlockedBorderThickness = 4f;
        [SerializeField] private float lockedBorderThickness = 2f;
        [SerializeField] private Color unlockedFillColor = new Color32(18, 32, 52, 255);
        [SerializeField] private Color lockedFillColor = new Color32(14, 24, 40, 255);
        [SerializeField] private Color lockedIconTint = new Color(0.38f, 0.38f, 0.38f, 1f);

        private CollectionItemViewData _data;

        public CollectionItemViewData Data => _data;
        public event Action<CollectionItemViewData> OnPressed;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    if (_data != null) OnPressed?.Invoke(_data);
                });
            }
        }

        public void Bind(CollectionItemViewData data)
        {
            _data = data;
            if (data == null) return;

            bool unlocked = data.Unlocked;
            if (border != null) border.color = unlocked ? unlockedBorderColor : lockedBorderColor;
            if (fillRect != null)
            {
                float thickness = unlocked ? unlockedBorderThickness : lockedBorderThickness;
                fillRect.offsetMin = new Vector2(thickness, thickness);
                fillRect.offsetMax = new Vector2(-thickness, -thickness);
            }
            if (fill != null) fill.color = unlocked ? unlockedFillColor : lockedFillColor;
            if (icon != null)
            {
                if (data.Icon != null) icon.sprite = data.Icon;
                icon.color = unlocked ? data.TintColor : lockedIconTint;
            }
        }
    }
}
