using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Project51.UI51.EditorTools
{
    /// <summary>
    /// Fase 1: font asset TMP di Cinzel e Nunito (SPEC §1) in Resources/UI51/Fonts/&lt;Nome&gt; SDF.asset,
    /// il percorso letto da UI51Tokens.Font. SDFAA, campionamento 48, padding 5, atlas 1024 dinamico multi-pagina:
    /// l'insieme garantito (ASCII, accentate italiane, punteggiatura dei mockup) e' pre-caricato qui, il resto
    /// entra a runtime. Idempotente: se l'asset esiste aggiunge solo i caratteri che mancano.
    /// </summary>
    public static class UI51FontBuilder
    {
        const string SourceFolder = UI51Build.Root + "/Fonts";
        const string OutputFolder = UI51Build.Root + "/Resources/UI51/Fonts";
        const int SamplingSize = 48, Padding = 5, AtlasSize = 1024;

        // à á è é ì í î ò ó ù ú, maiuscole, « » ‘ ’ “ ” … – — € · • ° ×
        const string Italian = "àáèéìíîòóùú" +
                               "ÀÁÈÉÌÍÎÒÓÙÚ";
        const string Punctuation = "«»‘’“”…–—€·•°×";

        [MenuItem("Tools/UI51/Font Assets")]
        private static void Menu() => Build();

        public static string Charset
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                for (char c = ' '; c <= '~'; c++) sb.Append(c);
                return sb.Append(Italian).Append(Punctuation).ToString();
            }
        }

        public static void Build()
        {
            UI51Build.EnsureFolder(OutputFolder);
            foreach (FontFace face in System.Enum.GetValues(typeof(FontFace))) BuildFace(face);
            AssetDatabase.SaveAssets();
            Debug.Log("[UI51 Font] Font asset completati.");
        }

        static void BuildFace(FontFace face)
        {
            string name = UI51Tokens.FontName(face);
            string source = $"{SourceFolder}/{name}.ttf";
            var font = AssetDatabase.LoadAssetAtPath<Font>(source);
            if (font == null) { Debug.LogError($"[UI51 Font] Font sorgente mancante: {source}"); return; }

            string path = $"{OutputFolder}/{name} SDF.asset";
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            bool created = asset == null;
            if (created)
            {
                asset = TMP_FontAsset.CreateFontAsset(font, SamplingSize, Padding, GlyphRenderMode.SDFAA,
                    AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
                if (asset == null) { Debug.LogError($"[UI51 Font] Creazione fallita per {source}"); return; }
                asset.name = name + " SDF";
                AssetDatabase.CreateAsset(asset, path);
                asset.material.name = name + " SDF Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }

            asset.TryAddCharacters(Charset, out string missing);
            // Le pagine nuove dell'atlas multi-pagina vanno salvate dentro l'asset.
            var pages = asset.atlasTextures;
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] == null || AssetDatabase.Contains(pages[i])) continue;
                pages[i].name = $"{name} SDF Atlas {i}";
                AssetDatabase.AddObjectToAsset(pages[i], asset);
            }
            EditorUtility.SetDirty(asset);

            Debug.Log($"[UI51 Font] {(created ? "Creato" : "Aggiornato")} {path}");
            if (!string.IsNullOrEmpty(missing?.Trim()))
                Debug.LogWarning($"[UI51 Font] {name}: caratteri assenti nel font (useranno il fallback TMP): {missing}");
        }
    }
}
