using Project51.UIV2.Animations;
using Project51.UIV2.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Sfondo animato della Home: base statica a riempimento (EnvelopeParent) dal bordo alto dello
    /// schermo alla nav, livelli d'effetto (stelle, luci del castello, stella cadente, bagliore delle
    /// torce, cespugli) che pulsano (UIV2AmbientFloat) e fiamme che si muovono (shader UIV2/FlameWobble).
    /// Fermo con Grafica ridotta: resta la posa del mockup. Rilanciabile: rimuove e ricrea solo "HomeAmbient".
    /// Posizioni in pixel dell'artwork base a meta' risoluzione (941x1672; il PNG e' 1882x3344,
    /// stesse proporzioni), origine in alto a sinistra; movimenti in unita' canvas (riferimento 1080x1920).
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const string HomeAmbientFolder = "Assets/UI/Sprites/DragonsHoard/sprites_unity/sprites_unity/BackgroundHome/";
        private const string FlameMaterialPath = "Assets/UIV2/Art/Shaders/UIV2FlameWobble.mat";
        private const float BaseWidth = 941f, BaseHeight = 1672f;

        // Livelli 941x1672 allineati 1:1 alla base (o in Place, px d'artwork): alpha di riposo (= mockup)
        // e pulsazione. home_vines escluso: ingombra gli angoli alti sotto l'HUD e ripete l'edera della base.
        private struct OverlayLayer
        {
            public string Name, Sprite;
            public float Alpha, AlphaAmount, MoveX, MoveY, Duration, Phase, Pause;
            public Rect Place;                         // vuoto = a tutta cornice
        }

        private static readonly OverlayLayer[] HomeOverlayLayers =
        {
            new OverlayLayer { Name = "Stars",         Sprite = "home_stars",         Alpha = 0.35f, AlphaAmount = 0.55f, Duration = 1.9f, Phase = 0.4f },
            // Disegnate su un castello piu' grande: scala 0.875 x 0.925 e spostamento (79, 35) misurati
            // sulle finestre della base.
            new OverlayLayer { Name = "CastleLights",  Sprite = "home_castle_lights", Alpha = 0.45f, AlphaAmount = 0.3f,  Duration = 1.3f, Phase = 0.9f,
                               Place = new Rect(79f, 35f, BaseWidth * 0.875f, BaseHeight * 0.925f) },
            // Passa verso il basso a sinistra, come la scia disegnata; invisibile a riposo.
            new OverlayLayer { Name = "ShootingStar",  Sprite = "home_shooting_star", Alpha = 0f,    AlphaAmount = 0.9f,  MoveX = -150f, MoveY = -80f, Duration = 1.2f, Pause = 9f },
        };

        // Primo piano (sopra fiamme e bagliori): i cespugli sfocati agli angoli bassi del mockup.
        private static readonly OverlayLayer Bushes =
            new OverlayLayer { Name = "Bushes", Sprite = "home_bushes", Alpha = 1f, MoveX = 4f, Duration = 3.2f, Phase = 1f };

        // Bagliore delle torce: le due meta' di home_torch_glow, ridotte attorno al centro di ciascuna.
        private const float TorchGlowScale = 0.75f;
        private static readonly Vector2[] TorchGlowCentres = { new Vector2(151f, 688f), new Vector2(796f, 688f) };

        // Fiamme sui bracieri: base della fiamma (X, Y) nel braciere del mockup, larghezza visibile.
        // Una per braciere: il movimento lo fa lo shader, sfasato dalla posizione.
        private struct FlameLayer
        {
            public string Name;
            public float X, Y;
        }

        private const string FlameSprite = "home_flame_a";
        private const float FlameWidth = 44f;
        // 2.20: sfondo, non primo piano: nucleo bianco spento verso l'arancio e un po' di trasparenza.
        private static readonly Color FlameTint = new Color(0.9f, 0.7f, 0.52f, 0.85f);
        private static readonly Rect FlameVisible = Rect.MinMaxRect(286, 60, 1002, 1193); // bbox alfa (px, origine in alto a sinistra)

        private static readonly FlameLayer[] HomeFlameLayers =
        {
            new FlameLayer { Name = "Flame_Left",  X = 137f, Y = 705f },
            new FlameLayer { Name = "Flame_Right", X = 798f, Y = 705f },
        };

        [MenuItem("Tools/UIV2/Build Home Ambient")]
        public static void BuildHomeAmbient()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Fiamme alte ~110 px su iPhone da sorgenti 1254: 256 con mipmap; i livelli 941x1672 restano pieni.
            ShrinkForUi(FlameSprite, 256, true);
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

            // Fuori dall'artwork (barra di stato, sotto la nav) resta il blu del cielo in cima alla base.
            var filler = CreateUIObject("Filler", ambient);
            Stretch(filler);
            var fillerImage = filler.gameObject.AddComponent<Image>();
            fillerImage.color = new Color32(4, 53, 113, 255);
            fillerImage.raycastTarget = false;

            // Artwork dal bordo alto dello schermo (come nel mockup, l'HUD ci sta sopra) al filo alto della
            // nav: fino al fondo dello schermo taglierebbe ~84 px per lato e con loro le colonne.
            // 96 = BottomNavHost 170 - la nav che ci scende dentro di ~71 (sink 0.75 x inset 94), qualche unita' sotto il suo bordo.
            // 2.23: con sink 0.5 la nav sta piu' in alto e copre il fondo dell'artwork; 96 resta per non cambiarne la scala.
            var region = CreateUIObject("Region", ambient);
            var regionFitter = new SerializedObject(region.gameObject.AddComponent<SafeAreaFitter>());
            regionFitter.FindProperty("ignoreTop").boolValue = true;
            regionFitter.ApplyModifiedPropertiesWithoutUndo();
            var aboveNav = CreateUIObject("AboveNav", region);
            Stretch(aboveNav);
            aboveNav.offsetMin = new Vector2(0f, 96f);

            // Ancorata in alto: sui 9:16 l'eccedenza scende dietro la nav invece di tagliare il cielo.
            var frame = CreateUIObject("Frame", aboveNav);
            Stretch(frame);
            frame.pivot = new Vector2(0.5f, 1f);
            var fitter = frame.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = BaseWidth / BaseHeight;

            Stretch(AmbientImage(frame, "BG_Base", "home_bg_base", 1f));

            foreach (var layer in HomeOverlayLayers) OverlayImage(frame, layer);

            // Meta' texture per torcia (RawImage.uvRect), scalata attorno al centro del suo bagliore.
            var glowTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(HomeAmbientFolder + "home_torch_glow.png");
            for (int side = 0; side < 2; side++)
            {
                var rect = CreateUIObject(side == 0 ? "TorchGlow_Left" : "TorchGlow_Right", frame);
                var raw = rect.gameObject.AddComponent<RawImage>();
                raw.texture = glowTexture;
                raw.uvRect = new Rect(side * 0.5f, 0f, 0.5f, 1f);
                raw.color = new Color(1f, 1f, 1f, 0.1f);
                raw.raycastTarget = false;
                float half = BaseWidth * 0.5f;
                PlaceInArt(rect, new Rect(side * half, 0f, half, BaseHeight));
                var centre = TorchGlowCentres[side];
                SetPivotKeepingPlace(rect, new Vector2((centre.x - side * half) / half, 1f - centre.y / BaseHeight));
                rect.localScale = new Vector3(TorchGlowScale, TorchGlowScale, 1f);
                Float(rect, 0f, 0f, 0f, 0f, 0.12f, 0.9f, 0.2f + side * 0.35f);
            }

            // Pivot alla base visibile della fiamma: il tremolio di scala parte dal braciere.
            var flameMaterial = FlameMaterial();
            var flameSize = SourceSize(FlameSprite);
            float k = FlameWidth / FlameVisible.width;
            foreach (var layer in HomeFlameLayers)
            {
                var rect = AmbientImage(frame, layer.Name, FlameSprite, 1f);
                rect.GetComponent<Image>().material = flameMaterial;
                rect.GetComponent<Image>().color = FlameTint;
                float left = layer.X - FlameVisible.center.x * k, top = layer.Y - FlameVisible.yMax * k;
                PlaceInArt(rect, new Rect(left, top, flameSize.x * k, flameSize.y * k));
                SetPivotKeepingPlace(rect, new Vector2(FlameVisible.center.x / flameSize.x, 1f - FlameVisible.yMax / flameSize.y));
                Float(rect, 0f, 0f, 0f, 0.015f, 0f, 0.6f, layer.X * 0.001f);
            }

            OverlayImage(frame, Bushes);

            var home = Object.FindObjectOfType<HomeV2Integration>(true);
            var so = new SerializedObject(home);
            so.FindProperty("homeAmbient").objectReferenceValue = group;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Sfondo Home animato ricostruito.");
        }

        private static void OverlayImage(RectTransform frame, OverlayLayer layer)
        {
            var rect = AmbientImage(frame, layer.Name, layer.Sprite, layer.Alpha);
            if (layer.Place.width > 0f) PlaceInArt(rect, layer.Place);
            else Stretch(rect);
            Float(rect, layer.MoveX, layer.MoveY, 0f, 0f, layer.AlphaAmount, layer.Duration, layer.Phase, layer.Pause);
        }

        // Rettangolo in px d'artwork (origine in alto a sinistra) -> ancore nella cornice.
        private static void PlaceInArt(RectTransform rect, Rect art)
        {
            rect.anchorMin = new Vector2(art.xMin / BaseWidth, 1f - art.yMax / BaseHeight);
            rect.anchorMax = new Vector2(art.xMax / BaseWidth, 1f - art.yMin / BaseHeight);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void SetPivotKeepingPlace(RectTransform rect, Vector2 pivot)
        {
            rect.pivot = pivot;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Material FlameMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FlameMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("UIV2/FlameWobble"));
                AssetDatabase.CreateAsset(material, FlameMaterialPath);
            }
            // 2.20: ondeggiamento piu' calmo, da sfondo.
            material.SetFloat("_Sway", 0.025f);
            material.SetFloat("_Speed", 0.8f);
            EditorUtility.SetDirty(material);
            return material;
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

        private static void Float(RectTransform rect, float moveX, float moveY, float rotation, float scale, float alpha, float duration, float phase, float pause = 0f)
        {
            var so = new SerializedObject(rect.gameObject.AddComponent<UIV2AmbientFloat>());
            so.FindProperty("moveX").floatValue = moveX;
            so.FindProperty("moveY").floatValue = moveY;
            so.FindProperty("rotation").floatValue = rotation;
            so.FindProperty("scaleAmount").floatValue = scale;
            so.FindProperty("alphaAmount").floatValue = alpha;
            so.FindProperty("duration").floatValue = duration;
            so.FindProperty("phase").floatValue = phase;
            so.FindProperty("pause").floatValue = pause;
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
