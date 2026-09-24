using DG.Tweening;
using Project51.Core;

namespace Project51.UIV2.Animations
{
    /// <summary>Tempi comuni, in secondi non scalati. Rispetta Animazioni veloci.</summary>
    public static class UIV2Motion
    {
        public const float PressScale = .94f;
        public const float PanelScale = .96f;
        public static float Press => GamePreferences.Scaled(.07f);
        public static float Release => GamePreferences.Scaled(.18f);
        public static float Enter => GamePreferences.Scaled(.24f);
        public static float Exit => GamePreferences.Scaled(.16f);
        public static float Page => GamePreferences.Scaled(.28f);
        public static float Fade => GamePreferences.Scaled(.25f); // passaggi Home <-> tavolo (AppLoadingView) e pagine
        public static float Count => GamePreferences.Scaled(.40f);
        public static float Flight => GamePreferences.Scaled(.55f);

        public static void Cancel(ref Tween tween)
        {
            tween?.Kill(false);
            tween = null;
        }
    }
}