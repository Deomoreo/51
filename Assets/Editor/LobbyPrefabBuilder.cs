using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce via codice i prefab mancanti per la Waiting Room del Tavolo Privato
    /// (PlayerSlot, WaitingRoomPanel, JoinRoomPopup), usando ThemedPanel/ThemedButton
    /// e collegando i campi serializzati reali di WaitingRoomUI/JoinRoomPopupUI/PlayerSlotUI.
    /// Non tocca nessuna scena: costruisce in una preview scene temporanea e salva solo
    /// gli asset prefab. Salta i prefab gia' esistenti (non li sovrascrive).
    /// </summary>
    public static class LobbyPrefabBuilder
    {
        private const string ThemeResourcePath = "DragonsHoardTheme";
        private const string PrefabFolder = "Assets/Prefabs/UI";
        private const string PlayerSlotPrefabPath = PrefabFolder + "/PlayerSlot.prefab";
        private const string JoinRoomPopupPrefabPath = PrefabFolder + "/JoinRoomPopup.prefab";
        private const string WaitingRoomPanelPrefabPath = PrefabFolder + "/WaitingRoomPanel.prefab";

        // Non definito nel design system Dragon's Hoard (nessun colore "errore" nella palette):
        // placeholder da rivedere quando ci sara' una decisione di design esplicita.
        private static readonly Color ErrorColorPlaceholder = new Color(0.92f, 0.35f, 0.35f);

        [MenuItem("Tools/Lobby/Build Missing Prefabs")]
        private static void BuildMissingPrefabs()
        {
            var theme = Resources.Load<UITheme>(ThemeResourcePath);
            if (theme == null)
            {
                Debug.LogError($"[LobbyPrefabBuilder] UITheme non trovato in Resources/{ThemeResourcePath}. Aborto.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                Debug.LogError($"[LobbyPrefabBuilder] Cartella {PrefabFolder} non trovata. Aborto.");
                return;
            }

            var previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject playerSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerSlotPrefabPath);
                if (playerSlotPrefab == null)
                {
                    var instance = BuildPlayerSlot(theme, previewScene);
                    playerSlotPrefab = SaveAndDestroy(instance, PlayerSlotPrefabPath);
                    Debug.Log($"[LobbyPrefabBuilder] Creato {PlayerSlotPrefabPath}");
                }
                else
                {
                    Debug.Log($"[LobbyPrefabBuilder] {PlayerSlotPrefabPath} esiste gia', saltato.");
                }

                if (AssetDatabase.LoadAssetAtPath<GameObject>(JoinRoomPopupPrefabPath) == null)
                {
                    var instance = BuildJoinRoomPopup(theme, previewScene);
                    SaveAndDestroy(instance, JoinRoomPopupPrefabPath);
                    Debug.Log($"[LobbyPrefabBuilder] Creato {JoinRoomPopupPrefabPath}");
                }
                else
                {
                    Debug.Log($"[LobbyPrefabBuilder] {JoinRoomPopupPrefabPath} esiste gia', saltato.");
                }

                if (AssetDatabase.LoadAssetAtPath<GameObject>(WaitingRoomPanelPrefabPath) == null)
                {
                    var instance = BuildWaitingRoomPanel(theme, playerSlotPrefab, previewScene);
                    SaveAndDestroy(instance, WaitingRoomPanelPrefabPath);
                    Debug.Log($"[LobbyPrefabBuilder] Creato {WaitingRoomPanelPrefabPath}");
                }
                else
                {
                    Debug.Log($"[LobbyPrefabBuilder] {WaitingRoomPanelPrefabPath} esiste gia', saltato.");
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject SaveAndDestroy(GameObject instance, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        // ==================== Bot Fill Toggle (patch existing prefab) ====================

        [MenuItem("Tools/Lobby/Add Fill-With-Bots Toggle To Waiting Room")]
        private static void AddFillWithBotsToggle()
        {
            var theme = Resources.Load<UITheme>(ThemeResourcePath);
            if (theme == null)
            {
                Debug.LogError($"[LobbyPrefabBuilder] UITheme non trovato in Resources/{ThemeResourcePath}. Aborto.");
                return;
            }

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WaitingRoomPanelPrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"[LobbyPrefabBuilder] {WaitingRoomPanelPrefabPath} non trovato. Esegui prima 'Tools/Lobby/Build Missing Prefabs'. Aborto.");
                return;
            }

            var contentsRoot = PrefabUtility.LoadPrefabContents(WaitingRoomPanelPrefabPath);
            try
            {
                var waitingRoomUI = contentsRoot.GetComponent<WaitingRoomUI>();
                if (waitingRoomUI == null)
                {
                    Debug.LogError($"[LobbyPrefabBuilder] {WaitingRoomPanelPrefabPath} non ha un componente WaitingRoomUI. Aborto.");
                    return;
                }

                var so = new SerializedObject(waitingRoomUI);
                var toggleProp = so.FindProperty("fillWithBotsToggle");
                if (toggleProp.objectReferenceValue != null)
                {
                    Debug.Log("[LobbyPrefabBuilder] Il toggle 'riempi con bot' e' gia' presente su questo prefab, salto.");
                    return;
                }

                var existing = contentsRoot.transform.Find("FillWithBotsToggle");
                if (existing != null)
                {
                    Debug.LogWarning("[LobbyPrefabBuilder] Trovato un GameObject 'FillWithBotsToggle' orfano (non collegato allo script): lo ricollego invece di duplicarlo.");
                    var existingToggle = existing.GetComponent<Toggle>();
                    toggleProp.objectReferenceValue = existingToggle;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(contentsRoot, WaitingRoomPanelPrefabPath);
                    return;
                }

                // TMP_DefaultControls (nella versione di TextMeshPro in questo progetto) non offre
                // CreateToggle (a differenza di CreateButton/CreateInputField/CreateDropdown):
                // costruiamo il Toggle a mano, come gia' fatto altrove in questo file (es. BuildPlayerSlot).
                var toggleGO = new GameObject("FillWithBotsToggle", typeof(RectTransform), typeof(Toggle));
                SceneManager.MoveGameObjectToScene(toggleGO, contentsRoot.scene);
                toggleGO.transform.SetParent(contentsRoot.transform, false);
                SetAnchoredRect((RectTransform)toggleGO.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(460, 46), new Vector2(0, 205));

                var backgroundGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
                SceneManager.MoveGameObjectToScene(backgroundGO, contentsRoot.scene);
                backgroundGO.transform.SetParent(toggleGO.transform, false);
                var backgroundRt = (RectTransform)backgroundGO.transform;
                backgroundRt.anchorMin = new Vector2(0f, 0.5f);
                backgroundRt.anchorMax = new Vector2(0f, 0.5f);
                backgroundRt.pivot = new Vector2(0f, 0.5f);
                backgroundRt.sizeDelta = new Vector2(40, 40);
                backgroundRt.anchoredPosition = new Vector2(4, 0);
                var backgroundImage = backgroundGO.GetComponent<Image>();
                backgroundImage.color = new Color(1f, 1f, 1f, 0.15f);

                var checkmarkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
                SceneManager.MoveGameObjectToScene(checkmarkGO, contentsRoot.scene);
                checkmarkGO.transform.SetParent(backgroundGO.transform, false);
                var checkmarkRt = (RectTransform)checkmarkGO.transform;
                checkmarkRt.anchorMin = Vector2.zero;
                checkmarkRt.anchorMax = Vector2.one;
                checkmarkRt.offsetMin = new Vector2(6, 6);
                checkmarkRt.offsetMax = new Vector2(-6, -6);
                var checkmarkImage = checkmarkGO.GetComponent<Image>();
                checkmarkImage.color = theme.GemAccent;

                CreateStretchedText(toggleGO.transform, "Label", "Riempi i posti vuoti con bot",
                    22f, FontStyles.Bold, theme.Cream, TextAlignmentOptions.MidlineLeft,
                    new Vector2(52, 0), Vector2.zero);

                var toggle = toggleGO.GetComponent<Toggle>();
                toggle.targetGraphic = backgroundImage;
                toggle.graphic = checkmarkImage;
                toggle.isOn = false;

                toggleProp.objectReferenceValue = toggle;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contentsRoot, WaitingRoomPanelPrefabPath);
                Debug.Log($"[LobbyPrefabBuilder] Toggle 'riempi con bot' aggiunto a {WaitingRoomPanelPrefabPath} e collegato a WaitingRoomUI.fillWithBotsToggle.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contentsRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ==================== Matchmaking Status UI (patch existing scene object) ====================

        [MenuItem("Tools/Lobby/Build Matchmaking Status UI")]
        private static void BuildMatchmakingStatusUI()
        {
            var theme = Resources.Load<UITheme>(ThemeResourcePath);
            if (theme == null)
            {
                Debug.LogError($"[LobbyPrefabBuilder] UITheme non trovato in Resources/{ThemeResourcePath}. Aborto.");
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            if (Path.GetFileName(activeScene.path) != "MainMenu.unity")
            {
                Debug.LogError($"[LobbyPrefabBuilder] La scena attiva e' '{activeScene.path}', non MainMenu.unity. Apri MainMenu.unity e riprova. Aborto.");
                return;
            }

            var statusUI = Object.FindObjectOfType<MatchmakingStatusUI>(true);
            if (statusUI == null)
            {
                Debug.LogError("[LobbyPrefabBuilder] Nessun MatchmakingStatusUI trovato nella scena attiva. Aborto.");
                return;
            }

            var so = new SerializedObject(statusUI);
            var statusTextProp = so.FindProperty("statusText");
            if (statusTextProp.objectReferenceValue != null)
            {
                Debug.Log("[LobbyPrefabBuilder] MatchmakingStatusUI risulta gia' collegato (statusText assegnato), salto.");
                return;
            }

            var root = statusUI.gameObject;

            // Il GameObject esiste gia' in scena (referenziato da GameLaunchController.matchmakingStatusUI)
            // ma non ha mai avuto testo/animazione/bottone collegati: e' solo un Image bianco al 39%
            // di opacita' con lo sprite di default di Unity, senza contenuto. Show()/Hide() lo attivano/
            // disattivano comunque (i null-check sui figli mancanti sono innocui), quindi appariva a
            // schermo come un riquadro grigio vuoto e senza fade durante "Creazione stanza..." o il
            // caricamento della GameScene. Qui lo ritematizziamo riusando lo stesso Image (non lo
            // ricreiamo, per non rompere il riferimento gia' assegnato in GameLaunchController).
            var existingImage = root.GetComponent<Image>();
            if (existingImage != null)
            {
                existingImage.color = Color.white;
                ApplyThemedPanel(root, theme);
            }

            var canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = root.AddComponent<CanvasGroup>();

            var statusText = CreateStretchedText(root.transform, "StatusText", "Connessione...",
                30f, FontStyles.Bold, theme.Cream, TextAlignmentOptions.Center,
                new Vector2(24, 0), new Vector2(-24, 0));
            SetAnchoredRect((RectTransform)statusText.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(600, 60), new Vector2(0, 30));

            var detailText = CreateStretchedText(root.transform, "DetailText", "",
                20f, FontStyles.Normal, theme.TextMuted, TextAlignmentOptions.Center,
                new Vector2(24, 0), new Vector2(-24, 0));
            SetAnchoredRect((RectTransform)detailText.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(600, 40), new Vector2(0, -20));
            detailText.gameObject.SetActive(false);

            var cancelButton = CreateThemedButton(root.transform, "CancelButton", "ANNULLA", ThemedButton.Variant.Secondary, theme, new Vector2(220, 70), out _);
            SetAnchoredRect((RectTransform)cancelButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(220, 70), new Vector2(0, 60));

            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.FindProperty("detailText").objectReferenceValue = detailText;
            so.FindProperty("cancelButton").objectReferenceValue = cancelButton;
            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[LobbyPrefabBuilder] MatchmakingStatusUI ritematizzato e collegato (scena NON salvata - salva tu manualmente).");
        }

        // ==================== Wire Into Scene ====================

        [MenuItem("Tools/Lobby/Wire Prefabs Into Scene")]
        private static void WirePrefabsIntoScene()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (Path.GetFileName(activeScene.path) != "MainMenu.unity")
            {
                Debug.LogError($"[LobbyPrefabBuilder] La scena attiva e' '{activeScene.path}', non MainMenu.unity. Apri MainMenu.unity e riprova. Aborto.");
                return;
            }

            var gameLaunchController = Object.FindObjectOfType<GameLaunchController>();
            if (gameLaunchController == null)
            {
                Debug.LogError("[LobbyPrefabBuilder] Nessun GameLaunchController trovato nella scena attiva. Aborto.");
                return;
            }

            var glc = new SerializedObject(gameLaunchController);
            var modalityPanelProp = glc.FindProperty("modalityPanel");
            var waitingRoomUIProp = glc.FindProperty("waitingRoomUI");
            var joinRoomPopupProp = glc.FindProperty("joinRoomPopup");

            var modalityPanel = modalityPanelProp.objectReferenceValue as Component;
            if (modalityPanel == null)
            {
                Debug.LogError("[LobbyPrefabBuilder] GameLaunchController.modalityPanel e' null: non posso determinare il parent corretto. Aborto.");
                return;
            }

            Transform parent = modalityPanel.transform.parent;
            if (parent == null)
            {
                Debug.LogError($"[LobbyPrefabBuilder] '{modalityPanel.name}' (modalityPanel) non ha un parent in scena. Aborto.", modalityPanel);
                return;
            }

            Debug.Log($"[LobbyPrefabBuilder] Parent scelto: '{GetHierarchyPath(parent.gameObject)}' (stesso parent di modalityPanel='{modalityPanel.name}').", parent);

            bool waitingRoomAlreadySet = waitingRoomUIProp.objectReferenceValue != null;
            bool joinRoomAlreadySet = joinRoomPopupProp.objectReferenceValue != null;
            if (waitingRoomAlreadySet || joinRoomAlreadySet)
            {
                string current =
                    $"waitingRoomUI: {(waitingRoomAlreadySet ? waitingRoomUIProp.objectReferenceValue.name : "None")}\n" +
                    $"joinRoomPopup: {(joinRoomAlreadySet ? joinRoomPopupProp.objectReferenceValue.name : "None")}";

                bool overwrite = EditorUtility.DisplayDialog(
                    "GameLaunchController ha gia' dei riferimenti",
                    $"Uno o entrambi i campi risultano gia' assegnati:\n\n{current}\n\nSovrascrivere con le nuove istanze?",
                    "Sovrascrivi", "Annulla");

                if (!overwrite)
                {
                    Debug.Log("[LobbyPrefabBuilder] Annullato dall'utente: nessuna modifica applicata.");
                    return;
                }

                Debug.LogWarning("[LobbyPrefabBuilder] Sovrascrittura confermata dall'utente.");
            }

            var waitingRoomPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WaitingRoomPanelPrefabPath);
            var joinRoomPopupPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(JoinRoomPopupPrefabPath);
            if (waitingRoomPrefabAsset == null || joinRoomPopupPrefabAsset == null)
            {
                Debug.LogError($"[LobbyPrefabBuilder] Prefab mancanti su disco (WaitingRoomPanel trovato={waitingRoomPrefabAsset != null}, JoinRoomPopup trovato={joinRoomPopupPrefabAsset != null}). Esegui prima 'Tools/Lobby/Build Missing Prefabs'. Aborto.");
                return;
            }

            var waitingRoomInstance = (GameObject)PrefabUtility.InstantiatePrefab(waitingRoomPrefabAsset, parent);
            var joinRoomPopupInstance = (GameObject)PrefabUtility.InstantiatePrefab(joinRoomPopupPrefabAsset, parent);
            Undo.RegisterCreatedObjectUndo(waitingRoomInstance, "Wire Lobby Prefabs Into Scene");
            Undo.RegisterCreatedObjectUndo(joinRoomPopupInstance, "Wire Lobby Prefabs Into Scene");

            waitingRoomInstance.SetActive(false);
            joinRoomPopupInstance.SetActive(false);

            var waitingRoomUIComponent = waitingRoomInstance.GetComponent<WaitingRoomUI>();
            var joinRoomPopupUIComponent = joinRoomPopupInstance.GetComponent<JoinRoomPopupUI>();
            if (waitingRoomUIComponent == null || joinRoomPopupUIComponent == null)
            {
                Debug.LogError("[LobbyPrefabBuilder] Le istanze non hanno i componenti attesi (WaitingRoomUI / JoinRoomPopupUI). Le istanze restano in scena ma NON sono state collegate a GameLaunchController.");
                return;
            }

            Undo.RecordObject(gameLaunchController, "Wire Lobby Prefabs Into Scene");
            waitingRoomUIProp.objectReferenceValue = waitingRoomUIComponent;
            joinRoomPopupProp.objectReferenceValue = joinRoomPopupUIComponent;
            glc.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(activeScene);

            Debug.Log(
                "[LobbyPrefabBuilder] Wiring completato (scena NON salvata - salva tu manualmente):\n" +
                $"  Parent: '{GetHierarchyPath(parent.gameObject)}' (fileID {TryGetFileId(parent.gameObject)})\n" +
                $"  WaitingRoomPanel: '{GetHierarchyPath(waitingRoomInstance)}' (fileID {TryGetFileId(waitingRoomInstance)}), attivo={waitingRoomInstance.activeSelf}\n" +
                $"  JoinRoomPopup: '{GetHierarchyPath(joinRoomPopupInstance)}' (fileID {TryGetFileId(joinRoomPopupInstance)}), attivo={joinRoomPopupInstance.activeSelf}\n" +
                $"  GameLaunchController.waitingRoomUI -> '{waitingRoomUIComponent.name}' (fileID {TryGetFileId(waitingRoomUIComponent.gameObject)})\n" +
                $"  GameLaunchController.joinRoomPopup -> '{joinRoomPopupUIComponent.name}' (fileID {TryGetFileId(joinRoomPopupUIComponent.gameObject)})",
                gameLaunchController);
        }

        // ==================== PlayerSlot ====================

        private static GameObject BuildPlayerSlot(UITheme theme, Scene scene)
        {
            var root = CreateRoot("PlayerSlot", scene, new Vector2(600, 90));
            root.AddComponent<Image>();
            ApplyThemedPanel(root, theme);

            var layoutElement = root.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 90;
            layoutElement.flexibleWidth = 1;

            var slotNumberText = CreateStretchedText(root.transform, "SlotNumberText", "Slot 1",
                26f, FontStyles.Bold, theme.TextMuted, TextAlignmentOptions.Midline,
                new Vector2(16, 8), new Vector2(-96, -8));

            var playerNameText = CreateStretchedText(root.transform, "PlayerNameText", "",
                26f, FontStyles.Bold, theme.Cream, TextAlignmentOptions.MidlineLeft,
                new Vector2(16, 8), new Vector2(-96, -8));

            // Nessuno sprite crown/checkmark nel progetto (Assets/UI non ne contiene):
            // placeholder colorati da sostituire con icone reali del design system.
            var hostCrown = CreateIconFromRight(root.transform, "HostCrown", theme.Gold, new Vector2(-52, 0), new Vector2(36, 36));
            hostCrown.gameObject.SetActive(false);

            var readyCheckmark = CreateIconFromRight(root.transform, "ReadyCheckmark", theme.GemAccent, new Vector2(-16, 0), new Vector2(28, 28));
            readyCheckmark.gameObject.SetActive(false);

            var slotUI = root.AddComponent<PlayerSlotUI>();
            var so = new SerializedObject(slotUI);
            so.FindProperty("playerNameText").objectReferenceValue = playerNameText;
            so.FindProperty("slotNumberText").objectReferenceValue = slotNumberText;
            so.FindProperty("backgroundImage").objectReferenceValue = root.GetComponent<Image>();
            so.FindProperty("hostCrown").objectReferenceValue = hostCrown;
            so.FindProperty("readyCheckmark").objectReferenceValue = readyCheckmark;
            // emptyStateRoot / filledStateRoot lasciati non assegnati: PlayerSlotUI li gestisce
            // come opzionali (controlli null-check), e non richiesti dalla spec di questo prefab.
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // ==================== JoinRoomPopup ====================

        private static GameObject BuildJoinRoomPopup(UITheme theme, Scene scene)
        {
            var root = CreateRoot("JoinRoomPopup", scene, new Vector2(560, 460));
            root.AddComponent<Image>();
            ApplyThemedPanel(root, theme);
            var canvasGroup = root.AddComponent<CanvasGroup>();

            var titleText = CreateStretchedText(root.transform, "TitleText", "UNISCITI A UNA STANZA",
                34f, FontStyles.Bold, theme.Cream, TextAlignmentOptions.Center,
                new Vector2(24, 0), new Vector2(-24, 0));
            SetAnchoredRect((RectTransform)titleText.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(512, 70), new Vector2(0, -50));

            var inputGO = TMP_DefaultControls.CreateInputField(GetStandardTMPResources());
            inputGO.name = "RoomCodeInput";
            SceneManager.MoveGameObjectToScene(inputGO, scene);
            inputGO.transform.SetParent(root.transform, false);
            SetAnchoredRect((RectTransform)inputGO.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400, 70), new Vector2(0, 40));

            var tmpInputField = inputGO.GetComponent<TMP_InputField>();
            tmpInputField.characterLimit = 6;

            var placeholderTmp = inputGO.transform.Find("Text Area/Placeholder")?.GetComponent<TMP_Text>();
            if (placeholderTmp != null)
            {
                placeholderTmp.text = "CODICE STANZA";
                placeholderTmp.color = theme.TextMuted;
                placeholderTmp.fontStyle = FontStyles.Bold;
            }

            var inputTextTmp = inputGO.transform.Find("Text Area/Text")?.GetComponent<TMP_Text>();
            if (inputTextTmp != null)
            {
                inputTextTmp.color = theme.Cream;
                inputTextTmp.fontStyle = FontStyles.Bold;
            }

            var errorText = CreateStretchedText(root.transform, "ErrorText", "Codice troppo corto!",
                20f, FontStyles.Bold, ErrorColorPlaceholder, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.zero);
            SetAnchoredRect((RectTransform)errorText.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460, 40), new Vector2(0, -20));
            errorText.gameObject.SetActive(false);

            var joinButton = CreateThemedButton(root.transform, "JoinButton", "ENTRA", ThemedButton.Variant.Primary, theme, new Vector2(240, 90), out _);
            SetAnchoredRect((RectTransform)joinButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(240, 90), new Vector2(-130, 40));

            var cancelButton = CreateThemedButton(root.transform, "CancelButton", "ANNULLA", ThemedButton.Variant.Secondary, theme, new Vector2(240, 90), out _);
            SetAnchoredRect((RectTransform)cancelButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(240, 90), new Vector2(130, 40));

            var popupUI = root.AddComponent<JoinRoomPopupUI>();
            var so = new SerializedObject(popupUI);
            so.FindProperty("roomCodeInput").objectReferenceValue = tmpInputField;
            so.FindProperty("joinButton").objectReferenceValue = joinButton;
            so.FindProperty("cancelButton").objectReferenceValue = cancelButton;
            so.FindProperty("errorText").objectReferenceValue = errorText;
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("panelTransform").objectReferenceValue = (RectTransform)root.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // ==================== WaitingRoomPanel ====================

        private static GameObject BuildWaitingRoomPanel(UITheme theme, GameObject playerSlotPrefab, Scene scene)
        {
            var root = CreateRoot("WaitingRoomPanel", scene, Vector2.zero);
            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            root.AddComponent<Image>();
            ApplyThemedPanel(root, theme);
            var canvasGroup = root.AddComponent<CanvasGroup>();

            // "Heading" dal design system (Poppins Bold 34) - Poppins non e' importato come
            // TMP Font Asset nel progetto, quindi resta il font TMP di default: dimensione/peso
            // seguono comunque la spec, solo il font family e' un fallback.
            var roomCodeText = CreateStretchedText(root.transform, "RoomCodeText", "AB12C",
                34f, FontStyles.Bold, theme.Cream, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.zero);
            SetAnchoredRect((RectTransform)roomCodeText.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 60), new Vector2(0, -40));

            var copyButton = CreateThemedButton(root.transform, "CopyCodeButton", "COPIA CODICE", ThemedButton.Variant.Secondary, theme, new Vector2(220, 60), out _);
            SetAnchoredRect((RectTransform)copyButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(220, 60), new Vector2(0, -110));

            var copiedFeedback = CreateStretchedText(root.transform, "RoomCodeCopiedFeedback", "Copiato!",
                20f, FontStyles.Bold, theme.GemAccent, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.zero);
            SetAnchoredRect((RectTransform)copiedFeedback.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(220, 30), new Vector2(0, -180));

            var containerGO = new GameObject("PlayerSlotsContainer", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(containerGO, scene);
            containerGO.transform.SetParent(root.transform, false);
            SetAnchoredRect((RectTransform)containerGO.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(640, 420), new Vector2(0, -10));

            var vlg = containerGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childAlignment = TextAnchor.UpperCenter;
            containerGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            // Vuoto: WaitingRoomUI.CreatePlayerSlots() (Awake) svuota il container e instanzia
            // maxSlots copie di playerSlotPrefab a runtime - non pre-popolare qui.

            var statusText = CreateStretchedText(root.transform, "StatusText", "In attesa di giocatori...",
                22f, FontStyles.Bold, theme.TextMuted, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.zero);
            SetAnchoredRect((RectTransform)statusText.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(0, 150));

            var startButton = CreateThemedButton(root.transform, "StartButton", "AVVIA PARTITA", ThemedButton.Variant.Primary, theme, new Vector2(320, 90), out var startLabel);
            SetAnchoredRect((RectTransform)startButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(320, 90), new Vector2(-100, 40));

            var leaveButton = CreateThemedButton(root.transform, "LeaveButton", "ESCI", ThemedButton.Variant.Secondary, theme, new Vector2(180, 90), out _);
            SetAnchoredRect((RectTransform)leaveButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(180, 90), new Vector2(230, 40));

            var waitingRoomUI = root.AddComponent<WaitingRoomUI>();
            var so = new SerializedObject(waitingRoomUI);
            so.FindProperty("roomCodeText").objectReferenceValue = roomCodeText;
            so.FindProperty("copyCodeButton").objectReferenceValue = copyButton;
            so.FindProperty("roomCodeCopiedFeedback").objectReferenceValue = copiedFeedback;
            so.FindProperty("playerSlotsContainer").objectReferenceValue = containerGO.transform;
            so.FindProperty("playerSlotPrefab").objectReferenceValue = playerSlotPrefab;
            so.FindProperty("startButton").objectReferenceValue = startButton;
            so.FindProperty("leaveButton").objectReferenceValue = leaveButton;
            so.FindProperty("startButtonText").objectReferenceValue = startLabel;
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            // maxSlots e fadeInDuration lasciati ai default dello script (4 e 0.3s).
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // ==================== Helpers ====================

        private static GameObject CreateRoot(string name, Scene scene, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, scene);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = sizeDelta;
            return go;
        }

        private static ThemedPanel ApplyThemedPanel(GameObject go, UITheme theme)
        {
            // ThemedPanel non ha varianti (a differenza di ThemedButton): un solo stile
            // "Glass" sia nello script sia nel design system XML (Components/Panel variant="Glass").
            // Le richieste "Secondary" / "Primary/Large" per i pannelli non corrispondono a
            // nulla nel codice attuale - applicato lo stile unico disponibile.
            var panel = go.AddComponent<ThemedPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("theme").objectReferenceValue = theme;
            so.ApplyModifiedPropertiesWithoutUndo();
            panel.Apply();
            return panel;
        }

        private static Button CreateThemedButton(Transform parent, string name, string label, ThemedButton.Variant variant, UITheme theme, Vector2 size, out TMP_Text labelText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = size;

            var image = go.GetComponent<Image>();
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            labelText = CreateStretchedText(go.transform, "Label", label, 26f, FontStyles.Bold, theme.Cream, TextAlignmentOptions.Midline, Vector2.zero, Vector2.zero);

            var themedButton = go.AddComponent<ThemedButton>();
            var so = new SerializedObject(themedButton);
            so.FindProperty("theme").objectReferenceValue = theme;
            so.FindProperty("variant").enumValueIndex = (int)variant;
            so.ApplyModifiedPropertiesWithoutUndo();
            themedButton.Apply();

            return button;
        }

        private static TMP_Text CreateStretchedText(Transform parent, string name, string content, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Image CreateIconFromRight(Transform parent, string name, Color color, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(1, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(1, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void SetAnchoredRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
        }

        private static TMP_DefaultControls.Resources GetStandardTMPResources()
        {
            return new TMP_DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
            };
        }

        private static string GetHierarchyPath(GameObject go)
        {
            string path = go.name;
            var t = go.transform;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        private static string TryGetFileId(Object obj)
        {
            try
            {
                ulong id = Unsupported.GetLocalIdentifierInFileForPersistentObject(obj);
                return id == 0 ? "n/d (scena non ancora salvata)" : id.ToString();
            }
            catch
            {
                return "n/d (scena non ancora salvata)";
            }
        }
    }
}
