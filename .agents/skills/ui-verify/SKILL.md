---
name: ui-verify
description: Use before declaring any UI/screen/visual change in project 51 done, or when comparing a built screen against a mockup in Assets/Mockup. Verification in the Device Simulator plus pixel measurement against the mockup.
---

# Verify UI (project 51)

A glance at the Game view is never evidence. The Game view's 1080x1920 hides aspect-ratio breakage.

## 1. Simulator, not the Game view
- Focus the Simulator window before Play Mode: find the EditorWindow with
  `GetType().FullName == "UnityEditor.DeviceSimulation.SimulatorWindow"` and call `Focus()`.
- `manage_camera screenshot` then captures the simulated device (real `Screen.safeArea`, e.g. 1170x2532).
- Test at least one tall phone and one 16:9 device.

## 2. Wait for real time, not frames
The Editor runs ~465 fps. Wait on `Time.realtimeSinceStartup` (>=1 s after opening a panel) before
capturing; otherwise you measure mid-tween.

## 3. Measure against the mockup (Assets/Mockup/NN_*.png)
- Mockup side: load the PNG, scan a row band for the element colour, take the bbox (y from top: `y = H-1-yTop`).
- Live side: `TMP_Text.ForceMeshUpdate()` then `textBounds.size`; for boxes, the RectTransform world corners.
- New size = current x (mockup / live). `characterSpacing` inflates width. Measure boxes too, not just text.
- Compare region by region (top bar, content, CTA, nav). Report the deltas in px.

## 4. Quality bar
Clean and professional first. Mockups are a starting point (since 19/09); improving on them is allowed.
No tinting of icons with baked-in colour; check 9-slice borders before choosing a pill/bar sprite.

## 5. Report
List what was verified, on which device, and the remaining deltas. Then run the `asset-check` report.
Useful Play Mode recipes: memory `reference_play_mode_test_recipes`.
