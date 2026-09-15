using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Bottone "azione non ancora collegata" generico: logga un messaggio invece di finger
    /// di fare qualcosa (comprare, condividere, ecc.) quando dietro non esiste ancora un
    /// sistema reale (negozio/economia, share nativo...). Riusabile su qualunque pagina/
    /// pannello per evitare N controller quasi identici che fanno solo Debug.Log.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class StubActionButton : MonoBehaviour
    {
        [SerializeField] private string logMessage = "Azione non ancora collegata.";

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => Debug.Log("[StubActionButton] " + logMessage));
        }
    }
}
