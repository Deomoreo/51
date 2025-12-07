using UnityEngine;
using UnityEngine.UI;
// (no direct Core or Networking references to keep UI assembly decoupled)

namespace Project51.Unity.UI
{
    /// <summary>
    /// Simple UI indicator showing whose turn it is. Works with TextMeshProUGUI if available, else falls back to Text.
    /// </summary>
    public class TurnIndicator : MonoBehaviour
    {
        private object tmpLabel; // if TextMeshProUGUI exists
        private Text uiTextLabel; // fallback
        private MonoBehaviour turnController;

        private void Awake()
        {
            // Try get TMP label via reflection
            var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpType != null)
            {
                var comp = GetComponent(tmpType);
                if (comp == null)
                {
                    comp = gameObject.AddComponent(tmpType);
                }
                tmpLabel = comp;
                // set alignment center, color white, font size
                var alignProp = tmpType.GetProperty("alignment");
                var colorProp = tmpType.GetProperty("color");
                var fontSizeProp = tmpType.GetProperty("fontSize");
                // TextAlignmentOptions.Center enum value = 514 per TMP, but use fallback of setting alignment if available
                var tao = System.Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
                object centerVal = null;
                if (tao != null)
                {
                    centerVal = System.Enum.Parse(tao, "Center");
                }
                alignProp?.SetValue(tmpLabel, centerVal);
                colorProp?.SetValue(tmpLabel, Color.white);
                fontSizeProp?.SetValue(tmpLabel, 36f);
            }
            else
            {
                uiTextLabel = GetComponent<Text>();
                if (uiTextLabel == null)
                {
                    uiTextLabel = gameObject.AddComponent<Text>();
                    uiTextLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    uiTextLabel.alignment = TextAnchor.MiddleCenter;
                    uiTextLabel.color = Color.white;
                    uiTextLabel.fontSize = 24;
                }
            }
        }

        private void Start()
        {
            // Find TurnController via reflection to avoid hard assembly reference
            var tcType = System.Type.GetType("Project51.Unity.TurnController, Project51.Gameplay");
            if (tcType == null)
            {
                // Fallback: try in current assembly
                tcType = System.Type.GetType("TurnController");
            }
            if (tcType != null)
            {
                foreach (var mb in FindObjectsOfType<MonoBehaviour>())
                {
                    if (mb != null && tcType.IsAssignableFrom(mb.GetType()))
                    {
                        turnController = mb;
                        break;
                    }
                }
            }
            InvokeRepeating(nameof(UpdateIndicator), 0.2f, 0.2f);
        }

        private void UpdateIndicator()
        {
            if (tmpLabel == null && uiTextLabel == null) return;
            // Access GameState via reflection
            if (turnController == null)
            {
                SetText("");
                return;
            }
            var gameStateProp = turnController.GetType().GetProperty("GameState");
            var state = gameStateProp?.GetValue(turnController);
            if (state == null)
            {
                SetText("");
                return;
            }
            int current = 0;
            // Reflect CurrentPlayerIndex
            var cpiProp = state.GetType().GetProperty("CurrentPlayerIndex");
            current = cpiProp != null ? (int)cpiProp.GetValue(state) : current;

            // Resolve display name
            string displayName = $"Player {current}";

            // Try to use RoomManager nicknames in multiplayer
            var gmType = System.Type.GetType("Project51.Unity.GameManager, Project51.Networking");
            bool isMultiplayer = false;
            if (gmType != null)
            {
                var isMpProp = gmType.GetProperty("IsMultiplayerSafe", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (isMpProp != null)
                {
                    var val = isMpProp.GetValue(null);
                    if (val is bool b) isMultiplayer = b;
                }
                if (isMultiplayer)
                {
                    var rmType = System.Type.GetType("Project51.Networking.RoomManager, Project51.Networking");
                    var rmInstanceProp = rmType?.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var rm = rmInstanceProp?.GetValue(null);
                    if (rm != null)
                    {
                        var slotsProp = rmType.GetProperty("PlayerSlots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        var slots = slotsProp?.GetValue(rm) as System.Array;
                        if (slots != null && current >= 0 && current < slots.Length)
                        {
                            var slot = slots.GetValue(current);
                            var nickProp = slot.GetType().GetProperty("NickName");
                            var isHumanProp = slot.GetType().GetProperty("IsHuman");
                            var isBotProp = slot.GetType().GetProperty("IsBot");
                            string nick = nickProp != null ? (nickProp.GetValue(slot) as string) : null;
                            bool isHuman = isHumanProp != null && (bool)isHumanProp.GetValue(slot);
                            bool isBot = isBotProp != null && (bool)isBotProp.GetValue(slot);
                            if (!string.IsNullOrEmpty(nick))
                            {
                                displayName = nick;
                            }
                            else if (isBot)
                            {
                                displayName = $"Bot {current - 1 + 1}"; // simple bot label
                            }
                        }
                    }
                }
            }

            // Check if local player's turn
            bool isLocalTurn = false;
            if (gmType != null)
            {
                var instanceProp = gmType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var gm = instanceProp?.GetValue(null);
                if (gm != null)
                {
                    var isLocalTurnMethod = gmType.GetMethod("IsLocalPlayerTurn", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (isLocalTurnMethod != null)
                    {
                        isLocalTurn = (bool)isLocalTurnMethod.Invoke(gm, null);
                    }
                }
            }

            SetText(isLocalTurn ? $"Turn: {displayName} (YOU)" : $"Turn: {displayName}");
        }

        private void SetText(string text)
        {
            if (tmpLabel != null)
            {
                var prop = tmpLabel.GetType().GetProperty("text");
                prop?.SetValue(tmpLabel, text);
            }
            else if (uiTextLabel != null)
            {
                uiTextLabel.text = text;
            }
        }
    }
}
