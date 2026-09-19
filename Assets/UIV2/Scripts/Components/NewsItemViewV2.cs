using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Scheda di una novita' (mockup 25): riquadro con icona, etichetta NUOVO, titolo, testo e data.
    /// Le novita' non ancora viste hanno il bordo oro e l'etichetta; le altre bordo blu e il testo sale
    /// al posto dell'etichetta. Il testo va a capo su due righe al massimo e la data lo segue.
    /// Grafica costruita da Tools/UIV2/Build News Screen.
    /// </summary>
    public sealed class NewsItemViewV2 : MonoBehaviour
    {
        public Image Border;
        public RectTransform Fill;
        public GameObject Badge;
        public RectTransform TextBlock;
        public TMP_Text Title;
        public TMP_Text Body;
        public TMP_Text Time;

        public Color NewBorderColor = new Color32(232, 178, 74, 255);
        public Color SeenBorderColor = new Color32(70, 102, 142, 255);
        public float NewBorderThickness = 3f;
        public float SeenBorderThickness = 2f;
        [Tooltip("Di quanto sale il testo quando manca l'etichetta NUOVO (altezza etichetta + spazio).")]
        public float BadgeShift = 55f;
        [Tooltip("Spazio fra la fine del testo e la data.")]
        public float TimeGap = 2f;

        private float textBlockY;
        private float timeY;
        private bool captured;

        public void Bind(string title, string body, string time, bool isNew)
        {
            if (!captured)
            {
                captured = true;
                textBlockY = TextBlock.anchoredPosition.y;
                timeY = Time.rectTransform.anchoredPosition.y;
            }

            Title.text = title;
            Body.text = body;
            Time.text = time;

            Badge.SetActive(isNew);
            Border.color = isNew ? NewBorderColor : SeenBorderColor;
            float thickness = isNew ? NewBorderThickness : SeenBorderThickness;
            Fill.offsetMin = new Vector2(thickness, thickness);
            Fill.offsetMax = new Vector2(-thickness, -thickness);
            TextBlock.anchoredPosition = new Vector2(TextBlock.anchoredPosition.x, textBlockY + (isNew ? 0f : BadgeShift));

            // Testo su una riga: la data sta dove nel mockup; su due righe scende di una riga.
            Body.ForceMeshUpdate();
            var face = Body.font.faceInfo;
            float lineHeight = Body.fontSize * face.lineHeight / face.pointSize;
            bool twoLines = !string.IsNullOrEmpty(body) && Body.textInfo.lineCount > 1;
            bool noBody = string.IsNullOrEmpty(body);
            float y = timeY - (twoLines ? lineHeight + TimeGap : 0f) + (noBody ? lineHeight : 0f);
            Time.rectTransform.anchoredPosition = new Vector2(Time.rectTransform.anchoredPosition.x, y);
        }
    }
}
