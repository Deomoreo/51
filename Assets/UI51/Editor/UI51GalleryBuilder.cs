using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project51.UI51.EditorTools
{
    /// <summary>
    /// Fase 1: scena di prova Assets/UI51/Scenes/UI51_Gallery.unity con tutti i prefab SPEC sez. 3 su UI51_Root
    /// (sfondo Home, lista scorrevole, barra di navigazione, bottom sheet, dialog) e UI51GalleryDemo per provarne
    /// animazioni e stati in Play. Non entra nei Build Settings. Rigenerata da zero a ogni esecuzione: e' una scena
    /// solo UI51, nessun'altra scena viene toccata. Se una scena aperta ha modifiche non salvate si ferma.
    /// </summary>
    public static class UI51GalleryBuilder
    {
        const string SceneFolder = UI51Build.Root + "/Scenes";
        const string ScenePath = SceneFolder + "/UI51_Gallery.unity";
        const float ContentWidth = 342f, NavHeight = 72f;

        static readonly string[] Required =
        {
            "UI51_Root", "GoldButton", "GoldButton_Panel", "GoldButton_Small", "OutlineButton", "DangerButton",
            "RoundIconButton", "Toggle", "Panel", "SegmentedTabs", "AvatarFrame", "Badge_More", "Badge_Dot",
            "PlayerBanner_Own", "PlayerBanner_Opponent", "PlayerBanner_Vertical", "BottomNav", "BottomSheet", "Dialog"
        };

        [MenuItem("Tools/UI51/Gallery Scene")]
        private static void Menu() => Build();

        /// <summary>Tutta la Fase 1 in ordine: import e atlas, font, prefab, scena gallery.</summary>
        [MenuItem("Tools/UI51/Build All (Fase 1)")]
        private static void BuildAll()
        {
            if (UI51Build.HasDirtyScene()) return;
            UI51ImportBuilder.Build();
            UI51FontBuilder.Build();
            UI51PrefabBuilder.Build();
            Build();
        }

        public static void Build()
        {
            if (UI51Build.HasDirtyScene()) return;
            foreach (string name in Required)
                if (Prefab(name) == null)
                {
                    Debug.LogError($"[UI51 Gallery] Prefab mancante: {UI51PrefabBuilder.PrefabPath(name)}. Esegui prima Tools/UI51/Component Prefabs.");
                    return;
                }

            UI51Build.EnsureFolder(SceneFolder);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            var cam = cameraGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var root = Instance("UI51_Root", null);
            var safe = root.transform.Find("SafeArea");

            // Sfondo Home a tutto schermo (fuori dalla SafeArea), proporzioni conservate coprendo il genitore.
            var bgSprite = UI51Build.Sprite("Backgrounds", "home_bg_base");
            var bg = UI51Build.Child(root.transform, "Background");
            bg.SetAsFirstSibling();
            UI51Build.Image(bg, bgSprite, Color.white, false, false);
            var fitter = UI51Build.GetOrAdd<AspectRatioFitter>(bg);
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = bgSprite != null ? bgSprite.rect.width / bgSprite.rect.height : 1153f / 2048f;

            var content = BuildScroll(safe);
            var banners = BuildGallery(content, out var avatar, out var demoButtons);

            Instance("BottomNav", safe);
            var sheet = Instance("BottomSheet", safe);
            var sheetConfirm = Instance("GoldButton_Panel", sheet.transform.Find("Sheet/Content"));
            UI51Build.Layout(sheetConfirm.transform, -1f, 52f, 1f);
            var dialog = Instance("Dialog", safe);

            var demo = new GameObject("GalleryDemo").AddComponent<UI51GalleryDemo>();
            UI51Build.Wire(demo, so =>
            {
                UI51Build.Ref(so, "m_Sheet", sheet.GetComponent<BottomSheet>());
                UI51Build.Ref(so, "m_Dialog", dialog.GetComponent<UI51Dialog>());
                UI51Build.Ref(so, "m_Avatar", avatar);
                var list = so.FindProperty("m_Banners");
                list.arraySize = banners.Length;
                for (int i = 0; i < banners.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = banners[i];
                UI51Build.Ref(so, "m_SheetButton", demoButtons[0]);
                UI51Build.Ref(so, "m_DialogButton", demoButtons[1]);
                UI51Build.Ref(so, "m_EmoticonButton", demoButtons[2]);
                UI51Build.Ref(so, "m_TurnButton", demoButtons[3]);
                UI51Build.Ref(so, "m_StyleButton", demoButtons[4]);
                UI51Build.Ref(so, "m_SheetConfirmButton", sheetConfirm.GetComponent<Button>());
            });

            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene, ScenePath)) Debug.Log($"[UI51 Gallery] Scena salvata: {ScenePath}");
            else Debug.LogError($"[UI51 Gallery] Salvataggio fallito: {ScenePath}");
        }

        /// <summary>ScrollRect verticale sopra la barra di navigazione; ritorna il Content (colonna centrata).</summary>
        static Transform BuildScroll(Transform safe)
        {
            var scrollRt = UI51Build.Stretch(UI51Build.Child(safe, "Scroll"), 0f, NavHeight, 0f, 0f);
            var scroll = UI51Build.GetOrAdd<ScrollRect>(scrollRt);

            var viewport = UI51Build.Stretch(UI51Build.Child(scrollRt, "Viewport"));
            UI51Build.Image(viewport, null, Color.clear, true, false);
            UI51Build.GetOrAdd<RectMask2D>(viewport);

            var content = UI51Build.Child(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            UI51Build.Column(content, 14f, UI51Build.Pad(24, 24, 24, 24), TextAnchor.UpperCenter, false, false);
            UI51Build.Fit(content, false, true);

            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
            return content;
        }

        /// <summary>
        /// Sezioni della gallery. Ritorna i banner (il primo e' il proprio: turno e timer) e i cinque bottoni di prova
        /// in ordine sheet, dialog, emoticon, turno, stile.
        /// </summary>
        static PlayerBanner[] BuildGallery(Transform content, out AvatarFrame avatar, out Button[] demoButtons)
        {
            Section(content, "PROVE");
            string[] labels = { "Apri sheet", "Dialog", "Emoticon", "Turno + timer", "Stile banner" };
            demoButtons = new Button[labels.Length];
            Transform row = null;
            for (int i = 0; i < labels.Length; i++)
            {
                if (i % 2 == 0) row = Row(content, "DemoRow" + (i / 2), 38f);
                var button = Instance("GoldButton_Small", row);
                button.name = "Demo_" + labels[i];
                SetLabel(button, labels[i]);
                demoButtons[i] = button.GetComponent<Button>();
            }

            Section(content, "BANNER GIOCATORE");
            var bannerRow = Row(content, "BannerRow", 50f);
            var own = Instance("PlayerBanner_Own", bannerRow).GetComponent<PlayerBanner>();
            var opponent = Instance("PlayerBanner_Opponent", bannerRow).GetComponent<PlayerBanner>();
            var vertical = Instance("PlayerBanner_Vertical", content).GetComponent<PlayerBanner>();

            Section(content, "AVATAR E BADGE");
            var avatarRow = Row(content, "AvatarRow", 64f);
            avatar = Instance("AvatarFrame", avatarRow).GetComponent<AvatarFrame>();
            Instance("Badge_More", avatarRow);
            Instance("Badge_Dot", avatarRow);

            Section(content, "BOTTONI");
            Instance("GoldButton", content);
            Instance("GoldButton_Panel", content);
            var smallRow = Row(content, "SmallRow", 40f);
            Instance("GoldButton_Small", smallRow);
            Instance("RoundIconButton", smallRow);
            Instance("Toggle", smallRow);
            Instance("OutlineButton", content);
            Instance("DangerButton", content);

            Section(content, "PANNELLO E TAB");
            Instance("SegmentedTabs", content);
            Instance("Panel", content);

            return new[] { own, opponent, vertical };
        }

        // --- Helper

        static GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(UI51PrefabBuilder.PrefabPath(name));

        static GameObject Instance(string name, Transform parent) =>
            (GameObject)PrefabUtility.InstantiatePrefab(Prefab(name), parent);

        static void Section(Transform content, string title)
        {
            var rt = UI51Build.Size(UI51Build.Child(content, "Section " + title), ContentWidth, 18f);
            UI51Build.Text(rt, title, FontFace.CinzelBold, 12f, UI51Tokens.Gold, TextAlignmentOptions.Left, 2f);
        }

        /// <summary>Riga orizzontale centrata a misura fissa: i figli tengono la dimensione del prefab.</summary>
        static Transform Row(Transform content, string name, float height)
        {
            var rt = UI51Build.Size(UI51Build.Child(content, name), ContentWidth, height);
            UI51Build.Row(rt, 16f, null, TextAnchor.MiddleCenter, false, false);
            return rt;
        }

        static void SetLabel(GameObject button, string text)
        {
            var label = button.transform.Find("Label").GetComponent<TMP_Text>();
            label.text = text;
            PrefabUtility.RecordPrefabInstancePropertyModifications(label);
        }
    }
}
