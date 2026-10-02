---
name: unity-builder
description: Use when creating or changing scenes, prefabs or UI hierarchy in project 51 through a Tools/ Editor builder script (Assets/Editor/*Builder.cs), or when deciding whether to hand-edit .unity/.prefab YAML. Covers the builder pattern, scene forcing, destructive builders, and assembly/TMP gotchas.
---

# Unity Editor builders (project 51)

Scene/prefab changes go through Editor builders run via Unity-MCP, never hand-edited YAML
on objects with live runtime scripts.

## Before writing or reusing a builder
1. Search for an existing builder that already owns the target (`Assets/UI51/Editor/UI51*Builder.cs`,
   menu Tools/UI51, shared helpers in `UI51Build.cs`; older ones in `Assets/Editor/*Builder*.cs`,
   graphify query). Extend it with a small additive, idempotent method instead of adding a new one.
2. Check the template you copy from: it must force its scene (see below).

## Required shape
- First real action: `EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)` for a hardcoded scene path.
  Never `FindObjectOfType<Canvas>()` on "whatever scene is open".
- If the scene is dirty, save it first (otherwise `SaveCurrentModifiedScenesIfUserWantsTo` pops a modal dialog).
- Create-or-reuse children by name (idempotent: running twice = same result).
- End with `EditorSceneManager.MarkSceneDirty` + `SaveScene`.

## Gotchas
- Gameplay/Core are separate asmdefs; `UI/` is not. Cross-reference via reflection (see `TrySendAccusoSync`), not `using`.
- TMP settings (outlineWidth etc.) on an inactive GameObject throw ArgumentNullException:
  `SetActive(true)`, set, restore.
- Don't `SetActive(false)` a root to hide one Image; `Set*`/`Bind` must null-guard optional overrides.
- Size sprites from their visible alpha bbox; for 9-slice at non-native size use
  `pixelsPerUnitMultiplier = nativeH / targetH` on the Image directly.
- Source files may be non-UTF8: check for non-ASCII bytes before Edit/Write.

## Run and check
`execute_menu_item` for the builder menu path, then `read_console` (0 errors), then the `ui-verify` skill for anything visual.
