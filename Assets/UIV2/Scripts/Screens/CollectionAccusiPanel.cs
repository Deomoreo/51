using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Components;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// Pannello ACCUSI della pagina COLLEZIONE (mockup 22_collezione_accusi (1).png): box IN USO
    /// (arte, titolo, sottotitolo, descrizione, ANTEPRIMA), conteggio + barra COLLEZIONE e lista
    /// accusi spawnata da Bind(). Nessun accuso e' hardcoded nel prefab.
    /// </summary>
    public class CollectionAccusiPanel : MonoBehaviour
    {
        [Header("IN USO")]
        [SerializeField] private Image heroArtwork;
        [SerializeField] private GameObject heroArtworkPlaceholder;
        [SerializeField] private TMP_Text heroTitleLabel;
        [SerializeField] private TMP_Text heroSubtitleLabel;
        [SerializeField] private TMP_Text heroDescriptionLabel;
        [SerializeField] private Button previewButton;
        [SerializeField] private bool uppercaseHeroTitle = true;

        [Header("COLLEZIONE")]
        [SerializeField] private TMP_Text collectionCountLabel;
        [SerializeField] private UIV2ProgressBar collectionProgress;
        [SerializeField] private RectTransform listContainer;
        [SerializeField] private AccusoRowView rowPrefab;

        private readonly List<AccusoRowView> _spawned = new List<AccusoRowView>();
        private AccusoViewData _equipped;

        public event Action<AccusoViewData> OnPreviewPressed;
        public event Action<AccusoViewData> OnAccusoActionPressed;

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

        /// <summary>
        /// totalCount = accusi esistenti nel catalogo (anche non in lista), per "1 / 6".
        /// </summary>
        public void Bind(IReadOnlyList<AccusoViewData> accusi, int totalCount)
        {
            ClearSpawned();

            int owned = 0;
            AccusoViewData equipped = null;
            if (accusi != null)
            {
                foreach (var accuso in accusi)
                {
                    if (accuso == null) continue;
                    if (accuso.Unlocked) owned++;
                    if (accuso.Unlocked && accuso.Equipped && equipped == null) equipped = accuso;
                    SpawnRow(accuso);
                }
            }

            SetEquippedAccuso(equipped);
            SetCollectionProgress(owned, Mathf.Max(totalCount, accusi != null ? accusi.Count : 0));
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
            if (heroArtworkPlaceholder != null) heroArtworkPlaceholder.SetActive(!hasArtwork);

            if (heroTitleLabel != null)
            {
                string title = accuso != null ? accuso.Title : "-";
                heroTitleLabel.text = uppercaseHeroTitle && title != null ? title.ToUpperInvariant() : title;
            }
            if (heroSubtitleLabel != null) heroSubtitleLabel.text = accuso != null ? accuso.Subtitle : string.Empty;
            if (heroDescriptionLabel != null) heroDescriptionLabel.text = accuso != null ? accuso.Description : string.Empty;
            if (previewButton != null) previewButton.interactable = accuso != null;
        }

        public void SetCollectionProgress(int owned, int total)
        {
            if (collectionCountLabel != null) collectionCountLabel.text = $"{owned} / {total}";
            if (collectionProgress != null) collectionProgress.SetProgress(total > 0 ? (float)owned / total : 0f, animate: false);
        }

        private void SpawnRow(AccusoViewData accuso)
        {
            if (rowPrefab == null || listContainer == null) return;
            var row = Instantiate(rowPrefab, listContainer);
            row.Bind(accuso);
            row.OnActionPressed += data => OnAccusoActionPressed?.Invoke(data);
            _spawned.Add(row);
        }

        private void ClearSpawned()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
            }
            _spawned.Clear();
        }
    }
}
