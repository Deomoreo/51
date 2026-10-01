using Project51.Auth;
using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Una notizia della pagina Notizie (mockup Notizie): scheda del carosello, riga dell'elenco o testata dell'articolo.
    /// Ognuna usa solo i campi che ha (vuoti = assenti). Colori, sfondi e icone per etichetta li passa UI51NewsView.
    /// Costruita da UI51SocialBuilder.
    /// </summary>
    public sealed class UI51NewsItem : MonoBehaviour
    {
        public Button button;
        [Tooltip("Sfondi del mockup: 0 foto della Home (art-2v2), 1 torneo, 2 dorso.")]
        public GameObject[] arts = new GameObject[0];
        public Image pic;
        public UI51Shape tagPill;
        public TMP_Text tagLabel, title, subtitle, date;
        public GameObject newDot;
        public UI51Shape thumb;
        public Image thumbIcon;

        public void Bind(NewsStory story, string dateText, bool isNew, UI51NewsView.TagStyle style)
        {
            for (int i = 0; i < arts.Length; i++) if (arts[i] != null) arts[i].SetActive(i == style.Art);
            if (pic != null)
            {
                pic.gameObject.SetActive(style.Pic != null);
                if (style.Pic != null)
                {
                    // Mockup: right/top/width in px, altezza dalla proporzione, ruotata attorno al centro.
                    var rt = pic.rectTransform;
                    float w = style.PicWidth, h = w * style.Pic.rect.height / style.Pic.rect.width;
                    pic.sprite = style.Pic;
                    rt.sizeDelta = new Vector2(w, h);
                    rt.anchoredPosition = new Vector2(-style.PicRight - w * 0.5f, -style.PicTop - h * 0.5f);
                    rt.localRotation = Quaternion.Euler(0f, 0f, -style.PicRotation);
                }
            }
            if (tagPill != null) tagPill.fill = UI51Shape.Solid(style.TagBackground);
            if (tagLabel != null) { tagLabel.text = story.Tag; tagLabel.color = style.TagText; }
            if (title != null) title.text = story.Title;
            if (subtitle != null) subtitle.text = story.Subtitle ?? string.Empty;
            if (date != null) date.text = dateText;
            if (newDot != null) newDot.SetActive(isNew);
            if (thumb != null) thumb.fill = style.ThumbFill;
            if (thumbIcon != null)
            {
                thumbIcon.sprite = style.Icon;
                thumbIcon.rectTransform.sizeDelta = new Vector2(style.IconWidth, style.IconWidth);
            }
            // Le pillole si allargano col testo (ContentSizeFitter): ricalcolo subito, non al frame dopo.
            if (tagPill != null) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)tagPill.transform.parent);
        }
    }
}
