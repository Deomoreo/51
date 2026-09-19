using System.Collections.Generic;
using System.IO;
using Project51.Unity;
using UnityEditor;
using UnityEngine;

namespace Project51.EditorTools
{
    /// <summary>
    /// Musica ed effetti da Assets/Audio (pacchetti audio v01-v03): impostazioni di importazione dei README,
    /// libreria Resources/Audio/SoundLibrary con i volumi di partenza, misura di inizio e colpo di ogni file.
    /// Rilanciabile: i volumi gia' ritoccati nella libreria restano, cambiano solo i file.
    /// </summary>
    public static class SoundLibraryBuilder
    {
        private const string AudioDir = "Assets/Audio/";
        private const string LibraryDir = "Assets/Resources/Audio";
        private const string LibraryPath = LibraryDir + "/SoundLibrary.asset";
        private const string MusicFile = "home_theme_loop.ogg";

        private struct Entry
        {
            public SoundId Id;
            public string[] Files;
            public float Volume;
            public float Jitter;
            public float MinInterval;
            /// <summary>Il colpo e' l'ultimo forte del file (carta che tocca il tavolo), non il piu' forte.</summary>
            public bool HitIsLastPeak;

            public Entry(SoundId id, float volume, float jitter, float minInterval, params string[] files)
            {
                Id = id;
                Volume = volume;
                Jitter = jitter;
                MinInterval = minInterval;
                Files = files;
                HitIsLastPeak = false;
            }

            public Entry LastPeak()
            {
                HitIsLastPeak = true;
                return this;
            }
        }

        // Volumi di partenza dai README del pacchetto audio.
        private static readonly Entry[] Entries =
        {
            new Entry(SoundId.UiClick, 0.45f, 0.03f, 0.05f, "ui_click.wav"),
            new Entry(SoundId.UiBack, 0.5f, 0f, 0.05f, "ui_back.wav"),
            new Entry(SoundId.UiTab, 0.5f, 0.02f, 0.03f, "ui_tab.wav"),
            new Entry(SoundId.UiConfirm, 0.6f, 0f, 0.2f, "ui_confirm_02.wav"),
            new Entry(SoundId.UiError, 0.55f, 0f, 0.2f, "ui_error.wav"),
            new Entry(SoundId.PopupOpen, 0.5f, 0f, 0.15f, "popup_open.wav"),
            new Entry(SoundId.PopupClose, 0.5f, 0f, 0.15f, "popup_close.wav"),
            new Entry(SoundId.Notification, 0.5f, 0f, 0.5f, "notification_soft.wav"),
            new Entry(SoundId.CardPlay, 0.65f, 0.04f, 0.05f, "card_play_01.wav", "card_play_02.wav", "card_play_03.wav").LastPeak(),
            new Entry(SoundId.CardCapture, 0.7f, 0.03f, 0.1f, "card_capture_01.wav", "card_capture_02.wav", "card_capture_03.wav"),
            new Entry(SoundId.CardDeal, 0.6f, 0f, 0.2f, "card_deal_3.wav"),
            new Entry(SoundId.CardDealLong, 0.6f, 0f, 0.2f, "card_deal_6.wav"),
            new Entry(SoundId.Scopa, 0.85f, 0f, 1f, "scopa.wav"),
            new Entry(SoundId.Accuso, 0.9f, 0f, 0.25f, "accuso_01.wav", "accuso_02.wav", "accuso_03.wav"),
            new Entry(SoundId.YourTurn, 0.55f, 0f, 0.5f, "your_turn.wav"),
            new Entry(SoundId.MatchStart, 0.8f, 0f, 1f, "match_start.wav"),
            new Entry(SoundId.Victory, 0.9f, 0f, 1f, "victory_short.wav"),
            new Entry(SoundId.Defeat, 0.8f, 0f, 1f, "defeat.wav"),
            new Entry(SoundId.RewardCoin, 0.75f, 0.03f, 0.08f, "reward_coin.wav"),
            new Entry(SoundId.RewardGem, 0.78f, 0f, 0.2f, "reward_gem.wav"),
        };

        [MenuItem("Tools/Audio/Build Sound Library")]
        private static void Build()
        {
            ImportMusic(AudioDir + MusicFile);
            var sounds = new List<SoundLibrary.Sound>();
            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
            bool created = library == null;
            if (created)
            {
                Directory.CreateDirectory(LibraryDir);
                library = ScriptableObject.CreateInstance<SoundLibrary>();
            }

            foreach (var entry in Entries)
            {
                var previous = library.Get(entry.Id);
                var sound = new SoundLibrary.Sound
                {
                    Id = entry.Id,
                    Volume = previous != null ? previous.Volume : entry.Volume,
                    PitchJitter = previous != null ? previous.PitchJitter : entry.Jitter,
                    MinInterval = previous != null ? previous.MinInterval : entry.MinInterval,
                };

                var variants = new List<SoundLibrary.Variant>();
                foreach (var file in entry.Files)
                {
                    string path = AudioDir + file;
                    var clip = ImportEffect(path);
                    if (clip == null)
                    {
                        Debug.LogWarning("[SoundLibraryBuilder] File audio mancante: " + path);
                        continue;
                    }
                    Measure(clip, entry.HitIsLastPeak, out float onset, out float hit);
                    variants.Add(new SoundLibrary.Variant { Clip = clip, Onset = onset, Hit = hit });
                }
                sound.Variants = variants.ToArray();
                sounds.Add(sound);
            }

            library.Sounds = sounds.ToArray();
            library.Music = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + MusicFile);
            if (created)
            {
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log("[SoundLibraryBuilder] Libreria audio aggiornata: " + sounds.Count + " suoni, musica " + (library.Music != null ? library.Music.name : "assente"));
        }

        /// <summary>Musica: letta a pezzi mentre suona (Streaming), Vorbis.</summary>
        private static void ImportMusic(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.loadInBackground = true;
            SetNormalize(importer, false);
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Effetti brevi: decompressi al caricamento, ADPCM (PCM per i click cortissimi), stereo, senza
        /// normalizzare (i livelli del pacchetto sono gia' bilanciati tra loro).
        /// </summary>
        private static AudioClip ImportEffect(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return null;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            bool veryShort = clip != null && clip.length < 0.3f;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = veryShort ? AudioCompressionFormat.PCM : AudioCompressionFormat.ADPCM;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.loadInBackground = false;
            SetNormalize(importer, false);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static void SetNormalize(AudioImporter importer, bool normalize)
        {
            var so = new SerializedObject(importer);
            var property = so.FindProperty("m_Normalize") ?? so.FindProperty("normalize");
            if (property == null) return;
            property.boolValue = normalize;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Onset: primo tratto da 10 ms sopra il 30% del massimo. Hit: tratto con il massimo, oppure
        /// (lastPeak) l'ultimo picco sopra il 60% del massimo.
        /// </summary>
        public static void Measure(AudioClip clip, bool lastPeak, out float onset, out float hit)
        {
            onset = 0f;
            hit = 0f;
            if (clip == null || clip.samples == 0) return;
            clip.LoadAudioData();
            var data = new float[clip.samples * clip.channels];
            if (!clip.GetData(data, 0)) return;
            MeasureSamples(data, clip.channels, clip.frequency, lastPeak, out onset, out hit);
        }

        public static void MeasureSamples(float[] data, int channels, int frequency, bool lastPeak, out float onset, out float hit)
        {
            onset = 0f;
            hit = 0f;
            int window = Mathf.Max(1, frequency / 100) * Mathf.Max(1, channels);
            int windows = Mathf.CeilToInt(data.Length / (float)window);
            var envelope = new float[windows];
            float peak = 0f;
            int peakIndex = 0;
            for (int w = 0; w < windows; w++)
            {
                float max = 0f;
                int end = Mathf.Min(data.Length, (w + 1) * window);
                for (int i = w * window; i < end; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
                envelope[w] = max;
                if (max > peak)
                {
                    peak = max;
                    peakIndex = w;
                }
            }
            if (peak <= 0f) return;
            int first = System.Array.FindIndex(envelope, e => e > peak * 0.3f);
            onset = Mathf.Max(0, first) * 0.01f;
            hit = peakIndex * 0.01f;
            if (!lastPeak) return;
            for (int w = windows - 1; w > peakIndex; w--)
            {
                bool localMax = envelope[w] >= envelope[w - 1] && (w == windows - 1 || envelope[w] >= envelope[w + 1]);
                if (envelope[w] >= peak * 0.6f && localMax)
                {
                    hit = w * 0.01f;
                    return;
                }
            }
        }
    }
}
