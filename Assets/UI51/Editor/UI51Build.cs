using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project51.UI51.EditorTools
{
    /// <summary>
    /// Helper comuni dei builder UI51 (Fase 1): cartelle, figli create-or-reuse per nome, RectTransform,
    /// testi TMP con i token, UI51Shape, Image, sprite e modifica idempotente di un prefab.
    /// Tutto idempotente: rieseguire un builder ricostruisce lo stesso risultato sugli oggetti esistenti.
    /// </summary>
    public static class UI51Build
    {
        public const string Root = "Assets/UI51";
        public const string ArtRoot = Root + "/Art";
        public const string PrefabRoot = Root + "/Prefabs";
        public const int UILayer = 5;

        // --- Cartelle e asset

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Sprite da Assets/UI51/Art/&lt;area&gt;/&lt;nome&gt;.png; null (con errore in console) se manca.</summary>
        public static Sprite Sprite(string area, string name)
        {
            string path = $"{ArtRoot}/{area}/{name}.png";
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogError($"[UI51] Sprite mancante o non importato come Sprite: {path}");
            return s;
        }

        // --- Gerarchia

        /// <summary>Figlio con quel nome (riusato se esiste, altrimenti creato), layer UI, messo in coda.</summary>
        public static RectTransform Child(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
            }
            else go = t.gameObject;
            go.layer = UILayer;
            go.SetActive(true);
            go.transform.SetAsLastSibling();
            var rt = (RectTransform)go.transform;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            return rt;
        }

        public static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        public static T GetOrAdd<T>(Component c) where T : Component => GetOrAdd<T>(c.gameObject);

        /// <summary>Rimuove il componente se presente (serve quando un nodo cambia tipo tra due versioni del builder).</summary>
        public static void Remove<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c, true);
        }

        // --- RectTransform

        /// <summary>Riempie il genitore con margini (sinistra, basso, destra, alto).</summary>
        public static RectTransform Stretch(RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Ancora e pivot nello stesso punto, dimensione e posizione fisse.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            return rt;
        }

        public static RectTransform Center(RectTransform rt, float w, float h) =>
            Place(rt, new Vector2(0.5f, 0.5f), new Vector2(w, h), Vector2.zero);

        /// <summary>Dimensione fissa per i figli di un layout group che non controlla le dimensioni.</summary>
        public static RectTransform Size(RectTransform rt, float w, float h)
        {
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static LayoutElement Layout(Component c, float preferredW = -1f, float preferredH = -1f,
            float flexibleW = -1f, float minW = -1f, bool ignore = false)
        {
            var le = GetOrAdd<LayoutElement>(c);
            le.preferredWidth = preferredW;
            le.preferredHeight = preferredH;
            le.flexibleWidth = flexibleW;
            le.minWidth = minW;
            le.ignoreLayout = ignore;
            return le;
        }

        public static HorizontalLayoutGroup Row(Component c, float spacing, RectOffset padding,
            TextAnchor align = TextAnchor.MiddleLeft, bool controlWidth = true, bool controlHeight = false)
        {
            var g = GetOrAdd<HorizontalLayoutGroup>(c);
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset();
            g.childAlignment = align;
            g.childControlWidth = controlWidth;
            g.childControlHeight = controlHeight;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = false;
            g.childScaleWidth = g.childScaleHeight = false;
            return g;
        }

        public static VerticalLayoutGroup Column(Component c, float spacing, RectOffset padding,
            TextAnchor align = TextAnchor.UpperCenter, bool controlWidth = true, bool controlHeight = true)
        {
            var g = GetOrAdd<VerticalLayoutGroup>(c);
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset();
            g.childAlignment = align;
            g.childControlWidth = controlWidth;
            g.childControlHeight = controlHeight;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = false;
            g.childScaleWidth = g.childScaleHeight = false;
            return g;
        }

        /// <summary>RectOffset in ordine CSS (alto, destra, basso, sinistra).</summary>
        public static RectOffset Pad(int top, int right, int bottom, int left) => new RectOffset(left, right, top, bottom);

        public static ContentSizeFitter Fit(Component c, bool width, bool height)
        {
            var f = GetOrAdd<ContentSizeFitter>(c);
            f.horizontalFit = width ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            f.verticalFit = height ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            return f;
        }

        // --- Grafica

        /// <summary>Testo TMP con i token (font, corpo px, colore, letter-spacing px). Mai bersaglio dei raycast.</summary>
        public static TextMeshProUGUI Text(RectTransform rt, string text, FontFace face, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Left, float letterSpacingPx = 0f)
        {
            var t = GetOrAdd<TextMeshProUGUI>(rt);
            UI51Tokens.Style(t, face, size, color, letterSpacingPx);
            t.text = text;
            t.alignment = align;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            t.margin = Vector4.zero;
            return t;
        }

        /// <summary>UI51Shape completo (un solo rebuild). Nessun raycast salvo richiesta.</summary>
        public static UI51Shape Shape(RectTransform rt, Gradient fill, float angle, Vector4 radii, float borderWidth,
            Color borderColor, bool raycast = false, params UI51Shadow[] shadows)
        {
            var s = GetOrAdd<UI51Shape>(rt);
            s.Set(fill, angle, radii, borderWidth, borderColor, shadows);
            s.color = Color.white;
            s.raycastTarget = raycast;
            return s;
        }

        public static UI51Shape Solid(RectTransform rt, Color color, float radius, float borderWidth = 0f,
            Color borderColor = default, bool raycast = false, params UI51Shadow[] shadows) =>
            Shape(rt, UI51Shape.Solid(color), 180f, UI51Tokens.Radii(radius), borderWidth, borderColor, raycast, shadows);

        public static Image Image(RectTransform rt, Sprite sprite, Color color, bool raycast = false, bool preserveAspect = true)
        {
            var img = GetOrAdd<Image>(rt);
            img.sprite = sprite;
            img.color = color;
            img.type = UnityEngine.UI.Image.Type.Simple;
            img.preserveAspect = preserveAspect;
            img.raycastTarget = raycast;
            return img;
        }

        /// <summary>Button senza transizione di colore (il feedback lo danno UI51Press o il componente).</summary>
        public static Button Button(Component c, Graphic target)
        {
            var b = GetOrAdd<Button>(c);
            b.transition = Selectable.Transition.None;
            b.targetGraphic = target;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            return b;
        }

        // --- Campi serializzati privati (m_*)

        public static void Wire(Component target, Action<SerializedObject> assign)
        {
            var so = new SerializedObject(target);
            so.Update();
            assign(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Ref(SerializedObject so, string field, UnityEngine.Object value)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[UI51] Campo {field} non trovato su {so.targetObject.GetType().Name}."); return; }
            p.objectReferenceValue = value;
        }

        public static void Int(SerializedObject so, string field, int value)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[UI51] Campo {field} non trovato su {so.targetObject.GetType().Name}."); return; }
            p.intValue = value;
        }

        public static void Float(SerializedObject so, string field, float value)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[UI51] Campo {field} non trovato su {so.targetObject.GetType().Name}."); return; }
            p.floatValue = value;
        }

        public static void Bool(SerializedObject so, string field, bool value)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[UI51] Campo {field} non trovato su {so.targetObject.GetType().Name}."); return; }
            p.boolValue = value;
        }

        // --- Prefab

        /// <summary>
        /// Apre il prefab (o ne crea la radice in una scena di anteprima se non esiste), lo passa a build e lo salva.
        /// La radice parte attiva: se build la spegne, il prefab resta salvato spento.
        /// </summary>
        public static GameObject EditPrefab(string path, Action<GameObject> build)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            Scene preview = default;
            GameObject root = null;
            try
            {
                if (exists) root = PrefabUtility.LoadPrefabContents(path);
                else
                {
                    preview = EditorSceneManager.NewPreviewScene();
                    root = new GameObject(Path.GetFileNameWithoutExtension(path), typeof(RectTransform));
                    SceneManager.MoveGameObjectToScene(root, preview);
                }
                root.layer = UILayer;
                root.SetActive(true);
                build(root);
                var saved = PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
                if (!ok) Debug.LogError($"[UI51] Salvataggio fallito: {path}");
                else Debug.Log($"[UI51] {(exists ? "Aggiornato" : "Creato")} {path}");
                return saved;
            }
            finally
            {
                if (root != null)
                {
                    if (exists) PrefabUtility.UnloadPrefabContents(root);
                    else UnityEngine.Object.DestroyImmediate(root);
                }
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        // --- Condivisi dai builder

        const string Tag = "[UI51]";

        /// <summary>Unita' del mockup in px della scena (1080 / 390): scala dei contenitori disegnati coi numeri del mockup.</summary>
        public const float Unit = 1080f / 390f;

        public static void SetArray(SerializedObject so, string field, params UnityEngine.Object[] values)
        {
            var p = so.FindProperty(field);
            if (p == null || !p.isArray) { Debug.LogError($"{Tag} Campo array {field} non trovato su {so.targetObject.GetType().Name}."); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        /// <summary>Centro in px del mockup (x da sinistra, y dall'alto), ancorato in alto al centro.</summary>
        public static RectTransform CenterAt(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x - 195f, -y);
            return rt;
        }

        /// <summary>Fascia larga quanto il genitore meno i margini, a top px dall'alto, alta h.</summary>
        public static RectTransform TopBand(RectTransform rt, float left, float right, float top, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -top - h);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Colonna a tutta larghezza, altezza dai figli.</summary>
        public static void Stack(RectTransform rt, float spacing, RectOffset padding = null) =>
            Column(rt, spacing, padding, TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;

        public static void Gap(Transform parent, string name, float height) =>
            Layout(Child(parent, name), -1f, height);

        public static TextMeshProUGUI NoWrap(TextMeshProUGUI t)
        {
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        /// <summary>Una riga sola, con i puntini se non ci sta.</summary>
        public static TextMeshProUGUI Clip(TextMeshProUGUI t)
        {
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        public static void Wrap(TextMeshProUGUI t, float lineSpacing)
        {
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Overflow;
            t.lineSpacing = lineSpacing;
        }

        /// <summary>Tratto UI51Polyline nel viewBox dato (coordinate SVG), senza raycast.</summary>
        public static UI51Polyline Polyline(RectTransform rt, Vector2 viewBox, float width, Color color, params Vector2[] points)
        {
            var line = GetOrAdd<UI51Polyline>(rt);
            line.Set(viewBox, width, points);
            line.color = color;
            line.raycastTarget = false;
            return line;
        }

        // --- Scena

        public static void HideChild(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) t.gameObject.SetActive(false);
        }

        public static GameObject PanelRef(SerializedObject authSo, string field, Scene scene, string fallbackName)
        {
            var p = authSo.FindProperty(field);
            if (p != null && p.objectReferenceValue is GameObject go) return go;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == fallbackName) return t.gameObject;
            return null;
        }

        public static Transform FindPath(Scene scene, string parentName, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name && t.parent != null && t.parent.name == parentName) return t;
            return null;
        }

        /// <summary>Vero (con errore in console) se una scena aperta ha modifiche non salvate: il builder non la tocca.</summary>
        public static bool HasDirtyScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.isDirty) continue;
                Debug.LogError($"{Tag} La scena aperta '{(string.IsNullOrEmpty(s.path) ? s.name : s.path)}' ha modifiche non salvate: " +
                               "salvala o scartala e riesegui. Non la tocco.");
                return true;
            }
            return false;
        }
    }
}
