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
        public const string ReducedGraphicsKey = "Settings_ReducedGraphics";
        public const string MoveHintsKey = "Settings_MoveHints";
        public const string VibrationKey = "Settings_Vibration";

        /// <summary>Con "Animazioni veloci" le animazioni e le attese del tavolo durano circa il 40% in meno.</summary>
        public const float FastAnimationSpeed = 1.6f;

        /// <summary>Una preferenza e' cambiata: chi disegna il tavolo si aggiorna subito.</summary>
        public static event Action Changed;

        private static int reducedGraphics = -1;
        private static int moveHints = -1;
        private static int vibration = -1;
        public static bool VibrationEnabled => Read(VibrationKey, ref vibration, 1);
        public static void SetVibrationEnabled(bool on) => Write(VibrationKey, ref vibration, on);

        public static bool ReducedGraphics
        {
            get
            {
                if (reducedGraphics < 0 && !PlayerPrefs.HasKey(ReducedGraphicsKey))
                {
                    // The current table setting wins over the older Home setting.
                    int previous = PlayerPrefs.HasKey(FastAnimationsKey)
                        ? PlayerPrefs.GetInt(FastAnimationsKey)
                        : PlayerPrefs.GetInt("Settings_AnimazioniVeloci", 0);
                    PlayerPrefs.SetInt(ReducedGraphicsKey, previous != 0 ? 1 : 0);
                    PlayerPrefs.Save();
                }
                return Read(ReducedGraphicsKey, ref reducedGraphics, 0);
            }
        }

        // Compatibility for existing callers and serialized settings. There is one choice.
        public static bool FastAnimations => ReducedGraphics;

        /// <summary>Evidenzia le carte in mano che fanno una presa.</summary>
        public static bool MoveHints => Read(MoveHintsKey, ref moveHints, 1);

        /// <summary>Moltiplicatore di velocita' delle animazioni del tavolo (1 = normale).</summary>
        public static float AnimationSpeed => FastAnimations ? FastAnimationSpeed : 1f;

        /// <summary>Durata o attesa gia' ridotta se le animazioni veloci sono attive.</summary>
        public static float Scaled(float seconds) => seconds / AnimationSpeed;

        public static void SetReducedGraphics(bool on)
        {
            PlayerPrefs.SetInt(FastAnimationsKey, on ? 1 : 0);
            Write(ReducedGraphicsKey, ref reducedGraphics, on);
        }
        public static void SetFastAnimations(bool on) => SetReducedGraphics(on);
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
            reducedGraphics = -1;
            moveHints = -1;
            vibration = -1;
        }
    }
}
