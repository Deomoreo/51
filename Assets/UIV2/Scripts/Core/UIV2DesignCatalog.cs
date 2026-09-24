using UnityEngine;

namespace Project51.UIV2.Core
{
    /// <summary>Resources entry point to the existing shared theme, not a second copy of it.</summary>
    public sealed class UIV2DesignCatalog : ScriptableObject
    {
        public UIV2Theme Theme;
    }
}
