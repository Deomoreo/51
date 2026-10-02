# Fase 10 Pulizia: execution plan

Base: v2.63, branch `codex/home-v2-training`, one live Unity Editor, one engineer, steps done in order. Batches B0 to B10 go from safest to riskiest. Every batch leaves the project compiling. Items that wait for a user decision are listed separately at the end and are **not scheduled**.

---

## 0. Rules for every batch

**Before B0**
- Make sure no other agent is driving the Editor.
- Record a baseline:
  - EditMode test count and pass state.
  - Console error count after a clean recompile. `UI51Build.Ref` and `SetArray` log errors without throwing, so write down the exact baseline lines.
  - `git status`.

**Line numbers** refer to the v2.63 snapshot. Earlier edits shift them, so always find the spot by its content and treat the number as a hint.

**Deleting files**
- Every file deletion below means the file plus its `.meta`. Every folder deletion means the folder plus `folder.meta`.
- Delete with `AssetDatabase.DeleteAsset(path)`, or with `git rm path path.meta` followed by `AssetDatabase.Refresh()`.
- Before deleting a script, prefab or material, grep its GUID:
  `g=$(grep -m1 guid X.meta | awk '{print $2}'); grep -rl "$g" Assets ProjectSettings --include=*.unity --include=*.prefab --include=*.asset --include=*.mat --include=*.controller`
  - Any hit not listed in the batch means stop and report.

**Deleting scene objects**
- Only with snippet **S** below. Never edit scene or prefab YAML by hand.
- Also grep the object's name in `Assets/**/*.cs` for `Find("…")`, `FindPath`, `FindInScene`, `HideChild` and `UIV2DesignSystem` name checks. The reference guard in S cannot see string lookups.

**Prefab children:** change them with `PrefabUtility.LoadPrefabContents` → `DestroyImmediate` → `SaveAsPrefabAsset` → `UnloadPrefabContents`. Then run `PrefabUtility.RemoveUnusedOverrides(instances, InteractionMode.AutomatedAction)` on the scene instances.

**Removed serialized fields:** at the end of any batch that removed `[SerializeField]`s, run `AssetDatabase.ForceReserializeAssets(new[]{ "Assets/Scenes/MainMenu.unity", "Assets/Scenes/GameScene.unity", <touched prefabs> })` so stale data is dropped.

**Encoding**

| Kind | Files | How to edit |
|---|---|---|
| Latin-1 / non-UTF-8, byte-safe only | HomePanelUI (CRLF), ModalitySelectorPanelUI (CRLF), **Rules51.cs** (CRLF, bytes at lines 96 and 325), **AuthUIController** (CRLF), **PlayFabAuthService** (CRLF), **ProfileService** (LF), **PlayerProgressLocal** (LF), **MatchmakingManager** (LF), SafeAreaUtil (CRLF), SlideUpPanelUI, BottomNavController, DeckSelectorPanelUI, PanelManager, README_AUTH.md, MattaAccusiCombinationsTests (CRLF) | Python script written with the **Write tool** into the scratchpad (no bash heredoc; if unavoidable use `chr(92)`, `chr(13)+chr(10)`): `s=open(p,'rb').read().decode('latin-1')`; `assert s.count(old)==1`; `s=s.replace(old,new)`; `open(p,'wb').write(s.encode('latin-1'))`. Old/new strings must carry `\r\n` on CRLF files. |
| UTF-8 | everything else (CRLF: CardViewManager, MatchResultsV2, NetworkGameController, GameLaunchController, AuthBootstrapper, AppFlowManager, RoundEndPanel, PhotonAuthConnector) | Edit tool. Never `sed -i`: it strips CRLF. |

**Builders**
- Any builder that is kept or edited keeps its explicit `EditorSceneManager.OpenScene(<its scene>)`.
- Never run `Tools/UIV2/Build Auth Screens` or any `UIV2FoundationBuilder` menu before B2 deletes them.
- After B6, never run `StartScreenAuthButtonsBuilder` or `GoldButtonLabelStyle`.

**Closing a batch**
1. Refresh, recompile, Console. There must be no new errors versus the baseline, and no `Ref`/`SetArray` "missing field" lines.
2. Missing-script sweep must return 0 on MainMenu and GameScene: `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount` over every GameObject.
3. EditMode tests all green.
4. Run the batch's verification flows in the **Simulator** (not the Game view), on a tall phone and on the SE viewport.
5. Bump `bundleVersion` by 0.01 (B0 = 2.64 … B10 = 2.74). From B0 on, set `PlayerSettings.Android.bundleVersionCode` and `PlayerSettings.iOS.buildNumber` to version × 100 through Player Settings or code, not YAML.
6. Make a local checkpoint commit only with the user's OK. Never push. Never stage `segreto.txt`.
7. End of Fase 10: run the `wrap-up` skill (it also runs after any batch that ends a work session).

**Snippet S** (a throwaway `Assets/Editor/Fase10Tools.cs`, deleted at the end of B10, or UnityMCP `execute_code`)

```csharp
using System.Collections.Generic; using System.Linq; using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine;
public static class Fase10Tools {
  static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
  // Opens the scene explicitly, deletes each object matched UNIQUELY by path suffix, refuses if anything outside still references it.
  public static void Delete(string scenePath, params string[] suffixes) {
    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
    foreach (var s in suffixes) {
      var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
      var hits = all.Where(t => { var p = PathOf(t); return p == s || p.EndsWith("/" + s); }).ToList();
      if (hits.Count == 0) { Debug.LogWarning($"[F10] {s}: already gone"); continue; }
      if (hits.Count > 1) { Debug.LogError($"[F10] {s}: {hits.Count} matches, skipped"); continue; }
      var go = hits[0].gameObject;
      if (PrefabUtility.IsPartOfPrefabInstance(go) && !PrefabUtility.IsAddedGameObjectOverride(go) && PrefabUtility.GetOutermostPrefabInstanceRoot(go) != go)
        { Debug.LogError($"[F10] {s}: inside a prefab instance, edit the prefab"); continue; }
      var inside = new HashSet<Object>(go.GetComponentsInChildren<Component>(true).Where(c => c != null).SelectMany(c => new Object[] { c, c.gameObject }));
      var refs = all.Where(t => !t.IsChildOf(go.transform)).SelectMany(t => t.GetComponents<Component>()).Where(c => c != null).Where(c => {
        var it = new SerializedObject(c).GetIterator();
        while (it.Next(true)) if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue != null && inside.Contains(it.objectReferenceValue)) return true;
        return false; }).ToList();
      if (refs.Count > 0) { refs.ForEach(c => Debug.LogError($"[F10] {s} referenced by {PathOf(c.transform)} ({c.GetType().Name})", c)); continue; }
      Undo.DestroyObjectImmediate(go); Debug.Log($"[F10] deleted {s}");
    }
    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
  }
}
```

Any `[F10]` error stops the batch. Fix the reference in code first, then rerun. The snippet is idempotent.

**Removing array elements** (CloseButtons, `LocalSeatBottomShift.targets`): use a `SerializedObject`, walk from the highest index down, set `objectReferenceValue = null` and then call `DeleteArrayElementAtIndex(i)`. In 2022.3 a non-null reference only gets nulled on the first delete call. Log the name of each removed element.

---

## B0. Zero-risk hygiene (docs and settings, no runtime effect)

**Delete (+ .meta)**
- `Assets/UI_SPEC_Home.md`, `Assets/UI_SPEC_PanelMazzo.md`, `Assets/UI_SPEC_PanelModalita.md`, `Assets/UI_SPEC_Collezione.md`, `Assets/ASSET_MAPPING_Collezione.md`. Keep `Assets/UI_SPEC_Tavolo.md`.
- `Assets/Scripts/UI/README_UIFoundation.md`, `Assets/UIV2/README.md`, and every `_RESERVED.md` under `Assets/UIV2` (`find Assets/UIV2 -name _RESERVED.md`, at least `Assets/UIV2/Art/_RESERVED.md`).
- `.github/copilot-instructions.md`, `PROJECT_STATUS.md`. Also remove `UI_INTEGRATION_ROADMAP.md` lines 282-284, which point to PROJECT_STATUS.

**Edits**
- `.graphifyignore`: add `Library/`, `Captures/`, `UIElementsSchema/`, `Assets/Screenshots/`. Keep the JsonDotNetWrapper line until B10.
- Player Settings: bundleVersion 2.64, Android bundleVersionCode 264, iOS buildNumber "264".

**Verify:** compile and tests. Flows: guest login, Home (smoke only).

---

## B1. Unreferenced scripts and prefabs (Block 3 plus extras)

Before deleting, run the GUID grep and a class-name grep. Both must return 0. If compile breaks, restore that file from git and report it.

**Delete (+ .meta)**
- `Assets/Scripts/Auth/LoginGateUI.cs`. The AuthBootstrapper and AppFlowManager members it used are removed in B7.
- `Assets/Scripts/Gameplay/CardShineOverlayMove.cs`, `Assets/Scripts/Gameplay/ProceduralShineStripeTexture.cs`
- `Assets/Scripts/UI/AnchorToPosition.cs`, `DebugUIRaycastLogger.cs`, `IModal.cs`, `PrivateRoomOptionsUI.cs`, `TabButtonUI.cs`, `PrimaryButtonUI.cs`
- `Assets/UIV2/Scripts/Data/` `GameResultEntryViewData.cs`, `LobbyPlayerViewData.cs`, `MailMessageViewData.cs`, `MatchmakingPlayerViewData.cs`, `RankingEntryViewData.cs`, `RewardViewData.cs`, `RoundResultEntryViewData.cs`, `ShopItemViewData.cs`
- `Assets/Scripts/Tests/TestDOTween.cs`, plus the folder if it ends up empty.
- `Assets/Prefabs/Card.prefab`, `Assets/Prefabs/UI/SectionCard_1.prefab`, `Assets/Prefabs/UI/SettingsPanel.prefab`

**Not in this batch:** `CosmeticCatalog.cs` (user decision). `MoveSelectionPanel.prefab` moves to B5 with `MoveButton.prefab`.

**Verify flows:** guest login, Home, training 1v1.

---

## B2. Editor builders (Block 9 corrected, Block 1 builders, one-shots)

This batch is editor-only. It must come before B3 to B6, because those batches delete runtime code and objects that these builders reference or would recreate.

**Gate:** Step 2.2 needs the **Shop** decision. If there is no answer yet, use the fallback in 2.2.

**2.1 Extract HomeAmbient**
- `git mv Assets/Editor/UIV2FoundationBuilder.HomeAmbient.cs Assets/Editor/HomeAmbientBuilder.cs`, and the same for the `.meta` so the GUID is kept.
- Make it a non-partial `public static class HomeAmbientBuilder` in the same namespace.
- Add `const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";` and private copies of `CreateUIObject` and `FindInScene` from `UIV2FoundationBuilder.cs`. If the compiler asks for more helpers, copy those too; never keep the partial.
- Keep the menu `Tools/UIV2/Build Home Ambient` and the explicit OpenScene.
- Keep `Assets/UIV2/Art/Generated/glow_soft_pill.png`.
- Compile.

**2.2 Shop**
- If the user keeps the Shop V2 builder, extract `UIV2FoundationBuilder.Shop.cs` the same way into `ShopV2Builder.cs`.
- If the user drops it, it goes in 2.3.
- **Fallback with no answer:** keep `UIV2FoundationBuilder.cs`, `UIV2FoundationBuilder.Shop.cs` and only the partials the compiler demands. Finish 2.3 later.

**2.3 Delete all remaining `Assets/Editor/UIV2FoundationBuilder*.cs` in one step:** `.cs` (main), `.Account`, `.Accuso`, `.AnimatedEmoticons`, `.Auth`, `.Collection`, `.ComingSoon`, `.Dealer`, `.DealerReveal`, `.DeleteAccount`, `.EmoticonQuickBar`, `.Glows`, `.HomeQuickActions`, `.HomeSettings`, `.IconSetV2`, `.Legal`, `.Motes`, `.News`, `.Online`, `.Profile`, `.QuickDeckPolish`, `.ResultsBlur`, `.RoomTable`, `.Settings`, `.Table`, and `.Shop` (per 2.2).

**2.4 Block 1 builders (17)**
- `Assets/Editor/` `AmiciBuilder`, `ClassificaBuilder`, `DeckPageBuilder`, `EmoticonKitBuilder`, `EmoticonScreenExactBuilder`, `EmoticonWireframeBuilder`, `HomeScreenBuilder`, `ImpostazioniBuilder`, `NegozioBuilder`, `PanelMazzoBuilder`, `PanelModalitaBuilder`, `PostaBuilder`, `PremiBuilder`, `ProfiloBuilder`, `DragonsHoardSprites`, `DragonsHoardThemeApplier`, `FrontendFlowBuilder` (all `.cs`).
- FrontendFlowBuilder (including its line 144) must be gone before the GameLaunchController and StartScreenV2 edits in B6.

**2.5 One-shot builders**
- `Assets/Editor/` `BackdropDismissBuilder`, `BottomNavPolishBuilder`, `QuickModeRowsCalibration`, `FrontendExpansionBuilder` together with `FrontendExpansionBuilder.Game`, `GameCanvasResolutionFixer`, `TableActionButtonsBuilder`, `TableDesignAreaBuilder`, `TableFeltBuilder`, `TablePlayerBannersBuilder`, `TableTopBarBuilder`, `IconFaceCentering`, `PanelTitlesCalibration`, `PoppinsMigration`, `UIV2FeedbackBuilder`, `UIV2ReducedGraphicsBuilder`, `UIV2ShaderBuilder`, `UIV2DesignBuilder` (all `.cs`).
- `Assets/Editor/SceneSetup/CreateCirullaScene.cs` and `Assets/Editor/Tools/FixGameManagerScenes.cs`, plus their folders and folder metas if empty.
- **Not here:** `GameSceneStrayCleanup` (B4, run first), `LobbyPrefabBuilder` (B6), `UIV2SandboxSceneBuilder` (B3), `SoundLibraryBuilder` (used by GameAudioTests), and `StartScreenAuthButtonsBuilder` / `GoldButtonLabelStyle` (user decision; neither depends on UIV2FoundationBuilder).

**2.6 UIV2MotionBuilder**
- Delete `Assets/Editor/UIV2MotionBuilder.cs`.
- In `Assets/Tests/Editor/UIV2MotionTests.cs:200-201`, call `UIV2MotionInstaller.Apply(panel)` instead. Check its namespace and that it returns the same count.

**2.7 Unused UIV2 prefabs**
- `Assets/UIV2/Prefabs/Components/` `UIV2_CollectionCard`, `UIV2_ContentPanel`, `UIV2_FlatButton`, `UIV2_IconButton`, `UIV2_ListRow`, `UIV2_ModalFrame`, `UIV2_PlayerRow`, `UIV2_SectionHeader`, `UIV2_StatTile`, `UIV2_TealButton` (all `.prefab`).
- In the same step, delete the test `FlatButtonKeepsItsHitTargetAndClickActionAcrossReopen` (`UIV2DesignTests.cs` ~152).
- `UIV2ModalFrame.cs` becomes orphaned and goes in B9.

**2.8 Comments and skills**
- Fix the comments `UI51TableBuilder.cs:263` and `UIV2BottomNav:48`.
- Update both copies of `unity-builder/SKILL.md` (`.claude/skills/` and `.agents/skills/`):
  - Remove line 22 (`HomeScreenBuilder.Build()`).
  - Change line 31: set `pixelsPerUnitMultiplier = nativeH/targetH` directly, without naming the deleted `AddSlicedImage`.

**Verify:** compile and tests. The `Tools/UIV2` menu now shows only Home Ambient, plus Shop if kept, plus the start-screen stylers. Run no builder. Flows: Home (smoke).

---

## B3. HomeScreen scene, preview scenes, legacy networking (Blocks 1 and 2, legacy stack)

**Pre-check**
- GUID-grep each script below. Allowed hits are only `HomeScreen.unity`, `TESTPHOTN.unity`, the 5 preview scenes, and the Kit/Friends prefabs being deleted.
- Do **not** touch `SelectableToggleItem` or `SelectableToggleGroup`; they are also in MainMenu and GameScene.

**Delete (+ .meta)**
- Scene `Assets/Scenes/HomeScreen.unity` (confirm it is not in Build Settings).
- `Assets/Scripts/UI/` `PanelAmiciController`, `PanelPremiController`, `PanelPostaController`, `PanelClassificaController`, `PanelImpostazioniController`, `PanelMazzoController`, `PanelModalitaController`, `HomeNavBarController`, `DeckPageController`, `OpenPanelShortcut`, `StubActionButton` (all `.cs`).
- `Assets/Scripts/UI/Kit/` `UICollectionGrid`, `UICollectionCard`, `UIEquippedRow`, `UIHeaderWidget`, `UIScreenRoot`, `UITabsRow` (all `.cs`). **Keep** `Kit/UIBottomNavBar.cs`.
- `Assets/Prefabs/UI/Kit/` `UI_BottomNav4`, `UI_CollectionCard`, `UI_CollectionGrid4`, `UI_EquippedRow3`, `UI_Header`, `UI_ScreenRoot`, `UI_Tabs3` (all `.prefab`).
- `Assets/Scenes/TESTPHOTN.unity`.
- `Assets/UIV2/Tests/` scenes `UIV2_Sandbox`, `UIV2_HomeV2_Preview`, `UIV2_CollectionV2_Preview`, `UIV2_ProfileV2_Preview`, `UIV2_ShopV2_Preview` (all `.unity`).
- `Assets/UIV2/Tests/` scripts `UIV2SandboxBootstrap`, `HomeV2PreviewBootstrap`, `CollectionV2PreviewBootstrap`, `ProfileV2PreviewBootstrap`, `ShopV2PreviewBootstrap` (all `.cs`), then the folder if empty.
- `Assets/Editor/UIV2SandboxSceneBuilder.cs`.
- `Assets/UIV2/Scripts/Screens/FriendsScreenController.cs`, `Assets/UIV2/Scripts/Screens/FriendRowView.cs`, `Assets/UIV2/Scripts/Data/FriendViewData.cs`.
- `Assets/UIV2/Prefabs/Screens/FriendRow.prefab`, `Assets/UIV2/Prefabs/Screens/FriendsScreenV2.prefab`.
- `Assets/Scripts/Networking/` `NetworkManager`, `RoomManager`, `SceneLoadManager`, `PlayerDataManager`, `NetworkTypes`, `NetworkTestUI`, `GameManager` (all `.cs`).

**Code edits**
- `Assets/Scripts/Auth/AccountDeletionService.cs` (UTF-8 LF): delete lines 173-174, the `FindObjectOfType<Project51.Networking.PlayerDataManager>()` call and the `ResetPlayerData()` call. **Keep** the keys `"PlayerData_V1"`, `"PlayerNickname"`, `"PlayerId"` at line 44, so old installs are still wiped.
- Comments that name the deleted classes: `PhotonAuthConnector:50`, `GameModeService:27`, `TurnController:526`, `CardView:512`, `CardViewManager:1183`.
- **Build Settings:** remove the TESTPHOTN row explicitly:
  `EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(s => !s.path.EndsWith("TESTPHOTN.unity")).ToArray();`
  Confirm the remaining enabled rows and their order are unchanged. The row was disabled, so no build index shifts.
- Leave the PhotonServerSettings RpcList as it is.

**Verify flows:** guest login, Home, training 1v1, online search/cancel, private room create/join.

---

## B4. Inactive scene objects with no code ties (Block 4 corrected)

Needs B2: the FrontendExpansionBuilder pair and the EmoticonQuickBar builder are gone.

**4.1 GameSceneStrayCleanup**
- Open `Assets/Scenes/GameScene.unity` explicitly.
- Run `Tools/Dragons Hoard/Cleanup Stray PanelPersonalizzaOverlay in GameScene` once and save.
- Read the Console to see what it removed, then confirm `PanelPersonalizzaOverlay` is gone (run S on it if needed).
- Delete `Assets/Editor/GameSceneStrayCleanup.cs`.

**4.2 MainMenu:** `Fase10Tools.Delete("Assets/Scenes/MainMenu.unity", …)`
- `Canvas_Background`
- `hierarchyDumper`, then delete `Assets/Scripts/DEBUG/HierarchyDumper.cs` and the `Assets/Scripts/DEBUG` folder.
- `LoginPanel/Design`
- `LegalV2/Design`, `LegalV2/Dim`
- `DeleteAccountV2/Design`. **Keep** `DeleteAccountV2/Dim`.
- `LoadingView/Background`, `LoadingView/DesignArea`. Afterwards GUID-grep `LoadingProgressBar`; if it has 0 hits, delete its script.
- `FoilTextureGenerator`, then delete `Assets/Scripts/UI/UICardFoilNoiseTexture.cs` after the GUID check.
- `HomeScreenV2/UI51/Rewards`, `HomeScreenV2/UI51/Ranking`, `HomeScreenV2/UI51/Settings` (the old direct children). In `Assets/UI51/Editor/UI51HomeBuilder.cs:195`, delete the `foreach … HideChild(c, old)` line.
- `QuickModeV2/PanelFrame`, `ModeRowGroup`, `DifficultyPillGroup`, `QuickDeckV2/PanelFrame`, `DeckSelectionGroup`. Use the full suffix if a bare name is not unique.

**4.3 GameScene:** `Fase10Tools.Delete("Assets/Scenes/GameScene.unity", …)`
- `TurnIndicator`, then delete `Assets/Scripts/UI/TurnIndicator.cs`.
- `Design/ConnectionNotice` (under GameSocialV2). First delete `UI51MomentsBuilder.cs:145-148` (the `Find("Design/ConnectionNotice")` block), then delete `Assets/UIV2/Scripts/Core/ConnectionNoticeV2.cs`.
- `EmoticonQuickBar`
- `TableTopBar/Background`, `TableTopBar/SettingsButton`
- `AccusoPanel`, then delete `Assets/Scripts/UI/AccusoPanelController.cs`, `AccusoCardSlot.cs`, `AccusoUIBridge.cs`. Grep the string `"AccusoUIBridge"` first, because there is reflection across assemblies.
- The 12 inactive leftovers under `TableActionButtons`. List the inactive children live; the reference guard decides. They include `AccusoWindowGlow`.
  - First remove the `accusoGlow` field from `TableActionButtonsController`.
  - In `UI51TableBuilder.cs`:
    - line 686: delete `UI51Build.Ref(so, "accusoGlow", null);`
    - line 272: drop the deleted names from the required `accuso` array.
    - line 279: update the error message.
  - Keep `EmojiButton`, `AccusoButton`, `AccusoWindowRing`, `AccusoWindowBadge` and `AccusoWindowPrompt` unless they are among the 12.

**Moved out of this batch:** the Dims of SearchMatch, LobbyHost and LobbyGuest, the Blur/Veil/Dim of RoundResults, and the Dim of MatchResults all move to B6, together with their Design and code edits.

**Verify flows:** guest login, Home, Modalità panel (mode and deck), settings, training 1v1 (top bar, action buttons, accuso window), results screens, online search/cancel (connection toasts), news from the start screen (loading view).

---

## B5. Gameplay dead code (selection/drag, captured piles, RoundEndPanel)

**5.1 Selection and drag** (run the steps in this order)
- `Assets/Scripts/Gameplay/CardViewManager.cs` (UTF-8 CRLF). Delete:
  - `EnterSelectionMode`, `TryConfirmSelection`, `CancelSelection`, `OnTableCardClicked` and its subscription at ~1288, `UpdateTableCardsInteractivity`, `OnHumanCardDragReleased`, `SetCardViewPrefab`, `SetTurnController`, `GetHandCardScale`, `SuitToIndex`, `ShowSequentialCaptureHints`, `StopAllHintAnimations`, `GetSpriteForRank`.
  - Fields `isSelecting`, `selectionPlayedCard`, `selectionTableCards`, `helpShownForCurrentSelection`, `enableSpriteDebug`, `playSound`, `playSoundVolume`.
  - The sprite heuristic: `explicitMappings`, `cardSprites`, `EnsureCardSpritesLoaded`, `PopulateSpriteLookup`, `spriteLookup`.
  - `GetCapturedPileDesignOffset`, `GetActiveCardViews`.
  - **Keep** `currentlyHighlightedCards`, `chooserCard`, `ClearArrowsAndHighlights`, `HighlightAlternative`.
- `Assets/Scripts/Gameplay/CardView.cs` (UTF-8 LF). Delete:
  - `OnDragReleased`, `allowDrag`, `OnMouseDrag`, the whole `OnMouseUp`, `dragOffset` with the camera block at ~242-249, `isDragging` (simplify its guards), `PlayHintBounce`, `StopHintBounce`, `HintBounceCoroutine` and the cleanup at ~714-717, `ClearTemporaryOverlay`, `HasMoveHint`.
- `Assets/Scripts/Gameplay/TurnController.cs` (UTF-8 LF). Delete:
  - `OnPlayerDragPlay`, the `moveSelectionUI` field (line 16), `SetCardViewManager`, `DealNewHands`, `EndRound` (~1843-1872), `OnPlayerConfirmMove`, and the QA `[ContextMenu]` block (~535-573).
  - **Keep** `SetupScenarioForCurrentPlayer` and `StartNewGame`. MattaVisualHintsTests and the play-mode recipes call them by reflection.
- `Assets/Scripts/Core/Rules51.cs` (**Latin-1 CRLF, byte-safe**). Delete:
  - `TryGetMoveFromSelection` (~115-158) and `GetMatchingMovesFromSelection` (~159-226). Their only callers were removed above.
  - `GetSumTo15CapturesWithEffectiveValue`, plus the stray `/// <summary>` at ~379.
- Tests, same step:
  - Delete `Assets/Tests/Editor/PlayerSelectionTests.cs` (2 tests).
  - Delete `#region TryGetMoveFromSelection Tests` in `Rules51CoreTests.cs` (~323-370, 3 tests).
  - Delete only the assert `Assert.IsFalse(Rules51.TryGetMoveFromSelection(...))` at `RulesDecisionsTests.cs:30`. The `GetValidMoves` asserts around it still cover the rule.
- `MoveSelectionUI`:
  - Delete `ShowMoves`, `buttonPrefab`, `invalidMessageColor`.
  - Reserialize GameScene and GUID-grep `MoveButton.prefab`.
  - Delete `Assets/Prefabs/MoveSelectionPanel.prefab` and `Assets/Prefabs/MoveButton.prefab`.
- `CardAnimationController`: delete `PlayDealtCardsReveal`, `FlipCard`, `flipDuration`.
- Delete `DealerAccusoRevealController.ShowMessage`, and `GameSceneInitializer.GetConfig` / `IsMultiplayerMode`.
- `Assets/Scripts/Core/MatchConfig.cs`: delete the class `TrainingGameModeProvider`; `GameSceneInitializer:321` uses `SinglePlayerProvider` instead.
- `MatchConfig.DeckBackId`: delete it everywhere, including `MatchConfigStorage.Clear` line 207 and its test.
- Gameplay `.asmdef`: remove the dangling reference GUID `dd6c30c0…` and the `TMP_PRESENT` define.
- Reserialize `Assets/Prefabs/CardViewManager.prefab`.

**5.2 Captured piles** (`renderWorldPiles` is always false)
1. Code first:
   - `TurnController`: field at ~21, ~474-477, ~609, ~1641, ~1789, ~1967.
   - `NetworkGameController` ~87-89.
2. Remove the child `CapturedPileManager` from `Assets/Prefabs/CardViewManager.prefab` with `LoadPrefabContents`.
3. `RemoveUnusedOverrides` on the CardViewManager instance in GameScene.
4. Delete `Assets/Scripts/Gameplay/CapturedPileManager.cs` and `PlayerCapturedPileView.cs`. This covers `CapturedPileManager.GetPileView`.

**5.3 RoundEndPanel** (`GamePresentation.ShowRound` is always subscribed in GameScene, so the panel is never shown)
- `TurnController.ShowRoundEndPanel` becomes:
  `if (!GamePresentation.ShowRound(gameState, OnRoundEndContinue, OnRoundEndMainMenu)) Debug.LogError("[TurnController] No round results view");`
- `OnRoundEndMainMenu` calls `GamePresentation.CloseResults();` before `GoToMainMenu`.
- Delete the `roundEndPanel` field.
- Scene: `Delete(GameScene, "GameCanvas/RoundEndPanel")` (the whole subtree, 9 objects).
- Delete `Assets/Scripts/Gameplay/RoundEndPanel.cs`, `PlayerScoreRow.cs`, `RoundEndPanel_README.md`, and `Assets/Prefabs/PlayerScoreRow.prefab`.

**Verify flows**
- Training 1v1, a full match with each deck (Napoletano, Barocco): deal, captures, chooser for alternatives, accuso, round results, Continue, next round, match results, Home.
- "Menu" from round results closes the results and returns Home.
- Training 2v2 and 1v3, private room create/join (an online match through to round end), results screens.
- If MattaVisualHintsTests fails here, delete it now (planned for B8).

---

## B6. UIV2 Design subtrees still wired to code (Blocks 5 and 6 corrected, plus additions)

Each sub-step is its own compile, scene delete and quick check, done in this order.

**6.1 Online windows**
- On `RoomFlowV2` (MainMenu), remove from `CloseButtons` only the 6 entries that point inside `{SearchMatch,LobbyHost,LobbyGuest}/{Design,Dim}` (fileIDs 1781079517, 686466960, 2115766564, 257876999, 772821517, 1365050716). Use the array snippet and log the names.
- **Keep** the CreateRoom entries [0..3] (Block 8).
- Optional null guard at `RoomFlowV2:101`.
- Delete `OnlineFlowV2/SearchMatch/Design`, `…/SearchMatch/Dim`, `OnlineFlowV2/LobbyHost/Design`, `…/LobbyHost/Dim`, `OnlineFlowV2/LobbyGuest/Design`, `…/LobbyGuest/Dim`.
- Delete `Assets/UIV2/Scripts/Components/LobbySlotRowV2.cs`.
- `CodeCellsV2` stays (Block 8).

**6.2 Legacy online views**
- `GameLaunchController.cs` (UTF-8 CRLF):
  - Remove the WaitingRoom/JoinRoomPopup/MatchmakingStatus fields and their uses.
  - Remove the fake matchmaking: lines 34-38, 45, 215-219, 236-265, 504, and `using System.Collections`.
  - Keep `Launch()` and the `modalityPanel` events (Block 7).
- Delete the MainMenu object `LegacyOnlineViews`.
- Delete `Assets/Scripts/UI/WaitingRoomUI.cs`, `JoinRoomPopupUI.cs`, `MatchmakingStatusUI.cs`, `PlayerSlotUI.cs`.
- Delete `Assets/Prefabs/UI/WaitingRoomPanel.prefab`, `JoinRoomPopup.prefab`, `PlayerSlot.prefab`.
- Delete `Assets/Editor/LobbyPrefabBuilder.cs` and `Assets/Scripts/LOBBY_SETUP_README.md`.
- `UIV2DesignSystem.cs:109`: drop the `LegacyOnlineViews` name check.

**6.3 Register screen**
- `AuthScreensV2.cs` (UTF-8 LF): remove `StrengthBars`, `RegisterGlow`, `UpdateStrengthBars`, `Strength`, `StrengthOn`, `StrengthOff`, `GlowAlpha`. Keep `LooksLikeEmail`.
- Delete `RegisterPanel/Design`.

**6.4 Start screen and NewsV2** (NewsV2 is already unreachable; the old Block 6 "reachable" claim was wrong)
- `StartScreenV2`: remove the button and settings fields and the `Guest` / `Register` / `Options` handlers. Keep the View writes. Keep `OnPlayPressed` wiring at 43/90 (Block 7).
- Delete the StartScreenV2 children `DesignArea`, `Background`, `StartMotes`, and the `NewsV2` root.
- Delete `Assets/UIV2/Scripts/Core/NewsScreenV2.cs` and `Assets/UIV2/Scripts/Components/NewsItemViewV2.cs`.
- Remove the NewsV2 entries at `UIV2DesignSystem.cs:52` and `UIV2MotionInstaller.cs:67`.

**6.5 Settings (Home)**
- `SettingsV2Integration.cs` (UTF-8 LF): remove the 7 legacy fields, the 3 `Ensure*` methods, `accountHeader`, `footer`, `rowsAfterAccount`, and the legacy-layout half. Keep `panelFrame` (now the UI51 Page) and `MaskEmail`.
- `UI51MetaBuilder.cs`: delete 182-185 and 80-81.
- Delete `SettingsV2/PanelFrame` and `SettingsV2/DimBackground`.

**6.6 In-game settings**
- **First** make `AnimatedModalV2` Frame-optional: `Initialize` succeeds without Frame, and Open/Close animate whatever exists. Behaviour with a Frame set must not change.
- **Then** set InGameSettings' `AnimatedModalV2.Frame` to null in GameScene.
- `InGameSettingsV2.cs` (UTF-8 LF): remove the legacy fields, `Abandon`, `Ensure*`, and the `LeaveDialog == null` fallback. Keep `LeaveMessage`, `OpenLeave`, `CloseLeave`, `ConfirmLeave`, `Hide`, `Blur`.
- Edit `I5ReducedGraphicsTests.cs:349-354`.
- Delete the in-game settings `Panel/Design`, `Veil`, `Dim`.

**6.7 Results**
- `MatchResultsV2.cs` (UTF-8 CRLF): remove the old fields and every `View == null` path. **Keep** `ConfettiRoot`, `PlayConfetti` and the Confetti expression (an I5 test uses them).
- Delete `UI51ResultsBuilder.cs:63-64`.
- Delete `RoundResults/Design`, `RoundResults/Blur`, `RoundResults/Veil`, `RoundResults/Dim`, `MatchResults/Design`, `MatchResults/Dim`.
- Delete `Assets/UIV2/Scripts/Components/ResultRowV2.cs`.

**6.8 Dealer roulette**
- `DealerRouletteController`: `PlayRoulette` becomes a thin entry point into the wheel. Remove the legacy fields and `legacyOnly`. Keep `Continue` (UnityEvent), `Hide`, `PlayWheel`, `Timing`.
- `UI51TableBuilder.cs`:
  - delete 1373
  - gate at 1379 becomes `roulette == null || bg == null …`
  - keep only the `ui51Wheel` wiring at 1545-1550.
- `K7FlowTests.cs`: delete `RouletteSpin` (56-89) and fix the Wheel test (100-112).
- In the same change, delete `DealerRoulette/Design`, `DealerRoulette/Blur`, `DealerRoulette/Veil`. They are **active**.

**6.9 Table top bar**
- `TableTopBarController`: remove `handText`, `cardsLeftText` and their writes (57-66).
- Delete `TableTopBar/HandText` and `TableTopBar/CardsLeftText`.
- Remove `UIV2DesignSystem.cs:72-73` and the names at `UI51TableBuilder.cs:90`.

**6.10 Emoticons, then AccusoImpact**
- `GameSocialV2.cs` (UTF-8 LF): remove `EmoticonPanel`, `EmoticonButtons`, `Close`, `Hint`, `Bubbles`, `BubbleImages`, `BubbleNames`. The accuso path becomes UI51-only.
- Delete `GamePresentationV2/Emoticons` and `GamePresentationV2/Design/Bubble0` through `Bubble3`. **Do not delete `GamePresentationV2/Design`**: AccusoImpact lives in it.
- `LocalSeatBottomShift.targets`: remove [7] first, **then** [2], so indices don't shift. Log both names before removing.
- After the GUID check, delete `Assets/UIV2/Art/Emoticons/Emoticons.controller` and its 6 orphaned `.anim`.
- `AccusoImpactV2`: remove `Group`, `Fist`, `Burst`, the legacy tails, `AppendSlam`, the duration constants, `RestCards`. Keep `CaptionShadow`.
- `CollectionCosmeticsV2`: remove `Preview`.
- Delete `…/AccusoImpact/Visual` in GameScene and the added object `AccusoImpact` in MainMenu.
- Rewrite or delete the matching `K6FeedbackTests` case.

**6.11 Player banners**
- `PlayerBanner.cs`: becomes an empty marker `MonoBehaviour`. Keep the file and GUID.
- `PlayerBannerManager`: remove the legacy branches (117-124, 166-169, 181), `GetBannerForPlayer`, `roundedFillSprite`.
- Delete the 8 inactive children of each `Banner_*` root and the 4 `Background` objects.
- **Keep** the `Banner_*` roots, `seatAvatars`, and the method name `SetDealerIndicatorForPlayer`.

**6.12 Fields that UI51 builders set to null**
- Remove `LoginBelowGuest`, `OpenOnWeb`, `Fan`, `StatTile.icon`, `RoundExit` together with their `UI51Build.Ref(so, "<name>", null)` lines. Grep them in `Assets/UI51/Editor`.
- The GuestPanel moves to B7, so AuthUIController is edited only once.

**Verify flows**
- Guest login; register screen (type a password); login; Home; settings (Home and in-game, including the leave dialog).
- Online search/cancel; private room create/join; **invite from Friends** (CreateRoom close buttons still work).
- Training 1v1 and 2v2: dealer wheel at 2 and 4 players, emoticons, accuso impact, banners and dealer indicator.
- Results screens (round and match, confetti); news from the start screen; Collection cosmetics.

---

## B7. Auth and network members, plus fixes

**7.1 AuthBootstrapper.cs** (UTF-8 CRLF)
- Remove:
  - `authUIComponent` (MainMenu wires it to a CanvasScaler; that reference simply goes), `_authUI`, the Awake cast (118-122)
  - `RegisterAuthUI`, `HandleDisplayNameChanged`, `ProtectAccount`, `RetryAuthentication`, `OnAccountLinked`, `OnAccountLinkFailed`
  - the `CheckAccountLinkStatus` call (589-593), `ShouldShowTapToEnter`
  - the events `OnAuthStateChanged` and `OnAuthError` (81-82, 631, 659)
  - the `PhotonApplicationNotFound` branch (503-522)
  - the PII log at 465.
- Remove `photonAppIdOverride` (42, 665-667). **Check first** that it is empty in MainMenu; if it holds a value different from `AppIdRealtime`, stop and report.
- Keep `StartAuthentication`.
- Delete `Assets/Scripts/Auth/IAuthUI.cs` and `Assets/Scripts/Auth/NativePlatformAuth.cs`. The missing AppleAuth package breaks iOS builds, and the Android success path is empty.

**7.2 PlayFabAuthService.cs** (**Latin-1 CRLF, byte-safe**)
- Remove:
  - `LinkGoogle`, `LinkApple`, `CheckAccountLinkStatus`, `CheckRegistrationStatus`, `RegisterWithUsernameEmailPassword`, `IsAccountLinked` (also 487, 612-613)
  - the 5 unused events and the 2 duplicate `using System;`
  - `HasEverLoggedIn`, `Mark…`, `Clear…`, `HAS_EVER_LOGGED_KEY`.
- Make `IS_REGISTERED_KEY` `internal` and `GetUserFriendlyError` `internal static`.
- Replace the TODO at 715-734 with an accurate pointer.
- Header at line 19: the stats setting must be **OFF**.
- PII logs at 248, 534, 546, 591, 620.
- Keep `"Project51_HasEverLogged"` in `AccountPrefKeys`.

**7.3 AuthUIController.cs** (**Latin-1 CRLF, byte-safe**)
- Remove:
  - `OnDestroy` / `HandleBootstrapperReady`, `IsClosingToTapToEnter`, `IsUserRegistered`, `ShowError(string)`
  - `Logout` and `logoutButton`
  - `accountPlayerNameText`, `accountStatusText`, `UpdateAccountPanelInfo`
  - `registerBackButton`, `loginBackButton`, `loadingOverlay`, `loadingText`
  - the GuestPanel handling and its fields. `ShowAuthUI` then shows AccountPanel only for real logins.
- `KEY_IS_REGISTERED`, `MarkRegisteredLocal`, `IsRegisteredLocal`: switch to `PlayFabAuthService`, **only if** both constants hold the same string. Otherwise keep reading the old key.
- **Keep** the `OnPlayPressed` event (Block 7).
- Scene: delete `Canvas_Login/GuestPanel`.

**7.4 ProfileService.cs** (**Latin-1 LF**)
- Delete `GetPublicProfile`, `PublicPlayerProfile`, `UpdateNickname`, `IncrementStatistic`, `UpdateStatistic`, `CalculateLevelFromXP`, `SelectedDeck` / `TitleId` / `SetTitle` and their keys, `OnError`, the Trophies stat.
- `STAT_XP` becomes the single const used by `FriendsService:58` and `LeaderboardService:26/91`. The string value stays identical.

**7.5 PhotonAuthConnector.cs** (UTF-8 CRLF)
- Delete `SetNickname`, `CurrentNickname`, `IsConnected`, `OnDisconnectedEvent`, `OnDisconnectedWithCause`.
- **Bug fix** in `OnDisconnected(cause)`:
  `bool wasConnecting = <connecting flag>; <connecting flag> = false; if (wasConnecting && cause != DisconnectCause.DisconnectByClientLogic && cause != DisconnectCause.ApplicationQuit) OnConnectionFailed?.Invoke(...)`
- Keep the 30 s timeout.
- Add one EditMode test:
  - create the component and set the connecting state;
  - `OnDisconnected(ServerTimeout)` raises `OnConnectionFailed` once;
  - `DisconnectByClientLogic` raises nothing.

**7.6 PlayerProgressLocal.cs** (**Latin-1 LF**)
- Delete the `pendingExp` members, `Start`, `OnLevelUp`, `ExpToNextLevel`, `LevelProgress`, `ResetAllProgress`, and the call at `AccountDeletionService:172`.
- Keep `"progress_level"` and `"progress_pendingExp"` in `AccountPrefKeys`.

**7.7 AppFlowManager.cs** (UTF-8 CRLF): delete `GoToGame` (its LoginGateUI caller went in B1), `GoToLobby`, `SceneExists`, `ReloadCurrentScene`, `SCENE_LOBBY`, `SCENE_WAITING_ROOM`, `using System`.

**7.8 MatchmakingManager.cs** (**Latin-1 LF**)
- Delete `OnPlayerJoined` and `OnPlayerLeft` (WaitingRoomUI went in B6).
- Add `PropFormat` / `PropTarget` consts with unchanged string values, and use them at `HomeConnectionWatcher:108-109` and `RoomFlowV2:405`.
- Make `RoomCodeLength` a const and use it in `RoomFlowV2.NormalizeCode` / `IsValidCode`.
- Remove the redundant null checks at 436 and 460.
- Fix the `RefusedByServer` comment.

**7.9 Rejoin window:** `NetworkGameController.RejoinWindowSeconds` and `ModerationService.SeatSeconds` derive from `MatchmakingManager.RejoinWindowMilliseconds`. The value must stay 60 s.

**7.10 RewardsService**
- Stop sending `avversario` (line 122), and edit `Server/CloudScript/test.js` lines 183, 194, 200 and 404. Run `node Server/CloudScript/test.js`. No CloudScript redeploy is needed.
- Delete the `ServerReward` fields `postaNuova`, `limitePartite`, `abbandonoNonValido`, `ospite`.
- The `errore` path at line 179 becomes `new ServerReward()`.

**7.11 NetworkGameController.cs** (UTF-8 CRLF)
- Delete the state dump at 764-771.
- Put lines 632, 749 and 774 behind `logNetworkMoves`.
- Remove the redundant PhotonView check in Awake (194-198).

**Verify flows**
- Guest login on a fresh install (clear PlayerPrefs first).
- Real-account login and register.
- Settings, including account delete with a real login (guests must not see it).
- Online search/cancel, then start search with the network off: the connection-failed overlay or toast appears.
- Private room create/join and **rejoin** within 60 s.
- Invite from Friends.
- Training 1v1: XP awarded on results.
- Online match end: coins shown on the results screens.

---

## B8. Test suite cleanup (test files only, plus their two code pairs)

1. Record the per-file test count first.
2. Move the unique tests from `Rules51ExtraTests`, `Rules51RestoredTests` and `PunteggioManagerTests` into `Rules51CoreTests`. Add `Assert.IsNotEmpty` at `Rules51CoreTests:161`. Keep RoundManager `TotalHands_Is_Three`.
3. Delete the duplicate files `Assets/Tests/Editor/AceAndCaptureRulesTests.cs`, `Rules51ValidMovesTests.cs`, `CirullaRulesTests.cs`, `AccusiCheckerTests.cs`, `MattaVisualHintsTests.cs`.
4. Replace the accusi tests with one `[TestCase]` table (keep the file `MattaAccusiCombinationsTests.cs` byte-safe; it is Latin-1 CRLF).
5. Delete `BackdropBlur.BoxBlur` and its test (I5 / InGameSettingsTests).
6. Delete `GameFeedback.SetParticlesEnabled` and its K6 test. Move the event check into I5.
7. Delete `UIV2DesignTests.cs:16-35`.
8. Use direct calls instead of reflection by name wherever the Editor test assembly can see the type. Keep reflection for `StartNewGame` and `SetupScenarioForCurrentPlayer` if used elsewhere.
9. Optional: one shared `TestUtil`, and drop the useless `#if UNITY_EDITOR` in Editor tests.

**Verify:** all green. The count drops only by the deleted duplicates, and every moved test is present. Flows: training 1v1 (smoke).

---

## B9. Simplifications and refactors

1. **`GameStateSerializer`**
   - First write a test pinning the exact string `NetworkGameController.SerializeMove` produces today, plus the round-trip.
   - Then move `SerializeMove` / `DeserializeMove` into `GameStateSerializer` with the format unchanged.
2. **TurnController:** one `InvokeNetwork` helper with a cached `Type` only (no cached instance).
3. **CameraResponsiveFit:** direct `GetComponent`, null-check, `AddComponent`, `Apply()`. No `??` on Unity objects. CardViewManager only calls Apply, never adds the component. Keep reflection wherever the caller sits in the Gameplay/Core asmdef and cannot see Assembly-CSharp.
4. **`CardViewManager.SeatOf(player, local, count)`.** Leave `TurnController` 1067 and 1521 as they are.
5. **`MatchRules.ForFormat`**, used in the 3-4 places that rebuild mode rules.
6. **Small dead UIV2 API**
   - Delete `UIV2ModalHost.CloseCurrent`, `UIV2Root.FxHost` (property only), `HomeScreenV2.SetFriendsBadge`, `HomeScreenV2.SetBackgroundVideoTexture`.
   - `UIV2AnimationHooks`: if its GUID has 0 hits, delete the file; otherwise delete only `PlayHide`, `PlaySelected`, `PlayUnlock`, `selected`.
   - Delete `Assets/UIV2/Scripts/Components/UIV2ModalFrame.cs`, then `Assets/UIV2/Scripts/Core/IUIV2Modal.cs`. `UIV2ModalHost` takes `AnimatedModalV2` directly.
7. **VersionLabelV2:** on MainMenu, swap `VersionLabelV2` for `UI51VersionLabel` (Editor snippet; same text in Play), then delete `Assets/UIV2/Scripts/Components/VersionLabelV2.cs`.
8. **SafeArea rename:** `git mv Assets/Scripts/UI/SafeArea.cs Assets/Scripts/UI/SafeAreaFitter.cs` plus the `.meta` (the class is already `SafeAreaFitter`; the GUID is kept). Remove or turn off the SafeArea `debugLog` flags.
9. **UI51 builder dedupe:** the Profile/Collection `crit-*` fields and the TopBar resources path.
10. **Trophies order:** after `UI51MetaBuilder:462`, add `var t = account.Find("Trophies"); if (t != null) t.SetAsLastSibling();`.
11. **UI51 builders:**
    - Dedupe `HasDirtyScene`, `Clip` and `Polyline`.
    - Delete the migration shims and any `HideChild` lines left pointing at deleted objects.
    - Optional, and adds a menu: `Tools/UI51/Build Tutto (in ordine)`.
    - **Last:** move the shared helpers into `UI51Build`.
12. Stale comments naming deleted builders (`UIV2DesignSystem:72` and others; grep the deleted class names).

**Verify:** a full regression covering all ten flows.

---

## B10. Assets, packages, repo, docs archive

**10.1 Assets** (GUID check each, delete + .meta)
- `Assets/Art/New Material.mat`, `Assets/Art/New Render Texture.renderTexture`, `Assets/Art/Generated/*` (FlatNavy included; TableFeltBuilder is gone).
- The 3 unused LiberationSans materials in `Assets/UIV2/Art`.
- The UI51 rings and `sun_emblem`, plus the import lines 74-76 in `import_handoff_art.py`.
- The DragonsHoard `import_manifest.json`, `home_flame_b`, `home_vines`.
- The material orphans left by B4/B6. Drop Shadow mats only with 7a.
- `Assets/UIV2/Prefabs/Components/UIV2_AvatarBadge.prefab`, `UIV2_GreenSmallButton.prefab`.
- `Assets/Screenshots/`: delete it, add it to `.gitignore`, and fix `docs/ui/k4-shaders-plan.md:37`.
  - Set the EditorPref `MCPForUnity_ScreenshotsFolder` to a folder outside Assets (for example `Captures`).
  - **Tell the user:** this pref is shared by all their projects.
- Folders:
  - `Assets/UI Toolkit`
  - JsonDotNetWrapper (then drop its `.graphifyignore` line)
  - the empty `Assets/Scripts/{Data,Editor,FX,Gameplay/FX,Multiplayer}`
  - the empty UIV2 `GameHUD` and `Modals` folders.
- MainMenu LightingData: with MainMenu open, Lighting → Clear Baked Data, then save.

**10.2 Packages** (Package Manager)
- Remove `com.unity.visualscripting` (and its `.gitignore` block), `com.unity.timeline`, `com.unity.collab-proxy`, `com.unity.ide.rider`.
- Replace `com.unity.feature.2d` with `com.unity.2d.sprite` 1.0.0. First check that no `.psd`/`.psb` `.meta` uses the PSD ScriptedImporter.
- Pin `ParticleEffectForUGUI.git#4.11.4`.
- **Last**, pin `unity-mcp…?path=/MCPForUnity#v10.2.0`. It reloads the MCP bridge, so expect the link to drop.

**10.3 Git (local only)**
- `git rm -r --cached -q Library`
- `git rm --cached -- '*.csproj' '*.sln'`
- Untrack `Captures/` and `UIElementsSchema/`.
- `.gitignore`: add `Library/`, `*.csproj`, `*.sln`, `Captures/`, `UIElementsSchema/`, `Assets/Screenshots/` (and `Assets/Screenshots.meta`).
- Confirm `segreto.txt` is ignored.
- **Warning:** `main` still tracks Library. Checking out across this commit deletes PackageCache files. Close Unity before any branch switch.

**10.4 Docs**
- `git mv` the roadmap (`UI_INTEGRATION_ROADMAP.md`), `docs/ui` and the backlog history into `docs/archivio/`.
- Update wrap-up step 4 in both `wrap-up/SKILL.md` copies.
- Update the memory entries that name these files.
- Delete `Assets/Editor/Fase10Tools.cs`.

**Verify:** packages resolve; compile; tests; full regression of all ten flows; an Android development build succeeds.

**End of Fase 10:** run the `wrap-up` skill (tests, version, backlog, Graphify refresh, memory) and mark FASE10_PULIZIA.md done.

---

## Conflicts found and how they are resolved

1. **Rules51 selection tests.** The plan was to retarget the `TryGetMoveFromSelection` tests to `GetMatchingMovesFromSelection`, but that method is also being deleted. Its only callers were `OnPlayerDragPlay`, `OnTableCardClicked` and `TryConfirmSelection`. Resolution (B5):
   - delete both methods;
   - delete `PlayerSelectionTests.cs` and the 3-test region in `Rules51CoreTests`;
   - delete only the one assert at `RulesDecisionsTests:30`.
2. **NewsV2 "reachable" (Block 6) is wrong.** It is unreachable, so it is deleted with no rewiring (B6.4). The start-screen News flow is still checked.
3. **Builder keep-list (Block 9) is wrong.**
   - Every `UIV2FoundationBuilder*` file goes.
   - HomeAmbient is extracted into `HomeAmbientBuilder.cs`.
   - Shop depends on the user (fallback in B2.2).
   - The start-screen stylers wait for a user decision and must not run after B6.
4. **Block 10 keep-list is wrong.**
   - `RoundEndPanel_README.md` goes with RoundEndPanel (B5).
   - The 26 Networking docs become a user decision, as one folder.
5. **Objects listed in both Block 4 and Block 5** (the Search/Lobby Dims, RoundResults Blur/Veil/Dim, MatchResults Dim) are scheduled with their Design and code edits in B6. LegalV2/Dim is added to B4; DeleteAccountV2/Dim stays.
6. **CloseButtons:** only the 6 Search/Lobby entries go. CreateRoom [0..3] stay until Block 8.
7. **InGameSettings Frame:** `AnimatedModalV2.Initialize` returns false without a Frame, which would turn Open and Close into no-ops. Frame-optional therefore lands before the Frame is nulled (B6.6).
8. **Confetti** is live (I5 test) and kept. **GamePresentationV2/Design** is kept because AccusoImpact lives there. **PlayerBanner** stays as an empty marker because the `Banner_*` names are load-bearing. **HighlightAlternative** is kept, to be conservative.
9. **RoundEndPanel:** `ShowRound` always returns true in GameScene, so the panel is dead. TurnController must now call `CloseResults()` before going to the main menu.
10. **GameSceneStrayCleanup** is run, then deleted, in B4 (not B2).
11. **LoginGateUI** compiles on its own through IAuthUI. The file goes in B1; the AuthBootstrapper and AppFlowManager members it used go in B7.
12. **MoveSelectionPanel.prefab** moves from Block 3 to B5, together with `MoveButton.prefab`, after the `buttonPrefab` field is gone.
13. **Captured piles:** `CapturedPileManager.GetPileView` is dropped as its own item; the whole class goes.
14. **LocalSeatBottomShift.targets:** remove [7] before [2].
15. **`OnPlayPressed`** and StartScreenV2 lines 43/90 stay. They go only with TapToEnterUI (Block 7).
16. **Block 11 "ic_* restano" is wrong.** Only the `ic_*` names that UI51 builders look up are live. `ic_shop` and the non-cream sets go to the user-decision list.
17. **`TrainingGameModeProvider`** is a class inside `Assets/Scripts/Core/MatchConfig.cs`, not a file of its own.
18. **GuestPanel** moves from B6 to B7, so the Latin-1 `AuthUIController` gets a single byte-safe pass.

---

## User decisions (not scheduled)

- **7a / 7b, legacy Home HUD.** 7a also unlocks:
  - moving the legacy selection into QuickSelectionPanels, then deleting the HUD and the safe-area fitters;
  - 2D Casual UI;
  - the theme chain (`DragonsHoardTheme.asset`, `UITheme`, `ThemedButton`, `ThemedPanel`);
  - the holo material, shader and driver;
  - the Drop Shadow mats;
  - removing `AuthUIController.OnPlayPressed` with `TapToEnterUI`.
  - With 7b instead, turn off the legacy HUD debug flags.
- **8a / 8b / 8c, CreateRoom and JoinRoom.** Unlocks removing JoinRoom, `CodeCellsV2` and CreateRoom CloseButtons [0..3].
- **Shop V2 and its builder**; `CosmeticCatalog.cs`; the start-screen stylers (`StartScreenAuthButtonsBuilder`, `GoldButtonLabelStyle`).
- **Tests:** explicit runtime test harness; holographic.
- **Packages and imports:** UIEffect package; Photon demos (3 folders); GPGS/EDM.
- **Player settings:** orientation; companyName and iOS bundle id.
- **Git:** deleting merged branches (pushing needs an explicit OK).
- **Online:** forfeit step B (drop the client forfeit estimate); release log filter.
- **Asset moves and imports:** move the mockups; reward sounds; deck import size; move the deck sources; audio masters.
- **Images:**
  - DragonsHoard ChatGPT images.
  - Art user images. Keep or move `SecondBanner` and `firstBanner 1`, which `chatgpt-art-brief.md:63` cites.
  - The table kit.
  - The DragonsHoard old emoticons: move, don't delete. They are the only source of the `emo_*_anim` sheets.
  - Non-cream icons; DragonsHoard/UI51 duplicates; HomeAmbient crop; oversized Common sprites; `ic_shop`; low-frequency textures.
- **Docs:** `Assets/Networking` (all 26 docs); rewrite README_AUTH; rewrite README; Codex/AGENTS drift.
- **Code:** move the UI51 views (not into `UI51/Scripts`); TurnController reflection → events; repoint the emoticon sheet.