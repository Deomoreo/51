using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Pannello Deck Selector che eredita da SlideUpPanelUI.
    /// Aggiunge la logica specifica per nascondere/mostrare la bottom bar.
    /// </summary>
    public class DeckSelectorPanelUI : SlideUpPanelUI, IBottomBarAware
    {
        [Header("Hide Home UI (behind)")]
        [Tooltip("Oggetti UI della Home da nascondere quando il DeckSelector è aperto.")]
        [SerializeField] private GameObject[] hideWhileOpen;

        [Header("Swipe Lock")]
        [Tooltip("Disabilita questi componenti quando il DeckSelector è aperto.")]
        [SerializeField] private Behaviour[] disableBehavioursWhileOpen;

        [Header("Overlay Button")]
        [SerializeField] private Button topOverlayButton;

        [Header("Close Button")]
        [SerializeField] private Button closeButton;

        [Header("Bottom Bar")]
        [Tooltip("Root GameObject della bottom bar da nascondere/mostrare.")]
        [SerializeField] private GameObject bottomBarRoot;

        protected override void Awake()
        {
            base.Awake();

            SetBehindVisibility(false);
            SetSwipeLock(false);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (topOverlayButton != null)
                topOverlayButton.onClick.AddListener(Close);
        }

        private void SetBehindVisibility(bool isOpen)
        {
            if (hideWhileOpen == null) return;
            foreach (var go in hideWhileOpen)
            {
                if (go != null)
                    go.SetActive(!isOpen);
            }
        }

        private void SetSwipeLock(bool isOpen)
        {
            if (disableBehavioursWhileOpen == null) return;
            foreach (var b in disableBehavioursWhileOpen)
            {
                if (b != null)
                    b.enabled = !isOpen;
            }
        }

        protected override void OnOpening()
        {
            SetBehindVisibility(true);
            SetSwipeLock(true);

            if (bottomBarRoot != null)
                bottomBarRoot.SetActive(false);
        }

        protected override void OnClosed()
        {
            if (bottomBarRoot != null)
                bottomBarRoot.SetActive(true);

            SetBehindVisibility(false);
            SetSwipeLock(false);
        }

        public void SetBottomBarRoot(GameObject root)
        {
            bottomBarRoot = root;
        }
    }
}
