using UnityEngine;

namespace Project51.Core
{
    public static class GameAudioPreferences
    {
        public const string PreferenceKey = "Settings_GameAudio";
        public const string MusicKey = "Settings_Music";
        public const string EffectsKey = "Settings_Effects";

        // Letti ogni frame dal sistema audio: tenuti in memoria (-1 = da leggere).
        private static int enabled = -1;
        private static int music = -1;
        private static int effects = -1;

        /// <summary>Audio generale (Impostazioni della Home): spegne tutto.</summary>
        public static bool Enabled => Read(PreferenceKey, ref enabled);

        /// <summary>Musica di sottofondo (Impostazioni in partita).</summary>
        public static bool MusicEnabled => Enabled && Read(MusicKey, ref music);

        /// <summary>Suoni di carte, prese e accusi (Impostazioni in partita).</summary>
        public static bool EffectsEnabled => Enabled && Read(EffectsKey, ref effects);

        /// <summary>Scelta salvata per la musica, senza tenere conto dell'audio generale.</summary>
        public static bool MusicChoice => Read(MusicKey, ref music);

        /// <summary>Scelta salvata per gli effetti, senza tenere conto dell'audio generale.</summary>
        public static bool EffectsChoice => Read(EffectsKey, ref effects);

        public static void SetEnabled(bool on)
        {
            Write(PreferenceKey, ref enabled, on);
            Apply();
        }

        public static void SetMusicEnabled(bool on) => Write(MusicKey, ref music, on);

        public static void SetEffectsEnabled(bool on) => Write(EffectsKey, ref effects, on);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Apply() => AudioListener.volume = Enabled ? 1f : 0f;

        private static bool Read(string key, ref int cache)
        {
            if (cache < 0) cache = PlayerPrefs.GetInt(key, 1) != 0 ? 1 : 0;
            return cache == 1;
        }

        private static void Write(string key, ref int cache, bool on)
        {
            cache = on ? 1 : 0;
            PlayerPrefs.SetInt(key, cache);
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            enabled = -1;
            music = -1;
            effects = -1;
        }
    }
}
