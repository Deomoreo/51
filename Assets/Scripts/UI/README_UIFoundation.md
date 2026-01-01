# UI Foundation Checklist

### Canvas Hierarchy
1. **Canvas**: Set `Render Mode` to `Screen Space - Overlay`.
2. **Dimmer (Fullscreen)**: Add an `Image` component that covers the entire screen. Place it outside the safe area. Use a semi-transparent color (e.g., black at 50% opacity).
3. **Window (Safe Area)**: Add a `RectTransform` that respects the safe area. This will contain the modal content.
4. **Order**: Ensure the `Dimmer` is below the `Window` in the hierarchy to prevent click-through issues.

### Prefabs
- **PrimaryButtonUI**: Button with punch scale feedback.
- **TabButtonUI**: Button with selectable states (normal/selected).
- **ModalWindowBaseUI**: Base modal window with fade/scale animations.
- **ModalManager**: Manages modal windows in the scene.

### Notes
- Use `DOTween` for animations.
- Ensure all references are assigned via the Inspector.
- Avoid using `FindObjectOfType` or singletons for better modularity.