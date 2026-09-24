using Project51.UIV2.Animations;
using Project51.UIV2.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Sfondo animato della Home: base statica a riempimento (EnvelopeParent) tra safe area e nav,
    /// bagliore che respira, gemma sull'arco e decorazioni ai bordi con micro-movimenti sfasati
    /// (UIV2AmbientFloat). Il centro resta libero per la Home. Fermo con Grafica ridotta.
    /// Rilanciabile: rimuove e ricrea solo "HomeAmbient".
    /// Posizioni in pixel dell'artwork base a meta' risoluzione (941x1672; il PNG e' 1882x3344,
    /// stesse proporzioni), origine in alto a sinistra, centro della
    /// parte visibile dello sprite; movimenti in unita' canvas (riferimento 1080x1920).
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const string HomeAmbientFolder = "Assets/UI/Sprites/DragonsHoard/sprites_unity/sprites_unity/BackgroundHome/";
        private const string TopFadePath = "Assets/Art/Generated/Home_TopFade.png";
        private const float BaseWidth = 941f, BaseHeight = 1672f;

        private struct AmbientLayer
        {
            public string Name, Sprite;
            public float X, Y, Width, Tilt;           // centro e larghezza visibili sull'artwork, inclinazione fissa
            public Rect Visible;                       // bbox alfa nello sprite (px, origine in alto a sinistra)
            public float MoveX, MoveY, Rotation, Scale, Duration, Phase;
            public bool Flip;                          // specchiato attorno al centro visibile
        }

        private static AmbientLayer Layer(string name, string sprite, float x, float y, float width, float tilt,
            Rect visible, float moveX, float moveY, float rotation, float scale, float duration, float phase, bool flip = false)
        {
            return new AmbientLayer { Name = name, Sprite = sprite, X = x, Y = y, Width = width, Tilt = tilt, Visible = visible,
                MoveX = moveX, MoveY = moveY, Rotation = rotation, Scale = scale, Duration = duration, Phase = phase, Flip = flip };
        }

        // Misurate sul mockup Assets/Mockup (941x1672): nastri avvolti alle colonne, denari e coppa a
        // sinistra, bastone e spada a destra. Il nastro destro specchiato ricalca il sinistro del mockup
        // meglio di home_ribbon_left.
        private static readonly AmbientLayer[] HomeAmbientLayers =
        {
            Layer("Ribbon_Left",  "home_ribbon_right",  96f, 465f, 210f,  0f,   Rect.MinMaxRect(480, 12, 890, 1518),  2f,   6f,  0.6f, 0f,     8.5f, 0f, true),
            Layer("Ribbon_Right", "home_ribbon_right", 826f, 475f, 215f,  0f,   Rect.MinMaxRect(480, 12, 890, 1518), -2f,   7f, -0.6f, 0f,     9.5f, 1.5f),
            Layer("Denari",       "home_denari",       215f, 245f, 138f,  0f,   Rect.MinMaxRect(54, 57, 1197, 1196),  2f,  10f,  1.2f, 0f,     6.8f, 0.6f),
            Layer("Coppe",        "home_coppe",        134f, 561f, 122f, -22f,  Rect.MinMaxRect(234, 93, 1019, 1155), -3f,  8f, -1f,   0f,     7.6f, 2.3f),
            Layer("Bastoni",      "home_bastoni",      775f, 320f, 191f,  51.8f, Rect.MinMaxRect(279, 35, 1068, 1179),  3f, -10f,  1.5f, 0f,     8.4f, 3.1f),
            Layer("Spade",        "home_spade",        800f, 616f, 165f,  21.5f, Rect.MinMaxRect(266, 58, 1132, 1186),  3f,   9f, -1.2f, 0f,     6.4f, 1.1f),
            Layer("Gem",          "home_gem",          470f,  45f,  56f,  0f,   Rect.MinMaxRect(208, 146, 1047, 1084), 0f,   0f,  0f,   0.018f, 4f,   0.8f),
        };

        [MenuItem("Tools/UIV2/Build Home Ambient")]
        public static void BuildHomeAmbient()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Nastri alti ~1100 px su iPhone: sorgente piena (1536); semi fino a ~460 px.
            foreach (var layer in HomeAmbientLayers) ShrinkForUi(layer.Sprite, layer.Sprite.Contains("ribbon") ? 2048 : 512, true);
            ShrinkForUi("home_bg_base", 4096, false); // 1882x3344: a 2048 tornerebbe sotto la risoluzione degli iPhone

            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            var background = FindInScene(scene, "UIV2_Home/BackgroundLayer");
            var old = background.Find("HomeAmbient");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var ambient = CreateUIObject("HomeAmbient", background);
            Stretch(ambient);
            var group = ambient.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            // Fuori dall'artwork (barra di stato, sotto la nav) resta il verde dell'arco.
            var filler = CreateUIObject("Filler", ambient);
            Stretch(filler);
            var fillerImage = filler.gameObject.AddComponent<Image>();
            fillerImage.color = new Color32(12, 52, 31, 255);
            fillerImage.raycastTarget = false;

            // Artwork tra il bordo alto della safe area e la nav: sui telefoni alti un riempimento a
            // tutto schermo tagliava ~84 px per lato e con loro le colonne. 170 = BottomNavHost.
            var region = CreateUIObject("Region", ambient);
            region.gameObject.AddComponent<SafeAreaFitter>();
            var aboveNav = CreateUIObject("AboveNav", region);
            Stretch(aboveNav);
            aboveNav.offsetMin = new Vector2(0f, 170f);

            // Ancorata in alto: sui 9:16 l'eccedenza scende dietro la nav invece di tagliare arco e gemma.
            var frame = CreateUIObject("Frame", aboveNav);
            Stretch(frame);
            frame.pivot = new Vector2(0.5f, 1f);
            var fitter = frame.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = BaseWidth / BaseHeight;

            Stretch(AmbientImage(frame, "BG_Base", "home_bg_base", 1f));
            var glow = AmbientImage(frame, "Glow", "home_glow", 0.06f);
            Stretch(glow);
            Float(glow, 0f, 0f, 0f, 0.03f, 0.05f, 4.2f, 0f);

            // Bordo alto dell'artwork sfumato nel verde della barra di stato (24 px d'artwork: oltre
            // spegne la gemma). Dentro Frame, cosi' segue l'arco anche dove la cornice sborda (iPad).
            var fadeImporter = (TextureImporter)AssetImporter.GetAtPath(TopFadePath);
            if (fadeImporter.textureType != TextureImporterType.Sprite)
            {
                fadeImporter.textureType = TextureImporterType.Sprite;
                fadeImporter.mipmapEnabled = false;
                fadeImporter.wrapMode = TextureWrapMode.Clamp;
                fadeImporter.SaveAndReimport();
            }
            var topFade = CreateUIObject("TopFade", frame);
            topFade.anchorMin = new Vector2(0f, 1f - 24f / BaseHeight);
            topFade.anchorMax = Vector2.one;
            topFade.offsetMin = topFade.offsetMax = Vector2.zero;
            var topFadeImage = topFade.gameObject.AddComponent<Image>();
            topFadeImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TopFadePath);
            topFadeImage.color = fillerImage.color;
            topFadeImage.raycastTarget = false;

            foreach (var layer in HomeAmbientLayers)
            {
                var rect = AmbientImage(frame, layer.Name, layer.Sprite, 1f);
                var size = SourceSize(layer.Sprite);
                float k = layer.Width / layer.Visible.width;
                Vector2 visibleCentre = layer.Visible.center;
                float left = layer.X - visibleCentre.x * k, top = layer.Y - visibleCentre.y * k;
                rect.anchorMin = new Vector2(left / BaseWidth, 1f - (top + size.y * k) / BaseHeight);
                rect.anchorMax = new Vector2((left + size.x * k) / BaseWidth, 1f - top / BaseHeight);
                rect.pivot = new Vector2(visibleCentre.x / size.x, 1f - visibleCentre.y / size.y);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.localEulerAngles = new Vector3(0f, 0f, layer.Tilt);
                if (layer.Flip) rect.localScale = new Vector3(-1f, 1f, 1f);
                Float(rect, layer.MoveX, layer.MoveY, layer.Rotation, layer.Scale, 0f, layer.Duration, layer.Phase);
            }

            var home = Object.FindObjectOfType<HomeV2Integration>(true);
            var so = new SerializedObject(home);
            so.FindProperty("homeAmbient").objectReferenceValue = group;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Sfondo Home animato ricostruito.");
        }

        private static RectTransform AmbientImage(RectTransform parent, string name, string sprite, float alpha)
        {
            var rect = CreateUIObject(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HomeAmbientFolder + sprite + ".png");
            if (image.sprite == null) throw new System.Exception("Sprite mancante: " + sprite);
            image.color = new Color(1f, 1f, 1f, alpha);
            image.raycastTarget = false;
            return rect;
        }

        private static void Float(RectTransform rect, float moveX, float moveY, float rotation, float scale, float alpha, float duration, float phase)
        {
            var so = new SerializedObject(rect.gameObject.AddComponent<UIV2AmbientFloat>());
            so.FindProperty("moveX").floatValue = moveX;
            so.FindProperty("moveY").floatValue = moveY;
            so.FindProperty("rotation").floatValue = rotation;
            so.FindProperty("scaleAmount").floatValue = scale;
            so.FindProperty("alphaAmount").floatValue = alpha;
            so.FindProperty("duration").floatValue = duration;
            so.FindProperty("phase").floatValue = phase;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Il bbox visibile e' misurato sul PNG originale, non sulla texture ridotta.
        private static Vector2 SourceSize(string sprite)
        {
            ((TextureImporter)AssetImporter.GetAtPath(HomeAmbientFolder + sprite + ".png")).GetSourceTextureWidthAndHeight(out int width, out int height);
            return new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        // Sorgenti da 1254 px senza mipmap sfarfallano quando si muovono e sprecano memoria.
        private static void ShrinkForUi(string sprite, int maxSize, bool mipmaps)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(HomeAmbientFolder + sprite + ".png");
            if (importer.maxTextureSize == maxSize && importer.mipmapEnabled == mipmaps) return;
            importer.maxTextureSize = maxSize;
            importer.mipmapEnabled = mipmaps;
            importer.SaveAndReimport();
        }
    }
}
