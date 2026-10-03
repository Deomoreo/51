using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// Pannello ACCUSI della pagina COLLEZIONE: box IN USO (arte, titolo, descrizione, ANTEPRIMA)
    /// dell'accuso equipaggiato. Nessun accuso e' scritto nella scena.
    /// </summary>
    public class CollectionAccusiPanel : MonoBehaviour
    {
        [Header("IN USO")]
        [SerializeField] private Image heroArtwork;
        [SerializeField] private TMP_Text heroTitleLabel;
        [SerializeField] private TMP_Text heroDescriptionLabel;
        [SerializeField] private Button previewButton;
        [SerializeField] private bool uppercaseHeroTitle = true;

        private AccusoViewData _equipped;

        public event Action<AccusoViewData> OnPreviewPressed;

        private void Awake()
        {
            if (previewButton != null)
            {
                previewButton.onClick.AddListener(() =>
                {
                    if (_equipped != null) OnPreviewPressed?.Invoke(_equipped);
                });
            }
        }

        public void Bind(IReadOnlyList<AccusoViewData> accusi)
        {
            AccusoViewData equipped = null;
            if (accusi != null)
                foreach (var accuso in accusi)
                    if (accuso != null && accuso.Unlocked && accuso.Equipped) { equipped = accuso; break; }
            SetEquippedAccuso(equipped);
        }

        public void SetEquippedAccuso(AccusoViewData accuso)
        {
            _equipped = accuso;

            bool hasArtwork = accuso != null && accuso.Artwork != null;
            if (heroArtwork != null)
            {
                heroArtwork.gameObject.SetActive(hasArtwork);
                if (hasArtwork) heroArtwork.sprite = accuso.Artwork;
            }

            if (heroTitleLabel != null)
            {
                string title = accuso != null ? accuso.Title : "-";
                heroTitleLabel.text = uppercaseHeroTitle && title != null ? title.ToUpperInvariant() : title;
            }
            if (heroDescriptionLabel != null) heroDescriptionLabel.text = accuso != null ? accuso.Description : string.Empty;
            if (previewButton != null) previewButton.interactable = accuso != null;
        }
    }
}
