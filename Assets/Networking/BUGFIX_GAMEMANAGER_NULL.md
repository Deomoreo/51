# BUGFIX: NullReferenceException in GameManager.CurrentGameMode

## Problem

When starting a multiplayer game, you get this error:

```
NullReferenceException: Object reference not set to an instance of an object
Project51.Unity.GameManager.get_CurrentGameMode () (at Assets/Scripts/Networking/GameManager.cs:40)
```

## Root Cause

The `CirullaGame` scene was created before the new `GameManager` component (from `Project51.Networking` assembly) was introduced. The scene only has a `GameObject` with a `TurnController` component, but is missing the `GameManager` component.

When `TurnController` tries to access `GameManager.Instance.CurrentGameMode` via reflection, it returns `null` because no `GameManager` exists in the scene.

## Solution

### Option 1: Recreate the Scene (Recommended)

1. Open Unity Editor
2. Go to menu: **Project51 > Setup > Create Main Scene**
3. This will create a new scene with all required components, including the new `GameManager`
4. Save this scene as `CirullaGame.unity` (or your preferred name)
5. Update the scene reference in `SceneLoadManager` if needed

### Option 2: Manually Add GameManager to Existing Scene

1. Open your `CirullaGame` scene in Unity Editor
2. Find the `GameManager` GameObject in the hierarchy (the one with `TurnController` component)
3. Select it
4. In the Inspector, click **Add Component**
5. Search for: `GameManager` (from `Project51.Unity` namespace)
6. Add the component
7. Verify the Inspector shows both:
   - `TurnController` component
   - `GameManager` component (from Project51.Networking assembly)
8. Save the scene

### Option 3: Script to Fix Existing Scenes

If you have multiple scenes to fix, you can use this editor script:

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixGameManagerScenes
{
    [MenuItem("Project51/Fix/Add GameManager to Current Scene")]
    public static void FixCurrentScene()
    {
        var turnController = Object.FindObjectOfType<Project51.Unity.TurnController>();
        if (turnController == null)
        {
            Debug.LogError("No TurnController found in scene!");
            return;
        }

        var gameManagerType = System.Type.GetType("Project51.Unity.GameManager, Project51.Networking");
        if (gameManagerType == null)
        {
            Debug.LogError("Could not find Project51.Unity.GameManager type!");
            return;
        }

        var existing = turnController.GetComponent(gameManagerType);
        if (existing != null)
        {
            Debug.Log("GameManager component already exists!");
            return;
        }

        turnController.gameObject.AddComponent(gameManagerType);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Added GameManager component to " + turnController.gameObject.name);
    }
}
```

## Verification

After applying the fix, verify that:

1. The scene has a GameObject (typically named "GameManager") with both:
   - `TurnController` component
   - `GameManager` component (from `Project51.Networking`)

2. When you start a multiplayer game, you should see this log instead of the error:
   ```
   <color=cyan>=== MULTIPLAYER MODE ===</color>
   ```

## Related Files

- `Assets/Scripts/Networking/GameManager.cs` - The new multiplayer-aware GameManager
- `Assets/Scripts/Gameplay/TurnController.cs` - Uses GameManager via reflection
- `Assets/Editor/SceneSetup/CreateCirullaScene.cs` - Now creates scenes with GameManager
- `Assets/Scenes/CirullaGame.unity` - Scene that needs to be fixed (if exists)

## Status

- ? `CreateCirullaScene.cs` updated to include GameManager component
- ?? Existing `CirullaGame` scene needs manual fix (follow Option 1 or 2 above)
