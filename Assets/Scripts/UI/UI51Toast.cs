using DG.Tweening;
using Project51.UI51;
using TMPro;
using UnityEngine;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Avviso breve (mockup Toast): pillola a 96 dal fondo, sale, resta e svanisce (2.8 s). Un solo componente per tutta l'app:
    /// prefab in Resources (UI51/Toast, costruito da UI51SystemBuilder), creato al primo uso e tenuto tra le scene.
    /// Un nuovo avviso prende il posto di quello in corso. Non prende i tocchi.
    /// </summary>
    public sealed class UI51Toast : MonoBehaviour
    {
        public enum Kind { Success, Coins, Error }

        public const string ResourcePath = "UI51/Toast";
        const float Duration = 2.8f;

        [SerializeField] private RectTransform pill;
        [SerializeField] private UI51Shape shape;
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject successIcon, coinIcon, errorIcon;

        private static UI51Toast s_Instance;

        /// <summary>Mostra l'avviso. Senza prefab (build vecchia) non fa niente.</summary>
        public static void Show(string text, Kind kind = Kind.Success)
        {
            if (s_Instance == null)
            {
                var prefab = Resources.Load<UI51Toast>(ResourcePath);
                if (prefab == null) return;
                s_Instance = Instantiate(prefab);
                s_Instance.name = prefab.name;
                DontDestroyOnLoad(s_Instance.gameObject);
            }
            s_Instance.Play(text, kind);
        }

        private void Awake() => pill.gameObject.SetActive(false);

        private void Play(string text, Kind kind)
        {
            // Successo: alta 42, testo 13; le altre varianti del mockup: alta 40, testo 12.
            bool big = kind == Kind.Success;
            label.text = text;
            label.fontSize = big ? 13f : 12f;
            pill.sizeDelta = new Vector2(pill.sizeDelta.x, big ? 42f : 40f);
            shape.radii = UI51Tokens.Radii(big ? 21f : 20f);
            shape.borderColor = kind == Kind.Success ? UI51Tokens.WithAlpha(UI51Tokens.SuccessText, 0.55f)
                : kind == Kind.Coins ? UI51Tokens.GoldA(0.5f) : UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.55f);
            successIcon.SetActive(kind == Kind.Success);
            coinIcon.SetActive(kind == Kind.Coins);
            errorIcon.SetActive(kind == Kind.Error);
            pill.gameObject.SetActive(true);
            // @keyframes toast: 0% su di 20 e trasparente, 12%-85% fermo, 100% giu' di 10 e trasparente (ease-in-out).
            new UIKeyframes(Duration, UIEase.EaseInOut)
                .Track(AnimProp.Y, 0f, 20f, 0.12f, 0f, 0.85f, 0f, 1f, 10f)
                .Track(AnimProp.Alpha, 0f, 0f, 0.12f, 1f, 0.85f, 1f, 1f, 0f)
                .Play(pill)
                ?.OnComplete(() => pill.gameObject.SetActive(false));
        }
    }
}
