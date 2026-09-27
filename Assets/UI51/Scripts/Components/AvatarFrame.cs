using DG.Tweening;
using Project51.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51
{
    /// <summary>Cornici avatar dei mockup (SPEC §2, ProfiloCornice). Grigio = anello crema .25 (cornice non scelta).</summary>
    public enum FrameStyle { Oro, Blu, Smeraldo, Grigio }

    /// <summary>
    /// Avatar tondo con anello (SPEC §3.6): anello = cornice (conic approssimato con un lineare) o colore pieno,
    /// timer opzionale (Image Filled Radial360 oro dall'alto, orario) sopra l'anello, interno #0B1D3A con maschera
    /// circolare e immagine larga il 132% dell'interno, altezza dalle proporzioni dello sprite, centrata in orizzontale
    /// e col bordo alto 4% sopra l'interno (CSS width:132%;margin:-4% 0 0 -16%) per nascondere la cornice dipinta. L'emoticon ricevuta prende il posto dell'avatar: cerchio crema .95 con anello oro 2 px, Pop .3 s.
    /// Gerarchia creata da Tools/UI51/Component Prefabs.
    /// </summary>
    [AddComponentMenu("UI51/Avatar Frame")]
    [DisallowMultipleComponent]
    public class AvatarFrame : MonoBehaviour
    {
        const float AvatarScale = 1.32f;
        const float AvatarTop = 0.04f; // margin-top -4%: bordo alto sopra l'interno, in unita' dell'interno

        [SerializeField] RectTransform m_Face;
        [SerializeField] UI51Shape m_Ring;
        [SerializeField] Image m_Timer;
        [SerializeField] UI51Shape m_Inner;
        [SerializeField] Image m_Avatar;
        [SerializeField] UI51Shape m_Emoticon;
        [SerializeField] EmoticonPlayer m_EmoticonPlayer;
        [SerializeField, Tooltip("Spessore anello: 2 banner, 4 profilo, 6 grande.")] float m_RingWidth = 2f;

        public float ringWidth { get => m_RingWidth; set { m_RingWidth = Mathf.Max(0f, value); Layout(); } }
        public bool showingEmoticon => m_Emoticon != null && m_Emoticon.gameObject.activeSelf;

        void OnEnable()
        {
            Layout();
            // Un'emoticon rimasta a meta' (oggetto spento prima dei 3.2 s) non deve ricomparire.
            if (showingEmoticon) HideEmoticon();
        }

        void OnDisable() => DOTween.Kill(this);

        void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) Layout();
        }

        /// <summary>Immagine dell'avatar; null = solo fondo blu notte.</summary>
        public void SetAvatar(Sprite sprite)
        {
            if (m_Avatar == null) return;
            m_Avatar.sprite = sprite;
            m_Avatar.enabled = sprite != null;
            Layout();
        }

        /// <summary>Anello con una delle cornici dei mockup.</summary>
        public void SetFrame(FrameStyle style)
        {
            if (m_Ring == null) return;
            m_Ring.fill = FrameFill(style);
            m_Ring.angle = 110f; // "from 20deg": il lineare parte dall'alto a destra
            m_Ring.color = Color.white;
        }

        /// <summary>Anello in tinta unita (es. crema .18 sotto il timer).</summary>
        public void SetRing(Color color)
        {
            if (m_Ring == null) return;
            m_Ring.fill = UI51Shape.Solid(Color.white);
            m_Ring.color = color;
        }

        /// <summary>Anello con gradiente libero (cornici future dal catalogo).</summary>
        public void SetRingGradient(Gradient fill, float angle = 110f)
        {
            if (m_Ring == null || fill == null) return;
            m_Ring.fill = fill;
            m_Ring.angle = angle;
            m_Ring.color = Color.white;
        }

        /// <summary>Tempo rimasto 0..1 (arco oro dall'alto in senso orario); negativo = timer nascosto.</summary>
        public void SetTimer(float fraction)
        {
            if (m_Timer == null) return;
            bool on = fraction >= 0f;
            if (m_Timer.gameObject.activeSelf != on) m_Timer.gameObject.SetActive(on);
            if (on) m_Timer.fillAmount = Mathf.Clamp01(fraction);
        }

        /// <summary>Emoticon per indice di rete (0 Risata ... 5 Furbo) al posto dell'avatar per 3.2 s.</summary>
        public void ShowEmoticon(int index, float duration = 3.2f) => ShowEmoticon(UI51EmoticonSet.Frames(index), duration);

        public void ShowEmoticon(Sprite[] frames, float duration = 3.2f)
        {
            if (m_Emoticon == null || frames == null || frames.Length == 0 || !isActiveAndEnabled) return;
            DOTween.Kill(this);
            if (m_Face != null) m_Face.gameObject.SetActive(false);
            m_Emoticon.gameObject.SetActive(true);
            if (m_EmoticonPlayer != null) m_EmoticonPlayer.SetFrames(frames);
            UIAnim.Pop(m_Emoticon.rectTransform);
            if (duration > 0f)
                DOVirtual.DelayedCall(GamePreferences.Scaled(duration), HideEmoticon, true).SetId(this);
        }

        public void HideEmoticon()
        {
            DOTween.Kill(this);
            if (m_Emoticon != null)
            {
                UIAnim.Stop(m_Emoticon.rectTransform);
                m_Emoticon.gameObject.SetActive(false);
            }
            if (m_Face != null) m_Face.gameObject.SetActive(true);
        }

        /// <summary>Raggi, spessore anello e ritaglio dell'avatar dalla dimensione attuale. Pubblico per i builder.</summary>
        public void Layout()
        {
            var rt = (RectTransform)transform;
            float size = Mathf.Min(rt.rect.width, rt.rect.height);
            if (size <= 0f) return;
            if (m_Ring != null) m_Ring.radius = size * 0.5f;
            float inner = Mathf.Max(0f, size - 2f * m_RingWidth);
            if (m_Inner != null)
            {
                var ir = m_Inner.rectTransform;
                ir.offsetMin = new Vector2(m_RingWidth, m_RingWidth);
                ir.offsetMax = new Vector2(-m_RingWidth, -m_RingWidth);
                m_Inner.radius = inner * 0.5f;
            }
            if (m_Avatar != null)
            {
                var ar = m_Avatar.rectTransform;
                var sprite = m_Avatar.sprite;
                float aspect = sprite != null && sprite.rect.width > 0f ? sprite.rect.height / sprite.rect.width : 1f;
                float w = inner * AvatarScale;
                ar.anchorMin = ar.anchorMax = ar.pivot = new Vector2(0.5f, 1f);
                ar.sizeDelta = new Vector2(w, w * aspect);
                ar.anchoredPosition = new Vector2(0f, AvatarTop * inner);
            }
            // Il cerchio emoticon ha l'anello oro 2 px fuori dal diametro (CSS box-shadow 0 0 0 2px).
            if (m_Emoticon != null) m_Emoticon.radius = size * 0.5f + 2f;
        }

        /// <summary>Gradiente della cornice (stop dei conic-gradient dei mockup, SPEC §2).</summary>
        public static Gradient FrameFill(FrameStyle style)
        {
            switch (style)
            {
                case FrameStyle.Blu:
                    return Conic("#4F80E8", "#F3C969", "#1B3A7A", "#FCE29A", "#4F80E8");
                case FrameStyle.Smeraldo:
                    return Conic("#27B585", "#F3C969", "#0E6B4F", "#FCE29A", "#27B585");
                case FrameStyle.Grigio:
                    return UI51Shape.Solid(UI51Tokens.CreamA(0.25f));
                default:
                    return Conic("#FCE29A", "#C4922F", "#FFF1C4", "#8A5A12", "#FCE29A");
            }
        }

        static Gradient Conic(string a, string b, string c, string d, string e) => UI51Shape.Linear(
            (UI51Tokens.Hex(a), 0f), (UI51Tokens.Hex(b), 0.25f), (UI51Tokens.Hex(c), 0.5f),
            (UI51Tokens.Hex(d), 0.75f), (UI51Tokens.Hex(e), 1f));
    }
}
