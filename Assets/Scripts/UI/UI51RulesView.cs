using Project51.Core;
using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 12: pagina Regole (mockup Regole, schede Prese · Scopa · Accusi · Punteggio · Formati) sopra alle Impostazioni.
    /// Testi ed esempi li scrive UI51RulesBuilder; qui solo le schede e le facce delle carte del mazzo scelto.
    /// </summary>
    public sealed class UI51RulesView : MonoBehaviour
    {
        [SerializeField] private Button back, tutorial;
        [SerializeField] private Button[] tabs = new Button[0];
        [SerializeField] private UI51Shape[] tabShapes = new UI51Shape[0];
        [SerializeField] private TMP_Text[] tabLabels = new TMP_Text[0];
        [SerializeField] private RectTransform[] sections = new RectTransform[0];
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Image[] cards = new Image[0];
        [SerializeField] private int[] cardIds = new int[0]; // seme * 10 + valore - 1, come CardDeckDefinition

        private void Awake()
        {
            back.onClick.AddListener(Close);
            if (tutorial != null) tutorial.onClick.AddListener(UI51TutorialView.Launch);
            for (int i = 0; i < tabs.Length; i++)
            {
                int tab = i;
                tabs[i].onClick.AddListener(() => Select(tab));
            }
        }

        public void Open(int tab = 0)
        {
            gameObject.SetActive(true);
            var deck = CardDecks.LoadForMatch();
            for (int i = 0; i < cards.Length && i < cardIds.Length; i++)
                if (deck != null) cards[i].sprite = deck.GetFace(new Card((Suit)(cardIds[i] / 10), cardIds[i] % 10 + 1));
            Select(tab);
            UIAnim.FadeIn((RectTransform)transform, 0.2f);
        }

        public void Close() => gameObject.SetActive(false);

        private void Select(int tab)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                bool on = i == tab;
                tabShapes[i].fill = UI51Shape.Solid(on ? UI51Tokens.GoldA(0.16f) : UI51Tokens.Rgba(6, 13, 27, 0.6f));
                tabShapes[i].borderColor = UI51Tokens.GoldA(on ? 0.7f : 0.2f);
                tabLabels[i].color = on ? UI51Tokens.Gold : UI51Tokens.CreamA(0.75f);
                tabLabels[i].font = UI51Tokens.Font(on ? FontFace.NunitoExtraBold : FontFace.NunitoBold);
            }
            for (int i = 0; i < sections.Length; i++) sections[i].gameObject.SetActive(i == tab);
            scroll.verticalNormalizedPosition = 1f;
            // .fade del mockup senza la salita: la sezione sta in un layout, spostarla la lascerebbe nel posto vecchio.
            if (tab < sections.Length) UIAnim.FadeIn(sections[tab], 0.25f);
        }
    }
}
