---
name: asset-check
description: Use in project 51 whenever a UI element seems to need an icon, sprite, panel or button, before saying an asset is missing, and at the end of every visual/UI task to produce the ASSET MANCANTI DA CREARE report.
---

# Asset inventory check (project 51)

Never assume an asset is missing before checking the inventory. Never fake, stretch or reuse a wrong asset.

## 1. Look before declaring missing
- Sheets in `Assets/UI/Sprites/DragonsHoard/sprites_unity/sprites_unity/`:
  - Icons.png: buttons, bars, panels, tabs, ribbons, frames, `ic_*`
  - Icons2.png: more `ic_*`
  - PanelsNeutral_v2.png: tintable fill/ring shapes
  - Avatars.png
  - 14_emoticon_set
  - logo_51
  The `.meta` sprite list is authoritative (not `import_manifest.json`).
- Also search `Assets/UIV2/`, `Assets/UI/`, `Assets/Audio`, and use Unity Search via Unity-MCP
  (`manage_asset` search) by name and type.
- Try composition: a neutral `panel_fill_*` + tint + `panel_ring_*` + ribbon/medallion + TMP text
  reproduces most panels. Check a sprite's pixel content, not just its name (`bar_energy` is an illustration, not a fill).

## 2. Only declare missing if all three are true
1. There is no suitable sprite.
2. It can't be faithfully composed from existing assets.
3. Reusing an existing asset would visibly differ from the intended design.

In that case use a neutral placeholder (flat colour, correct size/aspect) and do not create the art yourself.

## 3. Close every visual task with the report (even when empty)
```
ASSET MANCANTI DA CREARE
[MISSING ASSET]
- Screen/Component:
- Elemento necessario:
- Dimensione prevista (px):
- Placeholder usato:
```
If there are none, write "Nessun asset mancante".
