using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// Carta della griglia MAZZI (UI51 Collection_DeckCard): dorso, nome; sul mazzo in uso alone oro e
    /// "IN USO" al posto del pulsante. Bloccato = arte scurita.
    /// </summary>
    public class DeckCardView : MonoBehaviour
    {
        [SerializeField] private Image art;
        [SerializeField] private GameObject equippedBadge;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button actionButton;

        [Header("Stili")]
        [SerializeField] private Sprite lockedArtSprite;
        [SerializeField] private Color lockedArtTint = new Color(0.6f, 0.6f, 0.6f, 1f);
        [Tooltip("Falso = l'arte riempie tutta la carta (UI51: i dorsi hanno proporzioni un po' diverse fra loro).")]
        [SerializeField] private bool preserveArtAspect = true;
        [SerializeField] private Color nameColor = new Color32(255, 236, 190, 255);
        [SerializeField] private Color lockedNameColor = new Color32(124, 146, 176, 255);
        [SerializeField] private string equippedStatusText = "In uso";

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

            if (art != null)
            {
                art.preserveAspect = preserveArtAspect;
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

            if (actionButton != null) actionButton.gameObject.SetActive(!equipped);
        }
    }
}
