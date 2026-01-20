using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class HierarchyDumper : MonoBehaviour
{
    [Header("What to dump")]
    [Tooltip("If set, dumps only this subtree. If null, dumps all root objects in the active scene.")]
    [SerializeField] private Transform rootOverride;

    [SerializeField] private bool includeInactive = true;
    [SerializeField] private bool includeComponents = true;

    [Header("UI details")]
    [SerializeField] private bool includeRectTransformDetails = true;

    [Header("Limits")]
    [Min(0)]
    [SerializeField] private int maxDepth = 64;

    [Header("Output")]
    [SerializeField] private bool logToConsole = true;
    [SerializeField] private bool writeToFile = true;
    [SerializeField] private string fileNamePrefix = "hierarchy_dump";

    [ContextMenu("Dump Hierarchy (Scene or RootOverride)")]
    public void DumpHierarchy()
    {
        var sb = new StringBuilder(64 * 1024);

        sb.AppendLine($"=== Hierarchy Dump ===");
        sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Scene: {SceneManager.GetActiveScene().name}");
        sb.AppendLine($"Screen: {Screen.width}x{Screen.height}");
        sb.AppendLine($"SafeArea: {Screen.safeArea}");
        sb.AppendLine($"Screen.currentResolution: {Screen.currentResolution.width}x{Screen.currentResolution.height} @{Screen.currentResolution.refreshRate}Hz");
        sb.AppendLine($"Display.main.system: {Display.main.systemWidth}x{Display.main.systemHeight}");
        sb.AppendLine($"Display.main.rendering: {Display.main.renderingWidth}x{Display.main.renderingHeight}");
        sb.AppendLine();

        if (rootOverride != null)
        {
            DumpGameObject(rootOverride.gameObject, 0, sb);
        }
        else
        {
            // Dump all scene root objects
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                DumpGameObject(roots[i], 0, sb);
                if (i < roots.Length - 1) sb.AppendLine();
            }
        }

        string text = sb.ToString();

        if (logToConsole)
            Debug.Log(text, this);

        if (writeToFile)
            WriteToPersistentFile(text);

#if UNITY_EDITOR
        EditorGUIUtility.systemCopyBuffer = text;
        Debug.Log("[HierarchyDumper] Dump copied to clipboard.", this);
#endif
    }

    private void DumpGameObject(GameObject go, int depth, StringBuilder sb)
    {
        if (go == null) return;
        if (depth > maxDepth) return;

        if (!includeInactive && !go.activeInHierarchy)
            return;

        string indent = new string(' ', depth * 2);

        sb.Append(indent);
        sb.Append("- ");
        sb.Append(go.name);
        sb.Append($"  (activeSelf={go.activeSelf}, activeInHierarchy={go.activeInHierarchy}, layer={go.layer}, tag={go.tag})");
        sb.AppendLine();

        if (includeComponents)
        {
            var comps = go.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                var c = comps[i];
                sb.Append(indent).Append("  • ");

                if (c == null)
                {
                    sb.AppendLine("<Missing Script>");
                    continue;
                }

                Type t = c.GetType();
                sb.Append(t.Name);

                if (c is Behaviour b)
                    sb.Append($" (enabled={b.enabled})");

                sb.AppendLine();

                if (includeRectTransformDetails && c is RectTransform rt)
                    AppendRectTransform(indent + "    ", rt, sb);

                if (c is Canvas canvas)
                    AppendCanvasInfo(indent + "    ", canvas, sb);
            }
        }

        // Children
        var tr = go.transform;
        for (int i = 0; i < tr.childCount; i++)
        {
            var child = tr.GetChild(i);
            DumpGameObject(child.gameObject, depth + 1, sb);
        }
    }
    private static void AppendCanvasInfo(string indent, Canvas canvas, StringBuilder sb)
    {
        sb.Append(indent).AppendLine("[Canvas]");
        sb.Append(indent).AppendLine($"renderMode={canvas.renderMode} sortingOrder={canvas.sortingOrder} overrideSorting={canvas.overrideSorting}");
        sb.Append(indent).AppendLine($"scaleFactor={canvas.scaleFactor:F3} pixelPerfect={canvas.pixelPerfect}");

        // IMPORTANTISSIMO: se qualcuno scala il canvas via Transform lo vedi qui
        sb.Append(indent).AppendLine($"transform.lossyScale={canvas.transform.lossyScale}");

        // Se c’è un CanvasScaler sullo stesso GO, stampalo (spesso è la causa dei mismatch)
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            sb.Append(indent).AppendLine("[CanvasScaler]");
            sb.Append(indent).AppendLine($"uiScaleMode={scaler.uiScaleMode} referenceResolution={scaler.referenceResolution} matchWidthOrHeight={scaler.matchWidthOrHeight:F3}");
            sb.Append(indent).AppendLine($"scaleFactor={scaler.scaleFactor:F3} referencePixelsPerUnit={scaler.referencePixelsPerUnit:F3}");
            sb.Append(indent).AppendLine($"screenMatchMode={scaler.screenMatchMode}");
        }
    }


    private static void AppendRectTransform(string indent, RectTransform rt, StringBuilder sb)
    {
        sb.Append(indent).AppendLine("[RectTransform]");
        sb.Append(indent).AppendLine($"anchorMin={rt.anchorMin} anchorMax={rt.anchorMax} pivot={rt.pivot}");
        sb.Append(indent).AppendLine($"anchoredPosition={rt.anchoredPosition} sizeDelta={rt.sizeDelta}");
        sb.Append(indent).AppendLine($"offsetMin={rt.offsetMin} offsetMax={rt.offsetMax}");
        sb.Append(indent).AppendLine($"localScale={rt.localScale} localEulerAngles={rt.localEulerAngles}");
    }

    private void WriteToPersistentFile(string text)
    {
        try
        {
            string file = $"{fileNamePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string path = Path.Combine(Application.persistentDataPath, file);
            File.WriteAllText(path, text, Encoding.UTF8);
            Debug.Log($"[HierarchyDumper] Wrote file: {path}", this);
        }
        catch (Exception e)
        {
            Debug.LogError($"[HierarchyDumper] Failed to write file: {e.Message}", this);
        }
    }
}
