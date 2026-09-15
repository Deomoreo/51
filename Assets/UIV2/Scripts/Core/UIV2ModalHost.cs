using UnityEngine;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Apre/chiude i modal globali UIV2 uno alla volta dentro UIV2_Root/ModalHost. Ogni
    /// modal e' un prefab che implementa IUIV2Modal (es. UIV2ModalFrame): questo host non
    /// conosce i contenuti specifici (Ranking/Friends/Mail/...), solo il contratto Open/Close.
    /// </summary>
    public class UIV2ModalHost : MonoBehaviour
    {
        public static UIV2ModalHost Instance { get; private set; }

        private IUIV2Modal _current;

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

        public void Open(IUIV2Modal modal)
        {
            if (_current != null && _current != modal)
            {
                _current.Close();
            }
            _current = modal;
            _current?.Open();
        }

        public void CloseCurrent()
        {
            _current?.Close();
            _current = null;
        }
    }
}
