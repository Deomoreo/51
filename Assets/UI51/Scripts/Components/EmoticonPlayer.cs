using Project51.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51
{
    /// <summary>
    /// Emoticon animata da un foglio 4x2 di 8 fotogrammi (SPEC §5-6): 0.9 s per l'andata 0..7, poi ritorno 7..0
    /// (CSS "steps infinite alternate", periodo 1.8 s). Still = solo il primo fotogramma (picker).
    /// Gira anche con la grafica ridotta: e' il contenuto, non un decoro. Indipendente dal timeScale.
    /// Usato anche per le fiamme della Home (Ambient): li' e' un decoro e con la grafica ridotta resta fermo.
    /// </summary>
    [AddComponentMenu("UI51/Emoticon Player")]
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public class EmoticonPlayer : MonoBehaviour
    {
        public const int FrameCount = 8;

        [SerializeField] Sprite[] m_Frames = new Sprite[0];
        [SerializeField] bool m_Still;
        [SerializeField, Tooltip("Decoro: fermo sul primo fotogramma con la grafica ridotta.")] bool m_Ambient;
        [SerializeField, Tooltip("Durata dell'andata 0->7 in secondi (CSS .9s).")] float m_Period = 0.9f;

        Image m_Image;
        float m_Time;
        int m_Shown = -1;

        public Image image => m_Image != null ? m_Image : (m_Image = GetComponent<Image>());
        public bool still { get => m_Still; set { m_Still = value; Restart(); } }
        public bool hasFrames => m_Frames != null && m_Frames.Length > 0;

        /// <summary>Fotogrammi in ordine di lettura del foglio (riga per riga). null o vuoto = immagine nascosta.</summary>
        public void SetFrames(Sprite[] frames)
        {
            m_Frames = frames ?? new Sprite[0];
            Restart();
        }

        /// <summary>Emoticon per indice di rete (0 Risata ... 5 Furbo, vedi UI51EmoticonSet).</summary>
        public void SetEmoticon(int index) => SetFrames(UI51EmoticonSet.Frames(index));

        /// <summary>Riparte dal primo fotogramma.</summary>
        public void Restart()
        {
            m_Time = 0f;
            m_Shown = -1;
            Show(0);
        }

        /// <summary>Fotogramma al tempo (s) per un'andata di period secondi, andata e ritorno.</summary>
        public static int FrameAt(float time, float period, int count = FrameCount)
        {
            if (count <= 1 || period <= 0f || time <= 0f) return 0;
            float cycle = time / period;
            int leg = Mathf.FloorToInt(cycle);
            int f = Mathf.Min(Mathf.FloorToInt((cycle - leg) * count), count - 1);
            return (leg & 1) == 0 ? f : count - 1 - f;
        }

        void OnEnable() => Restart();

        void Update()
        {
            if (m_Still || !hasFrames) return;
            if (m_Ambient && GamePreferences.ReducedGraphics) { if (m_Shown != 0) Restart(); return; }
            m_Time += Time.unscaledDeltaTime;
            float period = GamePreferences.Scaled(m_Period);
            if (m_Time > period * 2f) m_Time %= period * 2f;
            Show(FrameAt(m_Time, period, m_Frames.Length));
        }

        void Show(int frame)
        {
            var img = image;
            if (img == null) return;
            if (!hasFrames)
            {
                img.enabled = false;
                return;
            }
            frame = Mathf.Clamp(frame, 0, m_Frames.Length - 1);
            if (frame == m_Shown && img.enabled) return;
            m_Shown = frame;
            img.sprite = m_Frames[frame];
            img.enabled = img.sprite != null;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            m_Period = Mathf.Max(0.05f, m_Period);
            if (isActiveAndEnabled) Restart();
        }
#endif
    }
}
