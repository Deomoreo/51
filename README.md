# 51 (Cirulla)

Mobile card game (Italian Cirulla / "51") built with Unity: 1v1, 2v2 and 1v3 tables against bots or online,
with guest or account login, XP, rewards, friends and mail.

- **Unity** 2022.3.60f1, portrait only, Android/iOS.
- **Online**: Photon PUN 2 (rooms, matchmaking) + PlayFab (accounts, data, CloudScript).

## Scenes
- `Assets/Scenes/MainMenu.unity`: login, Home and every menu page.
- `Assets/Scenes/GameScene.unity`: the table.
- `Assets/UI51/Scenes/UI51_Gallery.unity`: component gallery (not in the build).

## Where things live
- `Assets/Scripts/Core`, `Assets/Scripts/Gameplay`: rules, AI and match flow (separate asmdefs).
- `Assets/Scripts/Networking`, `Assets/Scripts/Auth`: Photon and PlayFab services.
- `Assets/Scripts/UI`: screen controllers and views.
- `Assets/UI51`: current UI system (art, prefabs, components); its Editor builders are
  `Assets/UI51/Editor/UI51*Builder.cs` (menu Tools/UI51). Scenes are changed through builders, not by hand.
- `Assets/UIV2`: older UI shells still used by some screens.
- `Assets/Tests/Editor`: EditMode tests.
- `Server/CloudScript`: PlayFab CloudScript (`51.js`). `node Server/CloudScript/test.js` runs its tests;
  `node Server/CloudScript/carica.js` writes the upload file. `segreto.txt` and `51.carica.js` stay out of git.
- `Design/`: handoff mockups and source art that is not imported by Unity. `docs/`: art briefs and the archive.

## Running the tests
Unity: Window > General > Test Runner > EditMode > Run All.

## More
- `CLAUDE.md`: working rules for agents; `.claude/skills`: project skills.
- `SPRINT_BACKLOG.md`: live to-do list and current state.
