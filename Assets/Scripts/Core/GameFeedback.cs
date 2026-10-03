using System;
using UnityEngine;

namespace Project51.Core
{
    public enum FeedbackKind { Scopa, Accuso, Victory, Reward }

    /// <summary>Presentation requests only. Never changes scores, rewards or animation timing.</summary>
    public static class GameFeedback
    {
        public static event Action<FeedbackKind, Vector2> Presented;
        public static event Action ParticlesDisabled;
        public static bool ParticlesEnabled
        {
            get { BindPolicy(); return !GamePreferences.ReducedGraphics; }
        }
        private static float lastHaptic = float.NegativeInfinity;
        private static bool lastStrong;
        private static volatile int generation;
        private static bool policyBound;
        private static bool focused = true;

        private static void PolicyChanged()
        {
            System.Threading.Interlocked.Increment(ref generation);
            if (GamePreferences.ReducedGraphics) ParticlesDisabled?.Invoke();
#if UNITY_IOS && !UNITY_EDITOR
            K6SetGeneration(generation);
#endif
        }

        private static void FocusChanged(bool value) { focused = value; PolicyChanged(); }

        public static void Present(FeedbackKind kind, bool localHuman, Vector2 viewportPosition)
        {
            TryHaptic(true, localHuman);
            if (ParticlesEnabled) Presented?.Invoke(kind, viewportPosition);
        }

        public static void ForPlayer(FeedbackKind kind, int player, Vector2 viewportPosition)
        {
            var mode = GameModeService.Current;
            Present(kind, mode.IsLocalPlayer(player) && mode.IsHumanPlayer(player), viewportPosition);
        }

        /// <summary>Returns whether the policy accepted a pulse; hardware may be absent or disabled by the OS.</summary>
        public static bool TryHaptic(bool strong, bool localHuman = true)
        {
            BindPolicy();
            if (!localHuman || !focused || !GamePreferences.VibrationEnabled) return false;
            float now = Time.unscaledTime;
            if (now - lastHaptic < .08f && (!strong || lastStrong)) return false;
            lastHaptic = now;
            lastStrong = strong;
            NativeHaptic(strong);
            return true;
        }

        private static void BindPolicy()
        {
            if (policyBound) return;
            GamePreferences.Changed += PolicyChanged;
            Application.focusChanged += FocusChanged;
            policyBound = true;
        }

        private static void NativeHaptic(bool strong)
        {
            int requestedGeneration = generation;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        if (requestedGeneration != generation) return;
                        try
                        {
                            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                            using (var current = player.GetStatic<AndroidJavaObject>("currentActivity"))
                            using (var window = current.Call<AndroidJavaObject>("getWindow"))
                            using (var view = window.Call<AndroidJavaObject>("getDecorView"))
                            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                            {
                                // VIRTUAL_KEY (API 3), CONFIRM (API 30), LONG_PRESS fallback.
                                int effect = strong ? (version.GetStatic<int>("SDK_INT") >= 30 ? 16 : 0) : 1;
                                view.Call<bool>("performHapticFeedback", effect);
                            }
                        }
                        catch (AndroidJavaException) { /* Device has no usable haptic view. */ }
                    }));
                }
            }
            catch (AndroidJavaException) { /* No activity during shutdown. */ }
#elif UNITY_IOS && !UNITY_EDITOR
            K6Impact(strong ? 1 : 0, requestedGeneration);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void K6Impact(int strong, int requestedGeneration);
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void K6SetGeneration(int value);
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            GamePreferences.Changed -= PolicyChanged;
            Application.focusChanged -= FocusChanged;
            policyBound = false;
            focused = true;
            PolicyChanged();
            Presented = null;
            ParticlesDisabled = null;
            lastHaptic = float.NegativeInfinity;
            lastStrong = false;
        }
    }
}
