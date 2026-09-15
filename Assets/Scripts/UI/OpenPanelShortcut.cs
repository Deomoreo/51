using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Bottone generico che apre un pannello overlay gia' esistente altrove (es. il gear
    /// icon sulla pagina Profilo che apre lo stesso ImpostazioniOverlay della RightRail di
    /// Home, invece di duplicare il pannello). Trova il controller a runtime via
    /// FindObjectOfType (stesso pattern gia' usato per collegare i bottoni RightRail ai loro
    /// pannelli) cosi' funziona indipendentemente da chi costruisce la scena per primo.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class OpenPanelShortcut : MonoBehaviour
    {
        [SerializeField] private string targetControllerTypeName;

        private void Awake()
        {
            MonoBehaviour target = targetControllerTypeName switch
            {
                nameof(PanelImpostazioniController) => FindObjectOfType<PanelImpostazioniController>(true),
                nameof(PanelAmiciController) => FindObjectOfType<PanelAmiciController>(true),
                nameof(PanelPremiController) => FindObjectOfType<PanelPremiController>(true),
                nameof(PanelPostaController) => FindObjectOfType<PanelPostaController>(true),
                nameof(PanelClassificaController) => FindObjectOfType<PanelClassificaController>(true),
                _ => null,
            };

            if (target == null)
            {
                Debug.LogWarning($"[OpenPanelShortcut] Nessun controller '{targetControllerTypeName}' trovato in scena.", this);
                return;
            }

            var openMethod = target.GetType().GetMethod("Open");
            var button = GetComponent<Button>();
            button.onClick.AddListener(() => openMethod.Invoke(target, null));
        }
    }
}
