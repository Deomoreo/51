using System.IO;
using System.Linq;
using Project51.UIV2.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Emoticon animate: fogli 4x2 da 8 frame in SheetsDir/emo_&lt;nome&gt;.png. Nei fogli le facce non sono
    /// centrate nelle celle (fino a ~25 px di deriva tra righe e colonne), quindi il builder allinea ogni
    /// frame al frame 0 (correlazione delle maschere alfa), ricompone un foglio pulito in Art/Emoticons
    /// (celle quadrate uguali, niente sbavature dalle celle vicine) e ne ricava sprite, clip a 10 fps e un
    /// AnimatorController con uno stato per emoticon ("E"+indice). Collezione e pannello usano il frame 0,
    /// la nuvoletta al tavolo anima (GameSocialV2.ShowEmoticon). Un'emoticon senza foglio nuovo resta
    /// quella vecchia. Rilanciabile: dopo aver sostituito o aggiunto un foglio basta rilanciarlo.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const string AnimEmoDir = "Assets/UIV2/Art/Emoticons";
        private const int EmoCell = 448, EmoCols = 4, EmoRows = 2, EmoFrames = EmoCols * EmoRows;
        private const float EmoFps = 10f;

        [MenuItem("Tools/UIV2/Build Animated Emoticons")]
        private static void BuildAnimatedEmoticons()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(AnimEmoDir);

            var names = CollectionCosmeticsV2.Names;
            var firstFrames = new Sprite[names.Length];
            string controllerPath = AnimEmoDir + "/Emoticons.controller";
            AssetDatabase.DeleteAsset(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            // Stato di riposo vuoto: lo sprite statico lo imposta GameSocialV2, l'Animator non lo tocca.
            var rest = machine.AddState("Static"); rest.writeDefaultValues = false; machine.defaultState = rest;

            for (int i = 0; i < names.Length; i++)
            {
                string key = "emo_" + names[i].ToLowerInvariant();
                string source = SheetsDir + "/" + key + ".png";
                if (!File.Exists(source)) { Debug.Log("[AnimatedEmoticons] " + key + ": nessun foglio animato, resta lo sprite vecchio"); continue; }
                var frames = BuildEmoticonSheet(source, AnimEmoDir + "/" + key + "_anim.png", key);
                firstFrames[i] = frames[0];

                var clip = new AnimationClip { frameRate = EmoFps };
                // Unity allunga da sola la clip di un frame dopo l'ultima chiave: 8 frame = 0,8 s.
                var keys = frames.Select((s, f) => new ObjectReferenceKeyframe { time = f / EmoFps, value = s }).ToArray();
                AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(Image), "m_Sprite"), keys);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, AnimEmoDir + "/" + key + ".anim");
                var state = machine.AddState("E" + i); state.motion = clip; state.writeDefaultValues = false;
            }
            AssetDatabase.SaveAssets();

            var menu = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            var cosmetics = Object.FindObjectOfType<CollectionCosmeticsV2>(true);
            if (cosmetics == null) throw new System.Exception("[AnimatedEmoticons] CollectionCosmeticsV2 non trovato in MainMenu");
            for (int i = 0; i < firstFrames.Length; i++) if (firstFrames[i] != null) cosmetics.Emoticons[i] = firstFrames[i];
            EditorUtility.SetDirty(cosmetics);
            EditorSceneManager.SaveScene(menu);

            var game = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var social = Object.FindObjectOfType<GameSocialV2>(true);
            if (social == null) throw new System.Exception("[AnimatedEmoticons] GameSocialV2 non trovato in GameScene");
            for (int i = 0; i < firstFrames.Length; i++)
            {
                if (firstFrames[i] == null) continue;
                social.Sprites[i] = firstFrames[i];
                var icon = social.EmoticonButtons[i].transform.Find("Icon");
                if (icon != null) icon.GetComponent<Image>().sprite = firstFrames[i];
            }
            foreach (var face in social.BubbleImages)
            {
                face.sprite = social.Sprites[0];
                var animator = face.GetComponent<Animator>();
                if (animator == null) animator = face.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.enabled = false; // lo accende ShowEmoticon solo per le emoticon animate
                EditorUtility.SetDirty(face);
            }
            EditorUtility.SetDirty(social);
            EditorSceneManager.SaveScene(game);
            Debug.Log("[AnimatedEmoticons] fatto: " + firstFrames.Count(s => s != null) + " emoticon animate");
        }

        private static Sprite[] BuildEmoticonSheet(string source, string target, string key)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(source));
            int w = texture.width, h = texture.height;
            var src = texture.GetPixels32();
            Object.DestroyImmediate(texture);
            float cw = w / (float)EmoCols, ch = h / (float)EmoRows;
            int m = Mathf.FloorToInt(Mathf.Min(cw, ch));

            // Maschere alfa delle celle, origine in alto a sinistra come il foglio.
            var masks = new float[EmoFrames][];
            for (int f = 0; f < EmoFrames; f++)
            {
                int x0 = Mathf.RoundToInt(f % EmoCols * cw), y0 = Mathf.RoundToInt(f / EmoCols * ch);
                var mask = masks[f] = new float[m * m];
                for (int y = 0; y < m; y++)
                for (int x = 0; x < m; x++)
                    mask[y * m + x] = src[(h - 1 - (y0 + y)) * w + x0 + x].a > 127 ? 1f : 0f;
            }

            // Centro della faccia del frame 0: diventa il centro di ogni cella del nuovo foglio.
            int minX = m, minY = m, maxX = 0, maxY = 0;
            for (int y = 0; y < m; y++)
            for (int x = 0; x < m; x++)
                if (masks[0][y * m + x] > 0) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
            int anchorX = (minX + maxX + 1) / 2, anchorY = (minY + maxY + 1) / 2;

            int outW = EmoCols * EmoCell, outH = EmoRows * EmoCell, clipped = 0;
            var dst = new Color32[outW * outH];
            for (int f = 0; f < EmoFrames; f++)
            {
                var shift = AlignToReference(masks[0], masks[f], m);
                int x0 = Mathf.RoundToInt(f % EmoCols * cw), y0 = Mathf.RoundToInt(f / EmoCols * ch);
                int x1 = Mathf.RoundToInt((f % EmoCols + 1) * cw), y1 = Mathf.RoundToInt((f / EmoCols + 1) * ch);
                int ox = f % EmoCols * EmoCell + EmoCell / 2 - anchorX + shift.x;
                int oy = f / EmoCols * EmoCell + EmoCell / 2 - anchorY + shift.y;
                // Solo i pixel della propria cella: niente pezzi delle facce vicine.
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    var p = src[(h - 1 - y) * w + x];
                    if (p.a == 0) continue;
                    int tx = x - x0 + ox, ty = y - y0 + oy;
                    if (tx < f % EmoCols * EmoCell || tx >= (f % EmoCols + 1) * EmoCell || ty < f / EmoCols * EmoCell || ty >= (f / EmoCols + 1) * EmoCell) { if (p.a > 8) clipped++; continue; }
                    dst[(outH - 1 - ty) * outW + tx] = p;
                }
            }
            if (clipped > 0) Debug.LogWarning("[AnimatedEmoticons] " + key + ": " + clipped + " pixel oltre la cella di " + EmoCell + " px, tagliati");

            var output = new Texture2D(outW, outH, TextureFormat.RGBA32, false);
            output.SetPixels32(dst);
            File.WriteAllBytes(target, output.EncodeToPNG());
            Object.DestroyImmediate(output);
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(target);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024; // 256 px per frame: la nuvoletta ne mostra 125, la Collezione ~150
#pragma warning disable 618
            importer.spritesheet = Enumerable.Range(0, EmoFrames).Select(f => new SpriteMetaData
            {
                name = key + "_" + f,
                rect = new Rect(f % EmoCols * EmoCell, (EmoRows - 1 - f / EmoCols) * EmoCell, EmoCell, EmoCell),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(.5f, .5f)
            }).ToArray();
#pragma warning restore 618
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(target).OfType<Sprite>()
                .OrderBy(s => int.Parse(s.name.Substring(key.Length + 1))).ToArray();
        }

        // Spostamento (in pixel, verso il basso/destra) che sovrappone il frame al riferimento:
        // ricerca grossolana a 1/4 di risoluzione (+-48 px), poi rifinitura a piena risoluzione (+-4 px).
        private static Vector2Int AlignToReference(float[] reference, float[] frame, int size)
        {
            const int factor = 4, coarseRange = 12, fineRange = 4;
            int small = size / factor;
            var a = Downsample(reference, size, factor); var b = Downsample(frame, size, factor);
            var best = BestShift(a, b, small, Vector2Int.zero, coarseRange);
            return BestShift(reference, frame, size, best * factor, fineRange);
        }

        private static float[] Downsample(float[] mask, int size, int factor)
        {
            int small = size / factor; var result = new float[small * small];
            for (int y = 0; y < small * factor; y++)
            for (int x = 0; x < small * factor; x++)
                result[y / factor * small + x / factor] += mask[y * size + x];
            return result;
        }

        private static Vector2Int BestShift(float[] reference, float[] frame, int size, Vector2Int centre, int range)
        {
            var best = centre; float bestScore = -1;
            for (int dy = centre.y - range; dy <= centre.y + range; dy++)
            for (int dx = centre.x - range; dx <= centre.x + range; dx++)
            {
                float score = 0;
                for (int y = Mathf.Max(0, dy); y < Mathf.Min(size, size + dy); y++)
                {
                    int row = y * size, frameRow = (y - dy) * size - dx;
                    for (int x = Mathf.Max(0, dx); x < Mathf.Min(size, size + dx); x++)
                        score += reference[row + x] * frame[frameRow + x];
                }
                if (score > bestScore) { bestScore = score; best = new Vector2Int(dx, dy); }
            }
            return best;
        }
    }
}
