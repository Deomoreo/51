using Project51.UIV2.Components;
using Project51.UIV2.Core;
using Project51.Unity;
using Project51.Unity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Finestra dell'accuso manuale al tavolo (5 secondi): bagliore pulsante e anello del tempo attorno
    /// al pulsante ACCUSO, numero dei secondi, avviso sopra la mano. Aggiunge anche l'esplosione di luce
    /// dietro al pugno (AccusoImpactV2). Rilanciabile.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const string GlowSheetDir = "Assets/UI/Sprites/DragonsHoard/sprites_unity/sprites_unity/";
        private const string PoppinsSemiBoldPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/poppins-semibold SDF.asset";
        private static readonly Color AccusoGlowColor = new Color32(255, 196, 70, 255);

        [MenuItem("Tools/UIV2/Build Accuso Window")]
        private static void BuildAccusoWindow()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("GameCanvas");
            if (canvas == null) throw new System.Exception("GameCanvas non trovato in GameScene");
            var root = canvas.transform.Find("TableActionButtons") as RectTransform;
            var accuso = root != null ? root.Find("AccusoButton") as RectTransform : null;
            var controller = root != null ? root.GetComponentInChildren<TableActionButtonsController>(true) : null;
            if (accuso == null || controller == null) throw new System.Exception("TableActionButtons/AccusoButton non trovati");

            var extraBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsExtraBoldPath);
            var semiBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsSemiBoldPath);

            var turns = Object.FindObjectOfType<TurnController>(true);
            if (turns != null)
            {
                var so = new SerializedObject(turns);
                so.FindProperty("manualAccusoWindowSeconds").floatValue = 5f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (var name in new[] { "AccusoWindowGlow", "AccusoWindowRing", "AccusoWindowBadge", "AccusoWindowPrompt" })
            {
                var old = root.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            var oldCountdown = accuso.Find("AccusoCountdown");
            if (oldCountdown != null) oldCountdown.gameObject.SetActive(false);

            Vector2 center = accuso.anchoredPosition;

            // Bagliore rotondo dietro al pulsante (segue l'anello): fratello precedente, cosi' resta sotto.
            var glow = CreateUIObject("AccusoWindowGlow", root);
            PlaceTopLeft(glow, center, new Vector2(250f, 250f));
            glow.SetSiblingIndex(accuso.GetSiblingIndex());
            var glowImage = glow.gameObject.AddComponent<Image>();
            glowImage.sprite = LoadSprite(GlowSheetDir + "Bagliore morbido cerchio.png", "Bagliore morbido cerchio");
            glowImage.preserveAspect = true;
            glowImage.color = AccusoGlowColor;
            glowImage.raycastTarget = false;

            // Anello del tempo rimasto, sopra al pulsante.
            var ring = CreateUIObject("AccusoWindowRing", root);
            PlaceTopLeft(ring, center, new Vector2(134f, 134f));
            ring.SetSiblingIndex(accuso.GetSiblingIndex() + 1);
            var arc = ring.gameObject.AddComponent<RingArcGraphic>();
            arc.Thickness = 7f;
            arc.Arc = 1f;
            arc.DegreesPerSecond = 0f;
            arc.TrackColor = new Color32(14, 24, 40, 210);
            arc.TailColor = new Color32(232, 140, 40, 255);
            arc.HeadColor = new Color32(255, 214, 96, 255);
            arc.raycastTarget = false;

            // Numero dei secondi in un cerchio oro in alto a destra del pulsante.
            var badge = CreateUIObject("AccusoWindowBadge", root);
            PlaceTopLeft(badge, center + new Vector2(40f, 40f), new Vector2(46f, 46f));
            var badgeRing = badge.gameObject.AddComponent<Image>();
            SetRounded(badgeRing, LoadSprite(PanelsNeutralPath, "panel_fill_r24"), RouletteGold, 23f);
            var badgeFill = CreateUIObject("Fill", badge);
            PlaceCenter(badgeFill, new Vector2(40f, 40f));
            SetRounded(badgeFill.gameObject.AddComponent<Image>(), LoadSprite(PanelsNeutralPath, "panel_fill_r24"), new Color32(14, 28, 48, 255), 20f);
            var number = AddText(Stretch(badge, "Number"), "5", 25f, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
            if (extraBold != null) UseFont(number, extraBold, null);

            // Avviso sopra la mano.
            var prompt = CreateUIObject("AccusoWindowPrompt", root);
            PlaceTopLeft(prompt, new Vector2(540f, -1470f), new Vector2(580f, 62f));
            var group = prompt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            SetRounded(Stretch(prompt, "Fill").gameObject.AddComponent<Image>(), LoadSprite(PanelsNeutralPath, "panel_fill_r24"), new Color32(14, 28, 48, 235), 31f);
            SetRounded(Stretch(prompt, "Ring").gameObject.AddComponent<Image>(), LoadSprite(PanelsNeutralPath, "panel_ring_r24"), RouletteGold, 31f);
            var promptText = AddText(Stretch(prompt, "Text"), "Hai un accuso? Premi <color=#E8B24A>ACCUSO</color>", 26f, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
            promptText.richText = true;
            if (semiBold != null) UseFont(promptText, semiBold, null);

            SetPrivateField(controller, "accusoCountdownText", number);
            SetPrivateField(controller, "accusoCountdownBadge", badge.gameObject);
            SetPrivateField(controller, "accusoGlow", glowImage);
            SetPrivateField(controller, "accusoRing", arc);
            SetPrivateField(controller, "accusoPrompt", group);
            EditorUtility.SetDirty(controller);

            glow.gameObject.SetActive(false);
            ring.gameObject.SetActive(false);
            badge.gameObject.SetActive(false);
            prompt.gameObject.SetActive(false);

            // Alone dietro alla matta quando vale come un'altra carta per l'accuso.
            var cardViews = Object.FindObjectOfType<CardViewManager>(true);
            if (cardViews != null)
            {
                SetPrivateField(cardViews, "mattaHaloSprite", LoadSprite(GlowSheetDir + "Bagliore morbido cerchio.png", "Bagliore morbido cerchio"));
                EditorUtility.SetDirty(cardViews);
            }

            // Pugno: esplosione di luce dietro al pugno e titolo in Poppins.
            var social = Object.FindObjectOfType<GameSocialV2>(true);
            if (social != null && social.Impact != null)
            {
                var impact = social.Impact;
                var visual = impact.Group.transform;
                // Nella parte alta del feltro: carte dell'accuso, pugno e scritta non coprono il tavolo
                // (layout mockup 09: carte in tavolo da y 830 in giu').
                ((RectTransform)visual).anchoredPosition = new Vector2(0f, -640f);
                var oldBurst = visual.Find("Burst");
                if (oldBurst != null) Object.DestroyImmediate(oldBurst.gameObject);
                var burst = CreateUIObject("Burst", visual);
                burst.anchorMin = burst.anchorMax = impact.Fist.anchorMin;
                burst.pivot = new Vector2(0.5f, 0.5f);
                burst.anchoredPosition = impact.Fist.anchoredPosition;
                burst.sizeDelta = new Vector2(520f, 520f);
                burst.SetSiblingIndex(0);
                var burstImage = burst.gameObject.AddComponent<Image>();
                burstImage.sprite = LoadSprite(GlowSheetDir + "Bagliore morbido.png", "Bagliore morbido");
                burstImage.preserveAspect = true;
                burstImage.raycastTarget = false;
                burstImage.color = new Color(1f, 1f, 1f, 0f);
                impact.Burst = burstImage;
                if (extraBold != null)
                {
                    UseFont(impact.Caption, extraBold, GetOutlineMaterial("Outline Navy", OutlineNavy, 0.3f, 0.2f, extraBold));
                    impact.Caption.fontSize = 52f;
                    impact.Caption.fontStyle = FontStyles.Normal;
                }
                EditorUtility.SetDirty(impact);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Finestra accuso (5 s) e pugno aggiornati in GameScene.");
        }

        /// <summary>Ancora in alto a sinistra come i pulsanti del tavolo; anchoredPosition con y negativa.</summary>
        private static void PlaceTopLeft(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void PlaceCenter(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }
    }
}
