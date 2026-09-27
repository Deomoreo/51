using TMPro;
using UnityEngine;

namespace Project51.UI51
{
    /// <summary>Scrive la versione reale dell'app (bundleVersion) nel testo, es. "v2.26".</summary>
    [AddComponentMenu("UI51/Version Label")]
    [RequireComponent(typeof(TMP_Text))]
    public class UI51VersionLabel : MonoBehaviour
    {
        void OnEnable() => GetComponent<TMP_Text>().text = "v" + Application.version;
    }
}
