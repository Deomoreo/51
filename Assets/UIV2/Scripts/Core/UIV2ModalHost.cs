using UnityEngine;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Apre i modal globali UIV2 (AnimatedModalV2: Legali, Elimina account) uno alla volta:
    /// aprendone uno, quello gia' aperto si chiude subito.
    /// </summary>
    public class UIV2ModalHost : MonoBehaviour
    {
        public static UIV2ModalHost Instance { get; private set; }

        private AnimatedModalV2 _current;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Open(AnimatedModalV2 modal)
        {
            if (_current != null && _current != modal)
            {
                _current.CloseImmediate();
            }
            _current = modal;
            if (_current != null) _current.Open();
        }
    }
}
