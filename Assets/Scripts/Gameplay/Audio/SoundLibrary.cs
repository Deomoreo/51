using System;
using UnityEngine;

namespace Project51.Unity
{
    /// <summary>Suoni del gioco. Le varianti di uno stesso suono si alternano a caso.</summary>
    public enum SoundId
    {
        UiClick,
        UiBack,
        UiTab,
        UiConfirm,
        UiError,
        PopupOpen,
        PopupClose,
        Notification,
        CardPlay,
        CardCapture,
        CardDeal,
        CardDealLong,
        Scopa,
        Accuso,
        YourTurn,
        MatchStart,
        Victory,
        Defeat,
        RewardCoin,
        RewardGem
    }

    /// <summary>
    /// Musica ed effetti con i loro volumi (Resources/Audio/SoundLibrary). Costruita da
    /// Tools/Audio/Build Sound Library, che misura anche inizio e colpo di ogni file: i volumi si
    /// possono poi ritoccare a mano dall'Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "51/Sound Library")]
    public sealed class SoundLibrary : ScriptableObject
    {
        public const string ResourcesPath = "Audio/SoundLibrary";

        [Serializable]
        public sealed class Variant
        {
            public AudioClip Clip;
            [Tooltip("Secondi di silenzio prima che il suono si senta.")]
            public float Onset;
            [Tooltip("Secondi al colpo piu' forte (es. la carta che tocca il tavolo).")]
            public float Hit;
        }

        [Serializable]
        public sealed class Sound
        {
            public SoundId Id;
            public Variant[] Variants = new Variant[0];
            [Range(0f, 1f)] public float Volume = 1f;
            [Tooltip("Variazione casuale dell'intonazione, per non sentire sempre lo stesso identico suono.")]
            [Range(0f, 0.2f)] public float PitchJitter;
            [Tooltip("Tempo minimo tra due riproduzioni dello stesso suono.")]
            public float MinInterval = 0.05f;
        }

        [Header("Musica")]
        public AudioClip Music;
        [Range(0f, 1f)] public float MusicVolumeHome = 0.3f;
        [Range(0f, 1f)] public float MusicVolumeTable = 0.15f;
        [Tooltip("Secondi per passare da un volume all'altro.")]
        public float MusicFadeSeconds = 1.2f;

        [Header("Effetti")]
        public Sound[] Sounds = new Sound[0];

        public Sound Get(SoundId id)
        {
            if (Sounds == null) return null;
            foreach (var sound in Sounds)
            {
                if (sound != null && sound.Id == id) return sound;
            }
            return null;
        }
    }
}
