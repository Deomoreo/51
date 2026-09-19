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

        /// <summary>
        /// Vero finche' la schermata di caricamento copre il gioco. Il tavolo la aspetta prima di
        /// far partire l'intro della smazzata: altrimenti la scelta del mazziere cominciava dietro
        /// al velo e i suoi suoni si sentivano prima che ci fosse qualcosa da vedere.
        /// </summary>
        public static bool IsCovering { get; private set; }

        public static void SetCovering(bool covering) => IsCovering = covering;
        public static bool LoadScene(string scene)
        {
            if (SceneRequested == null) return false;
            SceneRequested.Invoke(scene);
            return true;
        }
        public static void SetAuthenticationBusy(bool busy, string message) => AuthenticationBusy?.Invoke(busy, message);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { SceneRequested = null; AuthenticationBusy = null; IsCovering = false; }
    }
}
