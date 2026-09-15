using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Interruttore on/off in stile iOS (pillola + pallino che scorre), costruito da due
    /// sprite generiche (traccia + pallino) invece di uno sprite dedicato (non esiste nel
    /// manifest Dragon's Hoard). Nessuna dipendenza da SelectableToggleGroup/Item (quella
    /// famiglia gestisce selezione singola/multipla tra piu' celle, qui serve solo un
    /// bool indipendente per riga, come nel mockup Impostazioni).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SimpleToggleSwitch : MonoBehaviour
    {
        [SerializeField] private Image trackImage;
        [SerializeField] private RectTransform knob;
        [SerializeField] private Color colorOn = new Color(0.20f, 0.72f, 0.44f);
        [SerializeField] private Color colorOff = new Color(0.14f, 0.18f, 0.26f);
        [SerializeField] private float knobOffsetX = 22f;
        [SerializeField] private bool isOn;

        public bool IsOn => isOn;

        /// <summary>Assegnato dal codice che crea l'istanza (Editor tool o controller), non da UnityEvent persistenti.</summary>
        public System.Action<bool> OnChanged;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Toggle);
            ApplyImmediate();
        }

        public void SetOn(bool value, bool animate = false)
        {
            isOn = value;
            if (animate && Application.isPlaying)
            {
                trackImage.DOColor(isOn ? colorOn : colorOff, 0.15f);
                knob.DOAnchorPosX(isOn ? knobOffsetX : -knobOffsetX, 0.15f).SetEase(Ease.OutQuad);
            }
            else
            {
                ApplyImmediate();
            }
        }

        private void Toggle()
        {
            SetOn(!isOn, animate: true);
            OnChanged?.Invoke(isOn);
        }

        private void ApplyImmediate()
        {
            if (trackImage != null)
            {
                trackImage.color = isOn ? colorOn : colorOff;
            }

            if (knob != null)
            {
                var pos = knob.anchoredPosition;
                pos.x = isOn ? knobOffsetX : -knobOffsetX;
                knob.anchoredPosition = pos;
            }
        }
    }
}
