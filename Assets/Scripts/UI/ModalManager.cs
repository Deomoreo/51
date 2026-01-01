using UnityEngine;
using System.Collections.Generic;

namespace Project51.Unity
{
    /// <summary>
    /// Gestisce tutti i modal/popup dell'applicazione.
    /// Singleton accessibile globalmente.
    /// </summary>
    public class ModalManager : MonoBehaviour
    {
        public static ModalManager Instance { get; private set; }

        [Header("Modals")]
        [SerializeField] private List<ModalWindowBaseUI> modals;

        private ModalWindowBaseUI currentModal;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Rimane tra scene (opzionale)
        }

        public void Open(ModalWindowBaseUI modal)
        {
            if (currentModal != null)
            {
                Debug.Log($"Closing current modal: {currentModal.name}");
                currentModal.Close();
            }

            currentModal = modal;
            if (currentModal != null)
            {
                Debug.Log($"Opening modal: {currentModal.name}");
                currentModal.Open();
            }
        }

        /// <summary>
        /// Chiude il modal corrente.
        /// </summary>
        public void CloseCurrent()
        {
            if (currentModal != null)
            {
                Debug.Log($"Closing current modal: {currentModal.name}");
                currentModal.Close();
                currentModal = null;
            }
        }
    }
}
