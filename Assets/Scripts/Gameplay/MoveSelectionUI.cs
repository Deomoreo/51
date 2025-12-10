using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Project51.Unity
{
    /// <summary>
    /// UI for displaying move selection options when multiple moves are available.
    /// Uses TextMeshPro for text display.
    /// </summary>
    public class MoveSelectionUI : MonoBehaviour
    {
        [Header("Container")]
        [SerializeField] private RectTransform container;
        [SerializeField] private GameObject buttonPrefab;
        
        [Header("Text (TextMeshPro)")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        
        [Header("Colors")]
        [SerializeField] private Color invalidMessageColor = new Color(1f, 0.3f, 0.3f);

        private List<GameObject> buttons = new List<GameObject>();

        private void Awake()
        {
            // Ensure there's an EventSystem so UI buttons can receive clicks
            if (EventSystem.current == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
                Debug.Log("MoveSelectionUI: Created EventSystem at runtime.");
            }

            // Ensure container's Canvas has a GraphicRaycaster so buttons are interactive
            if (container != null)
            {
                var canvas = container.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    if (canvas.GetComponent<GraphicRaycaster>() == null)
                    {
                        canvas.gameObject.AddComponent<GraphicRaycaster>();
                        Debug.Log("MoveSelectionUI: Added GraphicRaycaster to Canvas.");
                    }
                }
            }

            // Validate button prefab
            if (buttonPrefab == null)
            {
                Debug.LogWarning("MoveSelectionUI: buttonPrefab is not assigned in inspector.");
            }
            else
            {
                var b = buttonPrefab.GetComponent<Button>();
                if (b == null)
                {
                    Debug.LogWarning("MoveSelectionUI: buttonPrefab does not contain a Button component.");
                }
            }
        }

        public void ShowMoves(List<string> moveDescriptions, System.Action<int> onChoose, bool autoHideOnChoose = true, System.Action<int> onHover = null, List<Sprite> icons = null)
        {
            Clear();
            if (moveDescriptions == null || moveDescriptions.Count == 0) return;

            if (titleText != null)
            {
                titleText.gameObject.SetActive(true);
                titleText.text = moveDescriptions.Count == 1 ? "Scegli mossa" : "Più mosse disponibili";
            }

            for (int i = 0; i < moveDescriptions.Count; i++)
            {
                var desc = moveDescriptions[i];
                var index = i;
                var btnObj = Instantiate(buttonPrefab, container);
                var btn = btnObj.GetComponent<Button>();
                
                // Try to find TMP_Text first, fallback to legacy Text
                var tmpText = btnObj.GetComponentInChildren<TMP_Text>();
                if (tmpText != null)
                {
                    tmpText.text = desc;
                }
                else
                {
                    var legacyText = btnObj.GetComponentInChildren<Text>();
                    if (legacyText != null) legacyText.text = desc;
                }
                
                btn.onClick.AddListener(() => 
                { 
                    Debug.Log($"MoveSelectionUI: button {index} clicked"); 
                    onChoose?.Invoke(index); 
                    if (autoHideOnChoose) Hide(); 
                });
                
                // Add hover callbacks if requested
                if (onHover != null)
                {
                    var trigger = btnObj.GetComponent<EventTrigger>();
                    if (trigger == null) trigger = btnObj.AddComponent<EventTrigger>();

                    var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                    entryEnter.callback.AddListener((ev) => { onHover(index); });
                    trigger.triggers.Add(entryEnter);

                    var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                    entryExit.callback.AddListener((ev) => { onHover(-1); });
                    trigger.triggers.Add(entryExit);
                }

                // Set icon if provided and prefab contains an Image named "Icon"
                if (icons != null && i < icons.Count && icons[i] != null)
                {
                    var img = btnObj.transform.Find("Icon")?.GetComponent<Image>();
                    if (img != null) img.sprite = icons[i];
                }
                
                Debug.Log($"MoveSelectionUI: created button #{index} '{desc}'");
                buttons.Add(btnObj);
            }
            gameObject.SetActive(true);
        }

        public void ShowInvalid(string message, float duration = 1.5f)
        {
            // Avoid starting coroutines on inactive GameObject
            if (!gameObject.activeInHierarchy)
            {
                Debug.Log(message);
                return;
            }

            if (messageText != null)
            {
                messageText.gameObject.SetActive(true);
                messageText.text = message;
                messageText.color = invalidMessageColor;
                StopAllCoroutines();
                StartCoroutine(HideMessageAfterDelay(duration));
            }
            else
            {
                Debug.LogWarning("No messageText assigned to MoveSelectionUI to show invalid selection.");
            }
        }

        private System.Collections.IEnumerator HideMessageAfterDelay(float t)
        {
            yield return new WaitForSeconds(t);
            if (messageText != null) messageText.gameObject.SetActive(false);
        }

        public void Hide()
        {
            Clear();
            if (titleText != null) titleText.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        private void Clear()
        {
            foreach (var b in buttons) Destroy(b);
            buttons.Clear();
        }
    }
}
