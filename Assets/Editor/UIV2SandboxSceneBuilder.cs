using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Core;
using Project51.UIV2.Components;
using Project51.UIV2.Screens;
using Project51.UIV2.Tests;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce Assets/UIV2/Tests/UIV2_Sandbox.unity: istanzia UIV2_Root + TopBar +
    /// BottomNav + FriendsScreenV2 e li popola con UIV2SandboxBootstrap, per verificare
    /// visivamente la foundation senza toccare MainMenu/HomeScreen/GameScene. Gli sprite reali
    /// (avatar, icone risorsa) vengono caricati qui (Editor-only, AssetDatabase) e assegnati ai
    /// campi serializzati del bootstrap - UIV2SandboxBootstrap.cs resta puro codice runtime.
    /// </summary>
    public static class UIV2SandboxSceneBuilder
    {
        private const string ScenePath = "Assets/UIV2/Tests/UIV2_Sandbox.unity";
        private const string RootPrefabPath = "Assets/UIV2/Prefabs/Core/UIV2_Root.prefab";
        private const string TopBarPrefabPath = "Assets/UIV2/Prefabs/Components/UIV2_TopBar.prefab";
        private const string BottomNavPrefabPath = "Assets/UIV2/Prefabs/Components/UIV2_BottomNav.prefab";
        private const string FriendsScreenPrefabPath = "Assets/UIV2/Prefabs/Screens/FriendsScreenV2.prefab";
        private const string ProgressBarPrefabPath = "Assets/UIV2/Prefabs/Components/UIV2_ProgressBar.prefab";
        private const string HomeScreenPrefabPath = "Assets/UIV2/Prefabs/Screens/HomeScreenV2.prefab";
        private const string HomePreviewScenePath = "Assets/UIV2/Tests/UIV2_HomeV2_Preview.unity";
        private const string CollectionScreenPrefabPath = "Assets/UIV2/Prefabs/Screens/CollectionScreenV2.prefab";
        private const string CollectionPreviewScenePath = "Assets/UIV2/Tests/UIV2_CollectionV2_Preview.unity";
        private const string ShopScreenPrefabPath = "Assets/UIV2/Prefabs/Screens/ShopScreenV2.prefab";
        private const string ShopPreviewScenePath = "Assets/UIV2/Tests/UIV2_ShopV2_Preview.unity";
        private const string ProfileScreenPrefabPath = "Assets/UIV2/Prefabs/Screens/ProfileScreenV2.prefab";
        private const string ProfilePreviewScenePath = "Assets/UIV2/Tests/UIV2_ProfileV2_Preview.unity";

        private const string SheetsDir = "Assets/UI/Sprites/DragonsHoard/sprites_unity/sprites_unity";
        private const string IconsPath = SheetsDir + "/Icons.png";
        private const string AvatarsPath = SheetsDir + "/Avatars.png";
        private const string EmoticonsPath = SheetsDir + "/14_emoticon_set.png";

        private static Sprite LoadSprite(string sheetPath, string spriteName)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().FirstOrDefault(s => s.name == spriteName);
            if (sprite == null)
            {
                Debug.LogError($"[UIV2SandboxSceneBuilder] Sprite '{spriteName}' non trovato in {sheetPath}.");
            }
            return sprite;
        }

        [MenuItem("Tools/UIV2/Build Sandbox Scene")]
        private static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            var rootInstance = (GameObject)PrefabUtility.InstantiatePrefab(rootPrefab);
            var uiv2Root = rootInstance.GetComponent<UIV2Root>();

            var topBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopBarPrefabPath);
            var topBarInstance = (GameObject)PrefabUtility.InstantiatePrefab(topBarPrefab, uiv2Root.TopBarHost);
            StretchFill((RectTransform)topBarInstance.transform);

            var bottomNavPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BottomNavPrefabPath);
            var bottomNavInstance = (GameObject)PrefabUtility.InstantiatePrefab(bottomNavPrefab, uiv2Root.BottomNavHost);
            StretchFill((RectTransform)bottomNavInstance.transform);

            var friendsScreenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FriendsScreenPrefabPath);
            var friendsScreenInstance = (GameObject)PrefabUtility.InstantiatePrefab(friendsScreenPrefab, uiv2Root.ScreenHost);
            StretchFill((RectTransform)friendsScreenInstance.transform);

            var progressBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProgressBarPrefabPath);
            BuildProgressBarShowcase(uiv2Root.OverlayHost, progressBarPrefab);

            var bootstrapGo = new GameObject("SandboxBootstrap");
            var bootstrap = bootstrapGo.AddComponent<UIV2SandboxBootstrap>();
            var so = new SerializedObject(bootstrap);
            so.FindProperty("topBar").objectReferenceValue = topBarInstance.GetComponent<UIV2TopBar>();
            so.FindProperty("bottomNav").objectReferenceValue = bottomNavInstance.GetComponent<UIV2BottomNav>();
            so.FindProperty("friendsScreen").objectReferenceValue = friendsScreenInstance.GetComponent<FriendsScreenController>();

            // Ogni "persona" del mock ha un avatar_XX diverso - dimostra selezione reale
            // per-ViewData, non un singolo avatar globale (avatar_01 non e' hardcoded qui).
            so.FindProperty("avatarPlayerDemo").objectReferenceValue = LoadSprite(AvatarsPath, "avatar_08");
            so.FindProperty("avatarMarco").objectReferenceValue = LoadSprite(AvatarsPath, "avatar_03");
            so.FindProperty("avatarLuca").objectReferenceValue = LoadSprite(AvatarsPath, "avatar_06");
            so.FindProperty("avatarGiulia").objectReferenceValue = LoadSprite(AvatarsPath, "avatar_01");
            so.FindProperty("iconGold").objectReferenceValue = LoadSprite(IconsPath, "ic_coin_clover");
            so.FindProperty("iconGems").objectReferenceValue = LoadSprite(IconsPath, "ic_gem_green");
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[UIV2SandboxSceneBuilder] Scena creata: {ScenePath}");
        }

        private static void StretchFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Costruisce Assets/UIV2/Tests/UIV2_HomeV2_Preview.unity: UIV2_Root + TopBar +
        /// BottomNav + HomeScreenV2, popolati con HomeV2PreviewBootstrap - dedicata cosi'
        /// FriendsScreenV2 nell'altra sandbox non viene toccata/sostituita.
        /// </summary>
        [MenuItem("Tools/UIV2/Build Home V2 Preview Scene")]
        private static void BuildHomePreview()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            var rootInstance = (GameObject)PrefabUtility.InstantiatePrefab(rootPrefab);
            var uiv2Root = rootInstance.GetComponent<UIV2Root>();

            var topBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopBarPrefabPath);
            var topBarInstance = (GameObject)PrefabUtility.InstantiatePrefab(topBarPrefab, uiv2Root.TopBarHost);
            StretchFill((RectTransform)topBarInstance.transform);

            var bottomNavPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BottomNavPrefabPath);
            var bottomNavInstance = (GameObject)PrefabUtility.InstantiatePrefab(bottomNavPrefab, uiv2Root.BottomNavHost);
            StretchFill((RectTransform)bottomNavInstance.transform);

            var homeScreenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HomeScreenPrefabPath);
            var homeScreenInstance = (GameObject)PrefabUtility.InstantiatePrefab(homeScreenPrefab, uiv2Root.ScreenHost);
            StretchFill((RectTransform)homeScreenInstance.transform);

            var bootstrapGo = new GameObject("HomeV2PreviewBootstrap");
            var bootstrap = bootstrapGo.AddComponent<HomeV2PreviewBootstrap>();
            var so = new SerializedObject(bootstrap);
            so.FindProperty("topBar").objectReferenceValue = topBarInstance.GetComponent<UIV2TopBar>();
            so.FindProperty("bottomNav").objectReferenceValue = bottomNavInstance.GetComponent<UIV2BottomNav>();
            so.FindProperty("homeScreen").objectReferenceValue = homeScreenInstance.GetComponent<HomeScreenV2>();
            so.FindProperty("avatarPlayerDemo").objectReferenceValue = LoadSprite(AvatarsPath, "avatar_08");
            so.FindProperty("iconGold").objectReferenceValue = LoadSprite(IconsPath, "ic_coin_clover");
            so.FindProperty("iconGems").objectReferenceValue = LoadSprite(IconsPath, "ic_gem_green");
            so.FindProperty("iconMode").objectReferenceValue = LoadSprite(IconsPath, "ic_gamepad");
            so.FindProperty("iconDeck").objectReferenceValue = LoadSprite(IconsPath, "ic_cards");
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, HomePreviewScenePath);
            Debug.Log($"[UIV2SandboxSceneBuilder] Scena creata: {HomePreviewScenePath}");
        }

        /// <summary>
        /// Costruisce Assets/UIV2/Tests/UIV2_CollectionV2_Preview.unity: UIV2_Root + TopBar +
        /// BottomNav (Carte selezionata) + CollectionScreenV2, popolati da CollectionV2PreviewBootstrap.
        /// </summary>
        [MenuItem("Tools/UIV2/Build Collection V2 Preview Scene")]
        private static void BuildCollectionPreview()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            var rootInstance = (GameObject)PrefabUtility.InstantiatePrefab(rootPrefab);
            var uiv2Root = rootInstance.GetComponent<UIV2Root>();

            var topBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopBarPrefabPath);
            var topBarInstance = (GameObject)PrefabUtility.InstantiatePrefab(topBarPrefab, uiv2Root.TopBarHost);
            StretchFill((RectTransform)topBarInstance.transform);

            var bottomNavPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BottomNavPrefabPath);
            var bottomNavInstance = (GameObject)PrefabUtility.InstantiatePrefab(bottomNavPrefab, uiv2Root.BottomNavHost);
            StretchFill((RectTransform)bottomNavInstance.transform);

            var collectionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionScreenPrefabPath);
            var collectionInstance = (GameObject)PrefabUtility.InstantiatePrefab(collectionPrefab, uiv2Root.ScreenHost);
            StretchFill((RectTransform)collectionInstance.transform);

            var bootstrapGo = new GameObject("CollectionV2PreviewBootstrap");
            var bootstrap = bootstrapGo.AddComponent<CollectionV2PreviewBootstrap>();
            var so = new SerializedObject(bootstrap);
            so.FindProperty("topBar").objectReferenceValue = topBarInstance.GetComponent<UIV2TopBar>();
            so.FindProperty("bottomNav").objectReferenceValue = bottomNavInstance.GetComponent<UIV2BottomNav>();
            so.FindProperty("collectionScreen").objectReferenceValue = collectionInstance.GetComponent<CollectionScreenV2>();
            so.FindProperty("deckArtDemo").objectReferenceValue = LoadSprite(IconsPath, "card_back_green");
            string[] emoticonSpriteNames = { "emo_risata", "emo_arrabbiato", "emo_sorpreso", "emo_pensieroso", "emo_triste", "emo_furbo" };
            var emoticonsProp = so.FindProperty("emoticonSprites");
            emoticonsProp.arraySize = emoticonSpriteNames.Length;
            for (int i = 0; i < emoticonSpriteNames.Length; i++)
            {
                emoticonsProp.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite(EmoticonsPath, emoticonSpriteNames[i]);
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, CollectionPreviewScenePath);
            Debug.Log($"[UIV2SandboxSceneBuilder] Scena creata: {CollectionPreviewScenePath}");
        }

        /// <summary>
        /// Costruisce Assets/UIV2/Tests/UIV2_ShopV2_Preview.unity: UIV2_Root + TopBar + BottomNav
        /// (Negozio selezionato) + ShopScreenV2, popolati da ShopV2PreviewBootstrap.
        /// </summary>
        [MenuItem("Tools/UIV2/Build Shop V2 Preview Scene")]
        private static void BuildShopPreview()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            var rootInstance = (GameObject)PrefabUtility.InstantiatePrefab(rootPrefab);
            var uiv2Root = rootInstance.GetComponent<UIV2Root>();

            var topBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopBarPrefabPath);
            var topBarInstance = (GameObject)PrefabUtility.InstantiatePrefab(topBarPrefab, uiv2Root.TopBarHost);
            StretchFill((RectTransform)topBarInstance.transform);

            var bottomNavPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BottomNavPrefabPath);
            var bottomNavInstance = (GameObject)PrefabUtility.InstantiatePrefab(bottomNavPrefab, uiv2Root.BottomNavHost);
            StretchFill((RectTransform)bottomNavInstance.transform);

            var shopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopScreenPrefabPath);
            var shopInstance = (GameObject)PrefabUtility.InstantiatePrefab(shopPrefab, uiv2Root.ScreenHost);
            StretchFill((RectTransform)shopInstance.transform);

            var bootstrapGo = new GameObject("ShopV2PreviewBootstrap");
            var bootstrap = bootstrapGo.AddComponent<ShopV2PreviewBootstrap>();
            var so = new SerializedObject(bootstrap);
            so.FindProperty("topBar").objectReferenceValue = topBarInstance.GetComponent<UIV2TopBar>();
            so.FindProperty("bottomNav").objectReferenceValue = bottomNavInstance.GetComponent<UIV2BottomNav>();
            so.FindProperty("shopScreen").objectReferenceValue = shopInstance.GetComponent<ShopScreenV2>();
            so.FindProperty("coinIcon").objectReferenceValue = LoadSprite(IconsPath, "ic_coin_clover");
            so.FindProperty("gemIcon").objectReferenceValue = LoadSprite(IconsPath, "ic_gem_green");
            so.FindProperty("chestGreen").objectReferenceValue = LoadSprite(IconsPath, "chest_green");
            so.FindProperty("chestPurple").objectReferenceValue = LoadSprite(IconsPath, "chest_purple");
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ShopPreviewScenePath);
            Debug.Log($"[UIV2SandboxSceneBuilder] Scena creata: {ShopPreviewScenePath}");
        }

        /// <summary>
        /// Costruisce Assets/UIV2/Tests/UIV2_ProfileV2_Preview.unity: UIV2_Root + TopBar + BottomNav
        /// (Profilo selezionato) + ProfileScreenV2, popolati da ProfileV2PreviewBootstrap.
        /// </summary>
        [MenuItem("Tools/UIV2/Build Profile V2 Preview Scene")]
        private static void BuildProfilePreview()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            var rootInstance = (GameObject)PrefabUtility.InstantiatePrefab(rootPrefab);
            var uiv2Root = rootInstance.GetComponent<UIV2Root>();

            var topBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopBarPrefabPath);
            var topBarInstance = (GameObject)PrefabUtility.InstantiatePrefab(topBarPrefab, uiv2Root.TopBarHost);
            StretchFill((RectTransform)topBarInstance.transform);

            var bottomNavPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BottomNavPrefabPath);
            var bottomNavInstance = (GameObject)PrefabUtility.InstantiatePrefab(bottomNavPrefab, uiv2Root.BottomNavHost);
            StretchFill((RectTransform)bottomNavInstance.transform);

            var profilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProfileScreenPrefabPath);
            var profileInstance = (GameObject)PrefabUtility.InstantiatePrefab(profilePrefab, uiv2Root.ScreenHost);
            StretchFill((RectTransform)profileInstance.transform);

            var bootstrapGo = new GameObject("ProfileV2PreviewBootstrap");
            var bootstrap = bootstrapGo.AddComponent<ProfileV2PreviewBootstrap>();
            var so = new SerializedObject(bootstrap);
            so.FindProperty("topBar").objectReferenceValue = topBarInstance.GetComponent<UIV2TopBar>();
            so.FindProperty("bottomNav").objectReferenceValue = bottomNavInstance.GetComponent<UIV2BottomNav>();
            so.FindProperty("profileScreen").objectReferenceValue = profileInstance.GetComponent<ProfileScreenV2>();
            so.FindProperty("demoPortrait").objectReferenceValue = LoadSprite(AvatarsPath, "avatar_08");
            string[] trophySpriteNames = { "ic_trophy", "ic_cup", "Icons_12", "ic_gem_green", "ic_clover_circle" };
            var trophiesProp = so.FindProperty("trophyIcons");
            trophiesProp.arraySize = trophySpriteNames.Length;
            for (int i = 0; i < trophySpriteNames.Length; i++)
            {
                trophiesProp.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite(IconsPath, trophySpriteNames[i]);
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ProfilePreviewScenePath);
            Debug.Log($"[UIV2SandboxSceneBuilder] Scena creata: {ProfilePreviewScenePath}");
        }

        /// <summary>
        /// Verifica visiva del fix UIV2_ProgressBar (Sliced, non Filled): 5 istanze fisse a
        /// 0/25/50/75/100%, dentro una VerticalLayoutGroup per dimostrare compatibilita' con
        /// i layout group. Solo per la sandbox - non fa parte della foundation riusabile.
        /// </summary>
        private static void BuildProgressBarShowcase(RectTransform parent, GameObject progressBarPrefab)
        {
            if (progressBarPrefab == null) return;

            var containerGo = new GameObject("ProgressBarShowcase", typeof(RectTransform));
            var containerRect = (RectTransform)containerGo.transform;
            containerRect.SetParent(parent, false);
            containerRect.anchorMin = new Vector2(0f, 1f);
            containerRect.anchorMax = new Vector2(0f, 1f);
            containerRect.pivot = new Vector2(0f, 1f);
            containerRect.anchoredPosition = new Vector2(40f, -220f);
            containerRect.sizeDelta = new Vector2(560f, 320f);

            var vlg = containerGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            float[] previewValues = { 0f, 0.25f, 0.5f, 0.75f, 1f };
            foreach (var value in previewValues)
            {
                var barInstance = (GameObject)PrefabUtility.InstantiatePrefab(progressBarPrefab, containerRect);
                barInstance.name = $"ProgressBar_{Mathf.RoundToInt(value * 100)}pct";

                var le = barInstance.AddComponent<LayoutElement>();
                le.preferredWidth = 500f;
                le.preferredHeight = 44f;

                var label = barInstance.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = $"{Mathf.RoundToInt(value * 100)}%";

                barInstance.GetComponent<UIV2ProgressBar>().SetProgress(value, animate: false);
            }
        }
    }
}
