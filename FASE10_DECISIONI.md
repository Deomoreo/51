# Fase 10 decisions (taken by the lead on the user's "fai tutto tu", 03/10)

These replace every "user decision" in plan.md. Items marked SKIP are NOT done (they go in the final report).

## Done (scheduled)
- **7a YES**: remove the legacy MainMenu HUD entirely (batch B7H). Selection moves into QuickSelectionPanels per finding `legacy-selection-into-quickpanels`; then `legacy-launcher-trim`, `legacy-authui-dead`, `legacy-mainmenu-hud-delete`, `scene-mm-legacy-hud`, `legacy-foil-noise`, `assets-foil`, `tests-holofoil-legacy-hud`, `dup-safearea-legacy`, `assets-legacy-hud-art` (7a branch: delete `Assets/2D Casual UI`, `Assets/Art/1spade.png`, `2spade.png`, `n o.png`), theme chain (`DragonsHoardTheme.asset`, `UITheme`, `ThemedButton`, `ThemedPanel`) and `TapToEnterUI` + `AuthUIController.OnPlayPressed` only if nothing live still needs them (grep first), Drop Shadow mats if orphaned.
- **8a YES**: INVITA from Friends opens the UI51 Modalita panel on the private-room tab (finding `legacy-invite-roomflow`, corrected_action), then delete CreateRoom + JoinRoom + their RoomFlowV2 fields + CodeCellsV2 (batch B8I). Also `scene-mm-createjoin`.
- **Shop V2: DELETE** (findings `rt-shopv2-cluster`, `assets-uiv2-shop-prototype`, `ed-shop-builder`). So in B2 every `UIV2FoundationBuilder*.cs` goes; only HomeAmbient is extracted to `HomeAmbientBuilder.cs`.
- **Unpack the 6 UIV2 prefab instances in MainMenu and delete their dead subtrees** (findings `scene-mm-unpack-uiv2-prefabs`, `scene-mm-prefab-screen-leftovers` with its corrected_action), then delete the prefab assets that end up with zero references and fix/delete the asset-bound tests (batch BU, after B8I).
- **UIEffect package: REMOVE** (`hyg-pkg-uieffect`).
- **Packages**: remove `com.unity.visualscripting`, `com.unity.timeline`, `com.unity.collab-proxy` (verify each unused first). Replace `com.unity.feature.2d` with `com.unity.2d.sprite` ONLY if no .psd/.psb/.ase importer or 2D package type is used by live assets/code; otherwise keep it. KEEP `com.unity.ide.rider`, `com.unity.ide.visualstudio`. Do NOT touch the unity-mcp package line (no pin). Do not pin ui-particle.
- **Player orientation**: portrait only (`hyg-player-orientation`).
- **Forfeit step A** (stop sending `avversario`, test.js update) YES; step B NO.
- **Reward sounds** (`crit-reward-sounds-unused`): Option A, play `SoundId.RewardCoin` (and RewardGem when gems are granted) on a successful claim in UI51RewardsView and UI51MailView. Tiny change.
- **Docs**: delete all 26 `Assets/Networking/*.md` (+ folder), delete `Assets/Scripts/Auth/README_AUTH.md`, rewrite `README.md` as a short UTF-8 README (what the game is, Unity version, scenes, where UI51/builders/tests/server live, how to run tests; point to CLAUDE.md and SPRINT_BACKLOG.md). `AGENTS.md`: replace body with a 2-line pointer to CLAUDE.md and .claude/skills; overwrite `.agents/skills/*` with `.claude/skills/*`; remove the CLAUDE_* block from `.codex/config.toml` if present.
- **User-made art/audio that is unreferenced: MOVE, never delete**, with `git mv` (drop the .meta files) into `Design/sorgenti/<same relative path under Assets>`: DragonsHoard "Immagine ChatGPT/Codex" images, DragonsHoard old `emo_*.png`, `home_flame_b`, `home_vines`, unreferenced user images in `Assets/Art` root (ChatGPT Image, SecondBanner, firstBanner 1, napoletane.svgz 1.png, TapToEnterBtn), `Assets/Art/Table` (only if zero refs after B2), unused `Assets/Audio` masters/alternates (only zero-ref ones; NOT the reward sounds), the 17 unused DragonsHoard icons from `assets-dh-noncream-icons`, UI51 `ic_shop`, rings and `sun_emblem` (keep the import script lines in sync). Update `docs/art/chatgpt-art-brief.md` paths if it cites moved files. Pure generated junk (`New Material.mat`, `New Render Texture`, `Art/Generated/*`, LiberationSans orphan mats) is deleted.
- **Assets/Screenshots**: delete and gitignore. Do NOT change any EditorPrefs.
- **Library untrack + generated files untrack** per B10.3. Never `git checkout` another branch.

## SKIP (report only)
- GPGS / EDM removal (Google login may come later).
- iOS bundle id / companyName.
- Moving Assets/Mockup (ui-verify skill uses it).
- Deck import size 1024, UI51 oversized Common sprites, low-frequency glow textures, HomeAmbient crop, DragonsHoard/UI51 duplicate consolidation (all change pixels: needs the user's eye).
- Holographic card hook, explicit runtime test harnesses (keep both).
- TurnController reflection -> events (needs a 2-device test).
- Forfeit step B.
- Moving the UI51 views folder.
- Deleting merged git branches; any push.
- unity-mcp / ui-particle pinning; EditorPrefs screenshot folder.

## Version
- B0 sets bundleVersion 2.64, Android bundleVersionCode 264, iOS buildNumber 264 (via PlayerSettings API). No further bumps inside Fase 10.
