using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Interfaccia per pannelli che necessitano di un riferimento alla bottom bar.
    /// </summary>
    public interface IBottomBarAware
    {
        void SetBottomBarRoot(GameObject root);
    }

    /// <summary>
    /// Manager centralizzato per aprire/chiudere pannelli slide-up.
    /// Permette di registrare bottoni ? pannelli in modo dichiarativo.
    /// </summary>
    public class PanelManager : MonoBehaviour
    {
        public static PanelManager Instance { get; private set; }

        [Serializable]
        public class ButtonPanelBinding
        {
            public Button button;
            public SlideUpPanelUI panel;
        }

        [Header("Button ? Panel Bindings")]
        [Tooltip("Associa bottoni a pannelli. Quando il bottone viene premuto, il pannello si apre.")]
        [SerializeField] private List<ButtonPanelBinding> bindings = new List<ButtonPanelBinding>();

        [Header("Global References (optional)")]
        [Tooltip("Se assegnata, viene passata ai pannelli che implementano IBottomBarAware.")]
        [SerializeField] private GameObject bottomBarRoot;

        [Tooltip("Componenti da disabilitare quando un pannello è aperto.")]
        [SerializeField] private Behaviour[] disableWhilePanelOpen;

        private SlideUpPanelUI _currentOpenPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Registra tutti i binding
            foreach (var binding in bindings)
            {
                if (binding.button != null && binding.panel != null)
                {
                    var panel = binding.panel; // capture for closure
                    binding.button.onClick.AddListener(() => OpenPanel(panel));
                    panel.OnVisibilityChanged += (isOpen) => HandlePanelVisibilityChanged(panel, isOpen);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Apre un pannello. Chiude automaticamente quello attualmente aperto.
        /// </summary>
        public void OpenPanel(SlideUpPanelUI panel)
        {
            if (panel == null) return;

            // Chiudi il pannello attuale se diverso
            if (_currentOpenPanel != null && _currentOpenPanel != panel && _currentOpenPanel.IsOpen)
                _currentOpenPanel.Close();

            // Se il pannello implementa IBottomBarAware, passa la bottom bar
            if (bottomBarRoot != null)
            {
                var bottomBarAware = panel as IBottomBarAware;
                if (bottomBarAware != null)
                    bottomBarAware.SetBottomBarRoot(bottomBarRoot);
            }

            panel.Open();
        }

        /// <summary>
        /// Chiude il pannello attualmente aperto.
        /// </summary>
        public void CloseCurrentPanel()
        {
            if (_currentOpenPanel != null && _currentOpenPanel.IsOpen)
                _currentOpenPanel.Close();
        }

        /// <summary>
        /// Registra un binding bottone ? pannello a runtime.
        /// </summary>
        public void RegisterBinding(Button button, SlideUpPanelUI panel)
        {
            if (button == null || panel == null) return;

            button.onClick.AddListener(() => OpenPanel(panel));
            panel.OnVisibilityChanged += (isOpen) => HandlePanelVisibilityChanged(panel, isOpen);

            bindings.Add(new ButtonPanelBinding { button = button, panel = panel });
        }

        private void HandlePanelVisibilityChanged(SlideUpPanelUI panel, bool isOpen)
        {
            if (isOpen)
            {
                _currentOpenPanel = panel;
                SetGlobalComponentsEnabled(false);
            }
            else
            {
                if (_currentOpenPanel == panel)
                    _currentOpenPanel = null;
                SetGlobalComponentsEnabled(true);
            }
        }

        private void SetGlobalComponentsEnabled(bool enabled)
        {
            if (disableWhilePanelOpen == null) return;

            foreach (var comp in disableWhilePanelOpen)
            {
                if (comp != null)
                    comp.enabled = enabled;
            }
        }

        /// <summary>
        /// Controlla se un pannello è attualmente aperto.
        /// </summary>
        public bool IsAnyPanelOpen => _currentOpenPanel != null && _currentOpenPanel.IsOpen;
    }
}
