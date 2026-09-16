using UnityEngine;

namespace Project51.Core
{
    public static class GameAudioPreferences
    {
        public const string PreferenceKey = "Settings_GameAudio";
        public static bool Enabled => PlayerPrefs.GetInt(PreferenceKey, 1) != 0;

        public static void SetEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(PreferenceKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Apply() => AudioListener.volume = Enabled ? 1f : 0f;
    }
}
