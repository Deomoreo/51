using Project51.UI51;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 12: Benvenuto (mockup Benvenuto) al primo ingresso in Home su questo telefono: "No, insegnami" apre la
    /// partita guidata, "Sì, voglio giocare" resta in Home. Grafica: UI51RulesBuilder; lo apre StartScreenV2.
    /// </summary>
    public sealed class UI51WelcomeView : MonoBehaviour
    {
        [SerializeField] private Button teach, play;

        const string SeenKey = "UI51WelcomeSeen";

        public static bool Pending => PlayerPrefs.GetInt(SeenKey, 0) == 0;

        private void Awake()
        {
            teach.onClick.AddListener(() => { Close(); UI51TutorialView.Launch(); });
            play.onClick.AddListener(Close);
        }

        public void Open()
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            gameObject.SetActive(true);
            UIAnim.FadeIn((RectTransform)transform, 0.3f);
        }

        private void Close() => gameObject.SetActive(false);
    }
}
