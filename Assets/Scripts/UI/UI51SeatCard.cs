using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Posto al tavolo (mockup Matchmaking e SalaPrivata): pieno = avatar con anello della squadra, nome e riga sotto
    /// ("Liv. 12", "Ospite", "Computer"), bordo oro (la tua squadra) o blu; vuoto = riquadro tratteggiato con la scritta del vuoto.
    /// La card piena entra con un pop quando il posto si riempie. Campi facoltativi: riga sotto, etichetta in alto (HOST, BOT),
    /// pulsante (bot nella sala privata), anello che pulsa nel vuoto. teamBorder false = bordo del builder (sala privata).
    /// </summary>
    public sealed class UI51SeatCard : MonoBehaviour
    {
        public sealed class Info
        {
            public string Name, Detail, Tag;
            public Sprite Portrait;
            public bool Ally;
        }

        [SerializeField] private RectTransform full;
        [SerializeField] private GameObject empty;
        [SerializeField] private UI51Shape panel;
        [SerializeField] private AvatarFrame avatar;
        [SerializeField] private TMP_Text nameLabel, detailLabel, emptyLabel, tagLabel;
        [SerializeField] private UI51Shape pulse;
        [SerializeField] private Button button;
        [SerializeField] private bool teamBorder = true;

        private int state = -1; // -1 da rifare (appena acceso), 0 vuoto, 1 pieno
        private Sprite portrait;

        public Button Button => button;

        // Spenta, l'anello smette di pulsare (SetLink): alla riaccensione si riparte.
        private void OnDisable() => state = -1;

        /// <summary>info null = posto vuoto con emptyText (null = lascia la scritta del builder).</summary>
        public void Bind(Info info, string emptyText = null)
        {
            gameObject.SetActive(true);
            bool filled = info != null;
            if ((filled ? 1 : 0) != state)
            {
                full.gameObject.SetActive(filled);
                empty.SetActive(!filled);
                if (filled && state == 0) UIAnim.Pop(full, 0.8f, 1.04f, 0.3f); // qualcuno si e' appena seduto
                if (!filled && pulse != null) UIAnim.Pulse(pulse, 9f, 1.6f, 0.6f);
                state = filled ? 1 : 0;
            }
            if (!filled)
            {
                if (emptyText != null) emptyLabel.text = emptyText;
                return;
            }
            nameLabel.text = info.Name;
            if (detailLabel != null) detailLabel.text = info.Detail;
            if (tagLabel != null)
            {
                tagLabel.gameObject.SetActive(!string.IsNullOrEmpty(info.Tag));
                tagLabel.text = info.Tag;
            }
            if (teamBorder) panel.borderColor = info.Ally ? UI51Tokens.GoldA(0.6f) : UI51Tokens.WithAlpha(UI51Tokens.TeamBlue, 0.5f);
            avatar.SetFrame(info.Ally ? FrameStyle.Oro : FrameStyle.Blu);
            if (info.Portrait != portrait) avatar.SetAvatar(portrait = info.Portrait); // SetAvatar rifa' il ritaglio: solo se cambia
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
