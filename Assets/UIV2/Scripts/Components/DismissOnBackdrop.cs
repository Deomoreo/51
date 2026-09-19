using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Sul velo scuro dietro a un pannello: toccare fuori dal pannello fa esattamente quello che fa
    /// la sua X (stesso pulsante, quindi stessi annullamenti: anteprima del mazzo, ritorno a
    /// Modalita' da Crea/Entra stanza...). Se la X non e' attiva o non e' cliccabile non fa niente.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class DismissOnBackdrop : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Button closeButton;

        public Button CloseButton
        {
            get => closeButton;
            set => closeButton = value;
        }

        private void Awake()
        {
            GetComponent<Graphic>().raycastTarget = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (closeButton == null || !closeButton.isActiveAndEnabled || !closeButton.interactable) return;
            closeButton.onClick.Invoke();
        }
    }
}
