using Coffee.UIExtensions;
using Project51.Auth;
using Project51.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project51.UIV2.Animations
{
    /// <summary>Four finite UI emitters, shared resources, no gameplay RNG or blocking raycasts.</summary>
    public sealed class UIV2FeedbackParticles : MonoBehaviour
    {
        private const int Capacity = 4;
        private static UIV2FeedbackParticles instance;
        private readonly UIParticle[] emitters = new UIParticle[Capacity];
        private readonly ParticleSystem[] systems = new ParticleSystem[Capacity];
        private readonly float[] expires = new float[Capacity];
        private int next;
        private Material material;
        private Texture2D texture;
        private PlayerProgressLocal progress;
        private System.Random random;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (instance != null) return;
            var root = new GameObject("K6 feedback", typeof(RectTransform));
            DontDestroyOnLoad(root);
            root.AddComponent<UIV2FeedbackParticles>();
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            random = new System.Random(6151);
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = .5f;
            CreateMaterial();
            for (int i = 0; i < Capacity; i++) CreateEmitter(i);
        }

        private void OnEnable()
        {
            GameFeedback.Presented += Burst;
            GameFeedback.ParticlesDisabled += Clear;
            SceneManager.sceneLoaded += SceneLoaded;
            BindProgress();
        }

        private void BindProgress()
        {
            if (progress == PlayerProgressLocal.Instance) return;
            if (progress != null) progress.OnExpChanged -= ExpChanged;
            progress = PlayerProgressLocal.Instance;
            if (progress != null) progress.OnExpChanged += ExpChanged;
        }

        private void ExpChanged(int total, int gained)
        {
            // B36 (O2, scelta 07/10): solo le particelle. L'XP arriva anche dopo una sconfitta, che non vibra; la vittoria ha la sua.
            if (gained > 0 && Application.isFocused)
                GameFeedback.Present(FeedbackKind.Reward, false, new Vector2(.5f, .78f));
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode) { Clear(); BindProgress(); }

        private void Update()
        {
            BindProgress();
            for (int i = 0; i < Capacity; i++)
                if (emitters[i] != null && emitters[i].gameObject.activeSelf && Time.unscaledTime >= expires[i])
                    Stop(i);
        }

        public void Burst(FeedbackKind kind, Vector2 viewportPosition)
        {
            if (!isActiveAndEnabled || !GameFeedback.ParticlesEnabled || emitters[0] == null) return;
            int slot = next++ % Capacity;
            Stop(slot);
            var ui = emitters[slot];
            var rect = (RectTransform)ui.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(Mathf.Clamp01(viewportPosition.x), Mathf.Clamp01(viewportPosition.y));
            rect.anchoredPosition = Vector2.zero;
            ui.gameObject.SetActive(true);
            ui.Play();
            int count = kind == FeedbackKind.Victory ? 48 : kind == FeedbackKind.Accuso ? 24 : 18;
            for (int i = 0; i < count; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float speed = Mathf.Lerp(80f, kind == FeedbackKind.Victory ? 480f : 270f, (float)random.NextDouble());
                float life = Mathf.Lerp(.45f, .95f, (float)random.NextDouble());
                var particle = new ParticleSystem.EmitParams
                {
                    position = Vector3.zero,
                    velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed,
                    startLifetime = life,
                    startSize = Mathf.Lerp(42f, 72f, (float)random.NextDouble()),
                    startColor = kind == FeedbackKind.Reward ? new Color(0.55f, .95f, 1f, 1f) :
                        Color.Lerp(new Color(1f, .62f, .12f, 1f), new Color(1f, .94f, .65f, 1f), (float)random.NextDouble()),
                    rotation = angle * Mathf.Rad2Deg,
                    randomSeed = (uint)(i + 1)
                };
                systems[slot].Emit(particle, 1);
            }
            expires[slot] = Time.unscaledTime + 1.1f;
        }

        public void Clear()
        {
            for (int i = 0; i < Capacity; i++) Stop(i);
        }

        private void Stop(int slot)
        {
            if (emitters[slot] == null) return;
            emitters[slot].Stop();
            systems[slot].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            emitters[slot].gameObject.SetActive(false);
        }

        private void CreateEmitter(int index)
        {
            var root = new GameObject("Burst " + index, typeof(RectTransform));
            root.SetActive(false);
            root.transform.SetParent(transform, false);
            var child = new GameObject("Particles");
            child.transform.SetParent(root.transform, false);
            var ps = child.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.useUnscaledTime = true;
            ps.useAutoRandomSeed = false;
            ps.randomSeed = (uint)(6151 + index);
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, .1f));
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1f, 0), new GradientAlphaKey(.9f, .45f), new GradientAlphaKey(0f, 1) });
            fade.color = gradient;
            var renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            // UIParticle bakes with a small orthographic camera; its default .5 cap
            // would shrink canvas-sized sparkles into a few pixels.
            renderer.maxParticleSize = 100f;
            var ui = root.AddComponent<UIParticle>();
            ui.raycastTarget = false;
            ui.scale = 1f;
            ui.autoScalingMode = UIParticle.AutoScalingMode.Transform;
            ui.positionMode = UIParticle.PositionMode.Relative;
            ui.RefreshParticles();
            emitters[index] = ui;
            systems[index] = ps;
        }

        private void CreateMaterial()
        {
            texture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            { name = "K6 sparkle", hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float u = Mathf.Abs((x + .5f - 16f) / 15f);
                float v = Mathf.Abs((y + .5f - 16f) / 15f);
                float alpha = Mathf.Clamp01((1f - Mathf.Pow(u, .7f) - Mathf.Pow(v, .7f)) * 10f);
                pixels[y * 32 + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            material = new Material(Shader.Find("UI/Default"))
            { name = "K6 particles", hideFlags = HideFlags.HideAndDontSave, mainTexture = texture };
        }

        private void OnDisable()
        {
            GameFeedback.Presented -= Burst;
            GameFeedback.ParticlesDisabled -= Clear;
            SceneManager.sceneLoaded -= SceneLoaded;
            if (progress != null) progress.OnExpChanged -= ExpChanged;
            progress = null;
            Clear();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (material != null) Destroy(material);
            if (texture != null) Destroy(texture);
        }
    }
}
