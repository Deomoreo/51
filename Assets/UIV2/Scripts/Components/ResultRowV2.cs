using TMPro;
using UnityEngine;
using Project51.UIV2.Animations;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Riga classifica dei risultati (mockup 12_fine_partita / 13_fine_smazzata): posizione,
    /// medaglione, nome, punti della smazzata, barra verso il traguardo, totale e "/ traguardo".
    /// </summary>
    public sealed class ResultRowV2 : MonoBehaviour
    {
        public TMP_Text Rank;
        public TMP_Text Name;
        public TMP_Text Delta;
        public RectTransform ProgressFill;
        public TMP_Text Score;
        public TMP_Text Target;
        public GameObject WinnerHighlight;
        public GameObject Trophy;
        private int scoreTotal;
        private bool isCappotto;

        public void AnimateScore()
        {
            if (Score == null || isCappotto) return;
            var counter = Score.GetComponent<UIV2NumberCounter>();
            if (counter == null) counter = Score.gameObject.AddComponent<UIV2NumberCounter>();
            counter.Label = Score;
            counter.AnimateFrom(0, scoreTotal);
        }

        public void Bind(int rank, string entryName, string delta, int total, int target, bool cappotto, bool winner)
        {
            gameObject.SetActive(true);
            Rank.text = rank.ToString();
            Name.text = entryName;
            Delta.text = delta;
            scoreTotal = total;
            isCappotto = cappotto;
            var oldCounter = Score.GetComponent<UIV2NumberCounter>();
            if (oldCounter != null) oldCounter.SetValue(total, false);
            Score.text = cappotto ? "Cappotto" : total.ToString();
            Target.text = cappotto ? "" : "/ " + target;
            ProgressFill.anchorMax = new Vector2(cappotto ? 1f : Mathf.Clamp01((float)total / Mathf.Max(1, target)), 1f);
            if (WinnerHighlight != null) WinnerHighlight.SetActive(winner);
            if (Trophy != null) Trophy.SetActive(winner);
        }
    }
}
