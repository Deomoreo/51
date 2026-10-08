using System.Collections.Generic;
using Project51.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Musica ed effetti di tutto il gioco. Nasce da sola all'avvio (se esiste Resources/Audio/SoundLibrary)
    /// e resta tra le scene. Rispetta GameAudioPreferences: audio generale (Home), musica ed effetti
    /// (impostazioni in partita). Aggancia da sola il click a ogni pulsante UI.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        /// <summary>Da dove far partire il file.</summary>
        public enum Sync
        {
            /// <summary>Dall'inizio del file.</summary>
            Start,
            /// <summary>Saltando il silenzio iniziale: si sente subito.</summary>
            Onset,
            /// <summary>Il colpo del file cade dopo hitIn secondi (es. quando la carta atterra).</summary>
            Hit
        }

        private const int PoolSize = 8;
        private const float ButtonScanInterval = 1f;

        private static GameAudio instance;

        private SoundLibrary library;
        private AudioSource music;
        private readonly List<AudioSource> pool = new List<AudioSource>();
        private readonly Dictionary<SoundId, float> lastPlayed = new Dictionary<SoundId, float>();
        private readonly Dictionary<SoundId, int> lastVariant = new Dictionary<SoundId, int>();
        private readonly HashSet<int> hookedButtons = new HashSet<int>();
        private SoundId pendingUi;
        private int pendingUiPriority = -1;
        private float nextButtonScan;
        // B35 (AU2): livello della musica senza abbassamento, e abbassamento (1 = niente) che dura quanto l'effetto che lo chiede.
        private float musicLevel, duck = 1f, duckTarget = 1f, duckUntil;

        /// <summary>Quanto si abbassa la musica sotto un effetto: circa -5 dB su scopa e accuso, -8 dB su vittoria e sconfitta.</summary>
        public static float DuckFor(SoundId id)
        {
            switch (id)
            {
                case SoundId.Scopa:
                case SoundId.Accuso: return 0.56f;
                case SoundId.Victory:
                case SoundId.Defeat: return 0.4f;
                default: return 1f;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (instance != null) return;
            var library = Resources.Load<SoundLibrary>(SoundLibrary.ResourcesPath);
            if (library == null) return;

            var go = new GameObject("GameAudio");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameAudio>();
            instance.library = library;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        private void Awake()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            music.volume = 0f;
            for (int i = 0; i < PoolSize; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                pool.Add(source);
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            GamePresentation.ConnectionNotice += OnConnectionNotice;
            GamePresentation.PlayerNotice += OnPlayerNotice;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GamePresentation.ConnectionNotice -= OnConnectionNotice;
            GamePresentation.PlayerNotice -= OnPlayerNotice;
            if (instance == this) instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            nextButtonScan = 0f;
            // Un effetto lungo iniziato nella scena precedente continuava a suonare sopra a quella
            // nuova (bug segnalato: suoni della partita che si sentono ancora in Home).
            StopEffects();
        }

        /// <summary>Zittisce subito tutti gli effetti in corso. La musica non si tocca: ha il suo dissolvimento.</summary>
        public static void StopAllEffects()
        {
            if (instance != null) instance.StopEffects();
        }

        private void StopEffects()
        {
            foreach (var source in pool)
            {
                source.Stop();
                source.clip = null;
            }
            pendingUiPriority = -1;
            lastPlayed.Clear();
        }

        private static void OnConnectionNotice(string message, float seconds)
        {
            if (!string.IsNullOrEmpty(message)) Play(SoundId.Notification, sync: Sync.Onset);
        }

        private static void OnPlayerNotice(int seat, string title, string sub, bool alert) => OnConnectionNotice(title, 0f);

        /// <summary>Effetto sonoro di gioco (carte, accusi, fine partita). Non fa nulla se gli effetti sono spenti.</summary>
        public static void Play(SoundId id, float volumeScale = 1f, Sync sync = Sync.Start, float hitIn = 0f)
        {
            if (instance == null || !GameAudioPreferences.EffectsEnabled) return;
            instance.PlayNow(id, volumeScale, sync, hitIn);
        }

        /// <summary>
        /// Suono di interfaccia: nello stesso frame ne suona uno solo, il piu' importante (un pannello che
        /// si apre batte il click del pulsante che l'ha aperto).
        /// </summary>
        public static void PlayUi(SoundId id)
        {
            if (instance == null) return;
            int priority = UiPriority(id);
            if (priority > instance.pendingUiPriority)
            {
                instance.pendingUi = id;
                instance.pendingUiPriority = priority;
            }
        }

        public static int UiPriority(SoundId id)
        {
            switch (id)
            {
                case SoundId.UiError: return 4;
                case SoundId.PopupOpen:
                case SoundId.PopupClose: return 3;
                case SoundId.UiConfirm: return 2;
                case SoundId.UiBack:
                case SoundId.UiTab: return 1;
                default: return 0;
            }
        }

        /// <summary>Suono del click di un pulsante, scelto dal nome dell'oggetto e del suo genitore.</summary>
        public static SoundId ClassifyButton(string objectName, string parentName = null)
        {
            string name = (objectName ?? string.Empty).ToLowerInvariant();
            string parent = (parentName ?? string.Empty).ToLowerInvariant();
            if (ContainsAny(name, "close", "back", "cancel", "indietro", "exit", "esci", "leave", "dim", "backdrop")) return SoundId.UiBack;
            // Barra in basso (GiocaSlot, BtnHome...) e schede: prima del controllo "gioca".
            if (ContainsAny(parent, "nav", "tabs") || name.StartsWith("tab_") || name.EndsWith("tab")) return SoundId.UiTab;
            if (ContainsAny(name, "continue", "rematch", "gioca", "confirm", "start", "create", "join", "guest")
                || name.StartsWith("play") || parent.StartsWith("playbutton")) return SoundId.UiConfirm;
            return SoundId.UiClick;
        }

        private static bool ContainsAny(string text, params string[] words)
        {
            foreach (var word in words)
            {
                if (text.Contains(word)) return true;
            }
            return false;
        }

        private void PlayNow(SoundId id, float volumeScale, Sync sync, float hitIn)
        {
            var sound = library.Get(id);
            if (sound == null || sound.Variants == null || sound.Variants.Length == 0) return;

            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out float last) && now - last < sound.MinInterval) return;
            lastPlayed[id] = now;

            int index = 0;
            if (sound.Variants.Length > 1)
            {
                if (!lastVariant.TryGetValue(id, out int previous)) index = Random.Range(0, sound.Variants.Length); // la prima volta tutte
                else
                {
                    index = Random.Range(0, sound.Variants.Length - 1);
                    if (index >= previous) index++; // mai la stessa variante due volte di fila
                }
            }
            lastVariant[id] = index;
            var variant = sound.Variants[index];
            if (variant == null || variant.Clip == null) return;

            var source = FreeSource();
            source.Stop();
            source.clip = variant.Clip;
            source.volume = Mathf.Clamp01(sound.Volume * volumeScale);
            source.pitch = 1f + (sound.PitchJitter > 0f ? Random.Range(-sound.PitchJitter, sound.PitchJitter) : 0f);
            source.time = 0f;

            float offset = 0f;
            switch (sync)
            {
                case Sync.Onset:
                    offset = Mathf.Max(0f, variant.Onset - 0.01f);
                    break;
                case Sync.Hit:
                    offset = variant.Hit - Mathf.Max(0f, hitIn);
                    break;
            }

            if (offset >= 0f)
            {
                source.time = Mathf.Min(offset, Mathf.Max(0f, variant.Clip.length - 0.05f));
                source.Play();
            }
            else
            {
                source.PlayDelayed(-offset);
            }

            // Effetti che si sovrappongono: vince l'abbassamento piu' forte e la fine piu' lontana, non si sommano.
            float gain = DuckFor(id);
            if (gain < 1f)
            {
                float end = now + Mathf.Max(0f, -offset) + (variant.Clip.length - Mathf.Max(0f, offset)) / Mathf.Max(0.5f, source.pitch);
                duckTarget = now >= duckUntil ? gain : Mathf.Min(duckTarget, gain);
                duckUntil = Mathf.Max(duckUntil, end);
            }
        }

        private AudioSource FreeSource()
        {
            AudioSource best = pool[0];
            float bestTime = float.MaxValue;
            foreach (var source in pool)
            {
                if (!source.isPlaying) return source;
                // Tutte occupate: si riusa quella piu' avanti nel suo file.
                float remaining = source.clip != null ? source.clip.length - source.time : 0f;
                if (remaining < bestTime)
                {
                    bestTime = remaining;
                    best = source;
                }
            }
            return best;
        }

        private void Update()
        {
            UpdateMusic();
            if (Time.unscaledTime >= nextButtonScan)
            {
                nextButtonScan = Time.unscaledTime + ButtonScanInterval;
                HookButtons();
            }
        }

        private void LateUpdate()
        {
            if (pendingUiPriority < 0) return;
            var id = pendingUi;
            pendingUiPriority = -1;
            Play(id, sync: Sync.Onset);
        }

        private void UpdateMusic()
        {
            if (library.Music == null) return;
            if (music.clip != library.Music) music.clip = library.Music;

            bool atTable = SceneManager.GetActiveScene().name == AppFlowManager.SCENE_GAME;
            float target = GameAudioPreferences.MusicEnabled ? (atTable ? library.MusicVolumeTable : library.MusicVolumeHome) : 0f;
            // B35 (AU2): il passo e' una frazione del volume piu' alto, cosi' MusicFadeSeconds e' davvero la durata della dissolvenza.
            float dt = Time.unscaledDeltaTime;
            float range = Mathf.Max(0.01f, Mathf.Max(library.MusicVolumeHome, library.MusicVolumeTable));
            musicLevel = Mathf.MoveTowards(musicLevel, target, dt * range / Mathf.Max(0.05f, library.MusicFadeSeconds));
            if (Time.unscaledTime >= duckUntil) duckTarget = 1f;
            duck = Mathf.MoveTowards(duck, duckTarget, dt / (duckTarget < duck ? 0.08f : 0.8f)); // giu' in fretta, su piano
            music.volume = musicLevel * duck;

            if (target > 0f && !music.isPlaying) music.Play();
            else if (target <= 0f && musicLevel <= 0f && music.isPlaying) music.Pause();
        }

        private void HookButtons()
        {
            foreach (var button in FindObjectsOfType<Button>(true))
            {
                if (button == null || !hookedButtons.Add(button.GetInstanceID())) continue;
                var parent = button.transform.parent;
                var id = ClassifyButton(button.gameObject.name, parent != null ? parent.name : null);
                button.onClick.AddListener(() => PlayUi(id));
            }
        }
    }
}
