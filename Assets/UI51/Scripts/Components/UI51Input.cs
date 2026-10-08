using DG.Tweening;
using Project51.Core;
using TMPro;
using UnityEngine;

namespace Project51.UI51
{
    /// <summary>
    /// Stile dei campi di Accesso e Registrazione (SPEC §3, mockup Main): scatola r14 bianco .06 con bordo oro .3,
    /// che al focus passa a bianco .1 e bordo oro pieno in 0.15 s. Solo grafica: il testo resta del TMP_InputField.
    /// </summary>
    [AddComponentMenu("UI51/Input")]
    [DisallowMultipleComponent]
    public class UI51Input : MonoBehaviour
    {
        [SerializeField] TMP_InputField m_Field;
        [SerializeField] UI51Shape m_Box;
        [SerializeField] float m_Duration = 0.15f;
        [Tooltip("Bordo a riposo e col focus: oro .3 -> oro pieno (rosso nella conferma Elimina account).")]
        [SerializeField] Color m_Border = new Color(243f / 255f, 201f / 255f, 105f / 255f, 0.3f);
        [SerializeField] Color m_BorderFocus = new Color(243f / 255f, 201f / 255f, 105f / 255f, 1f);

        float m_T;
        RectTransform m_Form;

        /// <summary>Falso dove la finestra gestisce da se' la tastiera (Elimina account).</summary>
        [System.NonSerialized] public bool LiftPanel = true;

        // Tastiera del telefono (una sola alla volta): sale il modulo del campo col focus finche' il campo sta sopra la tastiera.
        // Terzo giro Android 08/10: sale solo il modulo (il figlio di "Safe" che contiene il campo: il foglio di Accesso e Registrazione,
        // il corpo di Recupero), non piu' tutto Safe con logo e decorazioni. Safe lo tiene fermo DesignCanvasFit; il modulo lo sposta
        // solo questo codice, dalla sua posizione di riposo (s_Base) e con un movimento morbido, e ce lo rimette alla chiusura.
        static RectTransform s_Form;
        static Vector2 s_Base;
        static float s_Target, s_Applied, s_Speed, s_ClosedAt = -1f;
        static int s_DrivenFrame = -1, s_MeasuredFrame = -10;
        static readonly Vector3[] s_Corners = new Vector3[4];

        /// <summary>
        /// Quota di schermo coperta dalla tastiera quando Android non la dice (campo di Unity in un suo dialog, app a schermo intero).
        /// Per eccesso: un campo un po' piu' in alto del necessario si vede comunque, uno coperto no.
        /// </summary>
        public const float AndroidKeyboardShare = 0.45f;

        /// <summary>Stacco tra il campo e la tastiera, in unita' di progetto (terzo giro 08/10: 22, il campo non tocca la tastiera).</summary>
        public const float KeyboardGap = 22f;

        /// <summary>
        /// Di quanto deve salire il contenuto (unita' di progetto) perche' il bordo basso del campo stia sopra la tastiera.
        /// Misure in pixel dal basso dello schermo; unit = pixel per unita'. Mai oltre il bordo alto dell'area sicura.
        /// </summary>
        public static float KeyboardLift(float keyboardPx, float bottomPx, float topPx, float safeTopPx, float unit)
        {
            if (keyboardPx <= 0f || unit <= 0f) return 0f;
            float wanted = (keyboardPx - bottomPx) / unit + KeyboardGap;
            float room = (safeTopPx - topPx) / unit - KeyboardGap;
            return Mathf.Clamp(wanted, 0f, Mathf.Max(0f, room));
        }

        /// <summary>
        /// Altezza della tastiera in pixel dello schermo (0 se chiusa). Giro Android 08/10: su Android TouchScreenKeyboard.area resta a 0
        /// con l'app a schermo intero e il campo di sistema nascosto: li' si misura quanta finestra copre la tastiera
        /// (getWindowVisibleDisplayFrame), una volta per frame per tutti i campi. Secondo giro: Unity apre la tastiera da un suo dialog e
        /// la finestra del gioco puo' non accorgersene (misura 0 a tastiera aperta): con un campo attivo (fieldFocused) si usa
        /// AndroidKeyboardShare dello schermo. Nel log ([UI51Input]) le misure a ogni cambio, per la prova sul telefono.
        /// </summary>
        public static float KeyboardHeight(bool fieldFocused = false)
        {
            float area = TouchScreenKeyboard.visible ? TouchScreenKeyboard.area.height : 0f;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Time.frameCount != s_KeyboardFrame) { s_KeyboardFrame = Time.frameCount; s_AndroidKeyboard = AndroidKeyboard(); }
            float height = Mathf.Max(area, s_AndroidKeyboard);
            if (height <= 0f && fieldFocused) height = Screen.height * AndroidKeyboardShare;
            int key = Mathf.RoundToInt(area) * 31 + Mathf.RoundToInt(s_AndroidKeyboard) * 7 + Mathf.RoundToInt(height);
            if (key != s_Logged)
            {
                s_Logged = key;
                Debug.Log($"[UI51Input] tastiera: area {area:0}, finestra {s_AndroidKeyboard:0}, usata {height:0} su {Screen.height}");
            }
            return height;
#else
            return area;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static int s_KeyboardFrame = -1, s_Logged = -1;
        static float s_AndroidKeyboard;

        static float AndroidKeyboard()
        {
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var window = activity.Call<AndroidJavaObject>("getWindow"))
                using (var decor = window.Call<AndroidJavaObject>("getDecorView"))
                using (var visible = new AndroidJavaObject("android.graphics.Rect"))
                {
                    decor.Call("getWindowVisibleDisplayFrame", visible);
                    int height = decor.Call<int>("getHeight");
                    if (height <= 0) return 0f;
                    float covered = height - visible.Get<int>("bottom");
                    // Barra di navigazione o di stato (pochi dp): non e' la tastiera.
                    if (covered < height * 0.15f) return 0f;
                    return covered * Screen.height / height; // pixel della finestra -> pixel di Unity (risoluzione ridotta)
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[UI51Input] altezza tastiera: " + e.Message);
                return 0f;
            }
        }
#endif

        void Awake()
        {
            if (m_Field == null) return;
            // Niente barra di testo del sistema sopra la tastiera (nera su iPhone, bianca su Android): si scrive nel campo vero.
            m_Field.shouldHideMobileInput = true;
            // Build 3 I4: niente "seleziona tutto al tocco", che col campo in place copriva il testo con un rettangolo oro.
            m_Field.onFocusSelectAll = false;
            for (var t = m_Field.transform; t.parent != null; t = t.parent)
                if (t.parent.name == "Safe") { m_Form = (RectTransform)t; break; }
        }

        void LateUpdate()
        {
            if (m_Field == null || m_Form == null || !LiftPanel) return;
            if (m_Field.isFocused) Measure();
            if (s_Form == m_Form) Drive(); // niente misure (JNI su Android) dai campi a riposo
        }

        /// <summary>Quanto deve stare sollevato il modulo perche' il campo col focus resti sopra la tastiera (s_Target).</summary>
        void Measure()
        {
            float keyboard = KeyboardHeight(true);
            if (keyboard <= 0f) return; // tastiera chiusa: la discesa la decide Drive
            if (s_Form != m_Form) { Restore(); s_Form = m_Form; s_Base = m_Form.anchoredPosition; }
            s_MeasuredFrame = Time.frameCount;
            var rt = (RectTransform)m_Field.transform;
            if (rt.rect.height <= 0f) return;
            var canvas = rt.GetComponentInParent<Canvas>();
            var cam = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            rt.GetWorldCorners(s_Corners);
            float bottom = RectTransformUtility.WorldToScreenPoint(cam, s_Corners[0]).y;
            float top = RectTransformUtility.WorldToScreenPoint(cam, s_Corners[1]).y;
            float unit = (top - bottom) / rt.rect.height;
            // Posizione del campo a modulo fermo: il calcolo non dipende da dove il modulo sta andando (niente oscillazioni).
            bottom -= s_Applied * unit;
            top -= s_Applied * unit;
            float target = KeyboardLift(keyboard, bottom, top, Screen.safeArea.yMax, unit);
            // Col dito sullo schermo il modulo non scende (il tocco finirebbe su un'altra cosa); salire si', subito.
            if (target < s_Target && Input.touchCount > 0) return;
            s_Target = target;
        }

        /// <summary>
        /// Una volta per frame: nessun campo col focus e la tastiera aperta da 0,2 s (Avanti fra due campi non fa scendere e risalire) e
        /// nessun dito sullo schermo -> il modulo torna giu'. Il modulo va verso s_Target in circa 0,2 s, senza scatti.
        /// </summary>
        static void Drive()
        {
            if (s_Form == null || s_DrivenFrame == Time.frameCount) return;
            s_DrivenFrame = Time.frameCount;
            bool open = Time.frameCount - s_MeasuredFrame <= 1; // Measure l'ha visto in questo frame o nel precedente
            if (open) s_ClosedAt = -1f;
            else if (s_ClosedAt < 0f) s_ClosedAt = Time.unscaledTime;
            else if (Time.unscaledTime - s_ClosedAt > 0.2f && Input.touchCount == 0) s_Target = 0f;
            s_Applied = Mathf.SmoothDamp(s_Applied, s_Target, ref s_Speed, 0.07f, Mathf.Infinity, Time.unscaledDeltaTime);
            if (Mathf.Abs(s_Applied - s_Target) < 0.25f) { s_Applied = s_Target; s_Speed = 0f; }
            s_Form.anchoredPosition = s_Base + Vector2.up * s_Applied;
            if (s_Applied == 0f && s_Target == 0f) s_Form = null; // a riposo: il modulo e' di nuovo libero
        }

        /// <summary>Modulo subito al suo posto (pannello chiuso, altro modulo).</summary>
        static void Restore()
        {
            if (s_Form != null) s_Form.anchoredPosition = s_Base;
            s_Form = null;
            s_Target = s_Applied = s_Speed = 0f;
            s_ClosedAt = -1f;
        }

        void OnEnable()
        {
            if (m_Field != null)
            {
                m_Field.onSelect.AddListener(Focus);
                m_Field.onDeselect.AddListener(Blur);
                m_Field.onSubmit.AddListener(Next);
            }
            Apply(m_Field != null && m_Field.isFocused ? 1f : 0f);
        }

        void OnDisable()
        {
            if (m_Field != null)
            {
                m_Field.onSelect.RemoveListener(Focus);
                m_Field.onDeselect.RemoveListener(Blur);
                m_Field.onSubmit.RemoveListener(Next);
            }
            DOTween.Kill(this);
            if (m_Form != null && s_Form == m_Form) Restore(); // pannello chiuso con la tastiera aperta
        }

        /// <summary>
        /// B25 (I5): Invio passa al campo successivo dello stesso modulo (fratello attivo con UI51Input). Un frame dopo,
        /// perche' TMP toglie il focus subito dopo onSubmit. L'ultimo campo chiude solo la tastiera.
        /// </summary>
        void Next(string _)
        {
            var parent = transform.parent;
            if (parent == null) return;
            for (int i = transform.GetSiblingIndex() + 1; i < parent.childCount; i++)
            {
                var next = parent.GetChild(i).GetComponent<UI51Input>();
                if (next == null || !next.isActiveAndEnabled || next.m_Field == null || !next.m_Field.interactable) continue;
                DOVirtual.DelayedCall(0f, () => { if (next != null && next.isActiveAndEnabled) next.m_Field.ActivateInputField(); }, true);
                return;
            }
        }

        void Focus(string _) => AnimateTo(1f);
        void Blur(string _) => AnimateTo(0f);

        void AnimateTo(float target)
        {
            DOTween.Kill(this);
            if (!isActiveAndEnabled) { Apply(target); return; }
            DOVirtual.Float(m_T, target, GamePreferences.Scaled(m_Duration), Apply)
                .SetEase(Ease.OutQuad).SetUpdate(true).SetId(this);
        }

        void Apply(float t)
        {
            m_T = t;
            if (m_Box == null) return;
            // fill, non color: color e' una tinta moltiplicata sul riempimento (0.06 x 0.06 = invisibile).
            m_Box.color = Color.white;
            m_Box.fill = UI51Shape.Solid(UI51Tokens.WhiteA(Mathf.Lerp(0.06f, 0.1f, t)));
            m_Box.borderColor = Color.Lerp(m_Border, m_BorderFocus, t);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (m_Field == null) m_Field = GetComponent<TMP_InputField>();
            if (m_Box == null) m_Box = GetComponent<UI51Shape>();
        }
#endif
    }
}
