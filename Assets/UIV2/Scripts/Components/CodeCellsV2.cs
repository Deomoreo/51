using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Le 5 caselle del codice stanza: casella piena = bordo oro e lettera, vuota = bordo blu
    /// e trattino in basso (mockup screen_3_entra_codice / 03_crea_stanza_con_bot).
    /// </summary>
    public sealed class CodeCellsV2 : MonoBehaviour
    {
        public TMP_Text[] Letters;
        public Image[] Borders;
        public GameObject[] Underlines;
        public Color FilledBorder = new Color32(232, 176, 64, 255);
        public Color EmptyBorder = new Color32(70, 96, 130, 255);

        public void SetCode(string code)
        {
            code = code ?? string.Empty;
            for (int i = 0; i < Letters.Length; i++)
            {
                bool filled = i < code.Length;
                Letters[i].text = filled ? code[i].ToString() : string.Empty;
                if (i < Borders.Length && Borders[i] != null) Borders[i].color = filled ? FilledBorder : EmptyBorder;
                if (i < Underlines.Length && Underlines[i] != null) Underlines[i].SetActive(!filled);
            }
        }
    }
}
