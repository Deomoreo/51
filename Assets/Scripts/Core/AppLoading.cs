using System;
using UnityEngine;

namespace Project51.Core
{
    // Keeps gameplay independent of the menu's UI assembly.
    public static class AppLoading
    {
        public static event Action<string> SceneRequested;
        public static event Action<bool, string> AuthenticationBusy;
        public static bool IsAvailable => SceneRequested != null;
        public static bool LoadScene(string scene)
        {
            if (SceneRequested == null) return false;
            SceneRequested.Invoke(scene);
            return true;
        }
        public static void SetAuthenticationBusy(bool busy, string message) => AuthenticationBusy?.Invoke(busy, message);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { SceneRequested = null; AuthenticationBusy = null; }
    }
}
