using TMPro;
using UnityEngine;

namespace Project51.UI51
{
    /// <summary>Scrive la versione dell'app e il protocollo multiplayer (es. "v1.0.0 p3"): due telefoni si incontrano solo con lo stesso protocollo.</summary>
    [AddComponentMenu("UI51/Version Label")]
    [RequireComponent(typeof(TMP_Text))]
    public class UI51VersionLabel : MonoBehaviour
    {
        void OnEnable() => GetComponent<TMP_Text>().text = "v" + Application.version + " " + Project51.Auth.PhotonAuthConnector.AppVersion;
    }
}
