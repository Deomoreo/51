using System;
using UnityEngine;

namespace Project51.Core
{
    /// <summary>
    /// Preferenze di gioco scelte nelle Impostazioni in partita (mockup 26), salvate in PlayerPrefs.
    /// L'audio sta in GameAudioPreferences.
    /// </summary>
    public static class GamePreferences
    {
        public const string FastAnimationsKey = "Settings_FastAnimations";
        public const string MoveHintsKey = "Settings_MoveHints";

        /// <summary>Con "Animazioni veloci" le animazioni e le attese del tavolo durano circa il 40% in meno.</summary>
        public const float FastAnimationSpeed = 1.6f;

        /// <summary>Una preferenza e' cambiata: chi disegna il tavolo si aggiorna subito.</summary>
        public static event Action Changed;

        private static int fastAnimations = -1;
        private static int moveHints = -1;

        public static bool FastAnimations => Read(FastAnimationsKey, ref fastAnimations, 0);

        /// <summary>Evidenzia le carte in mano che fanno una presa.</summary>
        public static bool MoveHints => Read(MoveHintsKey, ref moveHints, 1);

        /// <summary>Moltiplicatore di velocita' delle animazioni del tavolo (1 = normale).</summary>
        public static float AnimationSpeed => FastAnimations ? FastAnimationSpeed : 1f;

        /// <summary>Durata o attesa gia' ridotta se le animazioni veloci sono attive.</summary>
        public static float Scaled(float seconds) => seconds / AnimationSpeed;

        public static void SetFastAnimations(bool on) => Write(FastAnimationsKey, ref fastAnimations, on);
        public static void SetMoveHints(bool on) => Write(MoveHintsKey, ref moveHints, on);

        private static bool Read(string key, ref int cache, int fallback)
        {
            if (cache < 0) cache = PlayerPrefs.GetInt(key, fallback) != 0 ? 1 : 0;
            return cache == 1;
        }

        private static void Write(string key, ref int cache, bool on)
        {
            cache = on ? 1 : 0;
            PlayerPrefs.SetInt(key, cache);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Changed = null;
            fastAnimations = -1;
            moveHints = -1;
        }
    }
}
