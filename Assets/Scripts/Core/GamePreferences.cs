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

        public const string GraphicsQualityKey = "Settings_GraphicsQuality";
        private const string LegacyHomeFastKey = "Settings_AnimazioniVeloci";
        public const int QualityLow = 0, QualityMedium = 1, QualityHigh = 2;

        private static int graphicsQuality = -1;
        private static int fastAnimations = -1;
        private static int moveHints = -1;
        private static int vibration = -1;
        public static bool VibrationEnabled => Read(VibrationKey, ref vibration, 1);
        public static void SetVibrationEnabled(bool on) => Write(VibrationKey, ref vibration, on);

        /// <summary>Qualita' grafica: 0 Bassa, 1 Media, 2 Alta. Bassa spegne gli effetti pesanti (ex "Grafica ridotta").</summary>
        public static int GraphicsQuality
        {
            get
            {
                if (graphicsQuality < 0)
                {
                    if (PlayerPrefs.HasKey(GraphicsQualityKey))
                        graphicsQuality = Mathf.Clamp(PlayerPrefs.GetInt(GraphicsQualityKey), QualityLow, QualityHigh);
                    else
                    {
                        // One-time migration from the single "Grafica ridotta" choice (and the older fast-animation keys).
                        int legacy = PlayerPrefs.HasKey(ReducedGraphicsKey) ? PlayerPrefs.GetInt(ReducedGraphicsKey) : LegacyFast();
                        graphicsQuality = legacy != 0 ? QualityLow : QualityHigh;
                        PlayerPrefs.SetInt(GraphicsQualityKey, graphicsQuality);
                        PlayerPrefs.Save();
                    }
                }
                return graphicsQuality;
            }
        }

        public static bool ReducedGraphics => GraphicsQuality == QualityLow;

        /// <summary>"Animazioni rapide": indipendente dalla qualita' grafica.</summary>
        public static bool FastAnimations
        {
            get
            {
                if (fastAnimations < 0) fastAnimations = LegacyFast() != 0 ? 1 : 0;
                return fastAnimations == 1;
            }
        }

        // The table setting wins over the older Home setting.
        private static int LegacyFast() => PlayerPrefs.HasKey(FastAnimationsKey)
            ? PlayerPrefs.GetInt(FastAnimationsKey)
            : PlayerPrefs.GetInt(LegacyHomeFastKey, 0);

        /// <summary>Evidenzia le carte in mano che fanno una presa.</summary>
        public static bool MoveHints => Read(MoveHintsKey, ref moveHints, 1);

        /// <summary>Moltiplicatore di velocita' delle animazioni del tavolo (1 = normale).</summary>
        public static float AnimationSpeed => FastAnimations ? FastAnimationSpeed : 1f;

        /// <summary>Durata o attesa gia' ridotta se le animazioni veloci sono attive.</summary>
        public static float Scaled(float seconds) => seconds / AnimationSpeed;

        public static void SetGraphicsQuality(int quality)
        {
            graphicsQuality = Mathf.Clamp(quality, QualityLow, QualityHigh);
            PlayerPrefs.SetInt(GraphicsQualityKey, graphicsQuality);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void SetFastAnimations(bool on) => Write(FastAnimationsKey, ref fastAnimations, on);

        // The old single "Grafica ridotta" toggle (V2 settings panels) still drives both until those panels are replaced.
        public static void SetReducedGraphics(bool on)
        {
            fastAnimations = on ? 1 : 0;
            PlayerPrefs.SetInt(FastAnimationsKey, fastAnimations);
            SetGraphicsQuality(on ? QualityLow : QualityHigh);
        }

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
            graphicsQuality = -1;
            fastAnimations = -1;
            moveHints = -1;
            vibration = -1;
        }
    }
}
