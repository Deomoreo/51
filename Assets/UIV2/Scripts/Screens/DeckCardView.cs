using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// Card della griglia MAZZI: 3 stati da DeckViewData.State - Equipped (bordo oro, check, "In
    /// uso"), Owned (bottone teal "USA"), Locked (arte scurita + lucchetto, bottone blu con prezzo o
    /// requisito). Colori di default campionati da 20_collezione_carte.png.
    /// </summary>
    public class DeckCardView : MonoBehaviour
    {
        [SerializeField] private Image border;
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private Image fill;
        [SerializeField] private Image art;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject equippedBadge;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button actionButton;
        [SerializeField] private Image actionBackground;
        [SerializeField] private TMP_Text actionLabel;

        [Header("Stili")]
        [SerializeField] private Sprite lockedArtSprite;
        [SerializeField] private Color lockedArtTint = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] private Sprite ownedActionSprite;
        [SerializeField] private Sprite lockedActionSprite;
        [SerializeField] private Color equippedBorderColor = new Color32(232, 178, 74, 255);
        [SerializeField] private Color normalBorderColor = new Color32(70, 102, 142, 255);
        [SerializeField] private float equippedBorderThickness = 5f;
        [SerializeField] private float normalBorderThickness = 3f;
        [SerializeField] private Color unlockedFillColor = new Color32(26, 44, 68, 255);
        [SerializeField] private Color lockedFillColor = new Color32(14, 24, 40, 255);
        [SerializeField] private Color nameColor = new Color32(255, 236, 190, 255);
        [SerializeField] private Color lockedNameColor = new Color32(124, 146, 176, 255);
        [SerializeField] private string equippedStatusText = "In uso";
        [SerializeField] private string useActionText = "USA";

        private DeckViewData _data;

        public DeckViewData Data => _data;
        public event Action<DeckViewData> OnActionPressed;

        private void Awake()
        {
            if (actionButton != null) actionButton.onClick.AddListener(() => OnActionPressed?.Invoke(_data));
        }

        public void Bind(DeckViewData data)
        {
            _data = data;
            if (data == null) return;

            var state = data.State;
            bool equipped = state == DeckCardState.Equipped;
            bool locked = state == DeckCardState.Locked;

            if (border != null) border.color = equipped ? equippedBorderColor : normalBorderColor;
            if (fillRect != null)
            {
                float thickness = equipped ? equippedBorderThickness : normalBorderThickness;
                fillRect.offsetMin = new Vector2(thickness, thickness);
                fillRect.offsetMax = new Vector2(-thickness, -thickness);
            }
            if (fill != null) fill.color = locked ? lockedFillColor : unlockedFillColor;

            if (art != null)
            {
                art.preserveAspect = true;
                if (locked)
                {
                    if (lockedArtSprite != null) art.sprite = lockedArtSprite;
                    art.color = lockedArtTint;
                }
                else
                {
                    if (data.Artwork != null) art.sprite = data.Artwork;
                    art.color = Color.white;
                }
            }
            if (lockIcon != null) lockIcon.SetActive(locked);
            if (equippedBadge != null) equippedBadge.SetActive(equipped);

            if (nameLabel != null)
            {
                nameLabel.text = data.Name;
                nameLabel.color = locked ? lockedNameColor : nameColor;
            }
            if (statusLabel != null)
            {
                statusLabel.text = equippedStatusText;
                statusLabel.gameObject.SetActive(equipped);
            }

            if (actionButton != null)
            {
                actionButton.gameObject.SetActive(!equipped);
                if (!equipped)
                {
                    var sprite = locked ? lockedActionSprite : ownedActionSprite;
                    if (actionBackground != null && sprite != null) actionBackground.sprite = sprite;
                    if (actionLabel != null)
                    {
                        actionLabel.text = !locked ? useActionText
                            : (!string.IsNullOrEmpty(data.PriceText) ? data.PriceText : data.RequirementText);
                    }
                }
            }
        }
    }
}
