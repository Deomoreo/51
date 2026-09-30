using System;
using System.Collections.Generic;
using DG.Tweening;
using Project51.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51
{
    /// <summary>cubic-bezier CSS (x -> y): Newton con ripiego a bisezione, come i browser.</summary>
    public sealed class CubicBezier
    {
        readonly float m_X1, m_Y1, m_X2, m_Y2;

        public CubicBezier(float x1, float y1, float x2, float y2)
        {
            m_X1 = Mathf.Clamp01(x1); m_Y1 = y1; m_X2 = Mathf.Clamp01(x2); m_Y2 = y2;
            Ease = (time, duration, overshoot, period) => Evaluate(duration > 0f ? time / duration : 1f);
        }

        /// <summary>Da passare a DOTween: tween.SetEase(curve.Ease).</summary>
        public EaseFunction Ease { get; }

        static float Comp(float u, float a, float b)
        {
            float v = 1f - u;
            return 3f * v * v * u * a + 3f * v * u * u * b + u * u * u;
        }

        public float Evaluate(float x)
        {
            if (x <= 0f) return 0f;
            if (x >= 1f) return 1f;
            float u = x;
            for (int i = 0; i < 8; i++)
            {
                float err = Comp(u, m_X1, m_X2) - x;
                if (Mathf.Abs(err) < 1e-5f) return Comp(u, m_Y1, m_Y2);
                float v = 1f - u;
                float d = 3f * v * v * m_X1 + 6f * v * u * (m_X2 - m_X1) + 3f * u * u * (1f - m_X2);
                if (Mathf.Abs(d) < 1e-6f) break;
                u -= err / d;
            }
            float lo = 0f, hi = 1f;
            u = x;
            for (int i = 0; i < 24; i++)
            {
                float cx = Comp(u, m_X1, m_X2);
                if (Mathf.Abs(cx - x) < 1e-5f) break;
                if (cx < x) lo = u; else hi = u;
                u = (lo + hi) * 0.5f;
            }
            return Comp(u, m_Y1, m_Y2);
        }
    }

    /// <summary>Curve dei mockup (SPEC §6).</summary>
    public static class UIEase
    {
        public static readonly CubicBezier Linear = new CubicBezier(0f, 0f, 1f, 1f);
        public static readonly CubicBezier Ease = new CubicBezier(0.25f, 0.1f, 0.25f, 1f);
        public static readonly CubicBezier EaseIn = new CubicBezier(0.42f, 0f, 1f, 1f);
        public static readonly CubicBezier EaseOut = new CubicBezier(0f, 0f, 0.58f, 1f);
        public static readonly CubicBezier EaseInOut = new CubicBezier(0.42f, 0f, 0.58f, 1f);
        /// <summary>Pannelli dal basso, barre (cubic .2,.8,.3,1).</summary>
        public static readonly CubicBezier Sheet = new CubicBezier(0.2f, 0.8f, 0.3f, 1f);
        /// <summary>Overshoot (cubic .2,.8,.3,1.2): medaglione ACCUSA, titolo fine partita, pop del mazzo.</summary>
        public static readonly CubicBezier PopBack = new CubicBezier(0.2f, 0.8f, 0.3f, 1.2f);
        /// <summary>Ventaglio delle carte (cubic .2,.8,.3,1.1).</summary>
        public static readonly CubicBezier Fan = new CubicBezier(0.2f, 0.8f, 0.3f, 1.1f);
        /// <summary>Pugno dell'accuso (cubic .3,.7,.3,1).</summary>
        public static readonly CubicBezier Fist = new CubicBezier(0.3f, 0.7f, 0.3f, 1f);
        /// <summary>Ruota del mazziere (cubic .12,.75,.18,1).</summary>
        public static readonly CubicBezier Wheel = new CubicBezier(0.12f, 0.75f, 0.18f, 1f);
    }

    /// <summary>Canali animabili, in unita' CSS: X/Y in px (Y verso il basso), rotazioni in gradi orari, scala e alpha assoluti.</summary>
    public enum AnimProp { X, Y, Rotation, Scale, Alpha, RotationY }

    /// <summary>
    /// Traduzione di un @keyframes CSS: una traccia per canale, curva applicata a ogni segmento fra due keyframe
    /// (come animation-timing-function). Posizione e rotazione si sommano alla posa di partenza, la scala la moltiplica,
    /// l'alpha va su un CanvasGroup (aggiunto se manca). Scrivere i transform completi per ogni keyframe che ne ha uno
    /// (in CSS le funzioni omesse valgono l'identita').
    /// </summary>
    public sealed class UIKeyframes
    {
        struct Key { public float t, v; }

        readonly Dictionary<AnimProp, List<Key>> m_Tracks = new Dictionary<AnimProp, List<Key>>();
        public float Duration { get; }
        public CubicBezier Curve { get; }

        public UIKeyframes(float duration, CubicBezier curve)
        {
            Duration = duration;
            Curve = curve ?? UIEase.Ease;
        }

        /// <summary>Coppie (tempo 0-1, valore): Track(AnimProp.Scale, 0f, 0.4f, 0.7f, 1.08f, 1f, 1f).</summary>
        public UIKeyframes Track(AnimProp prop, params float[] timeValuePairs)
        {
            var keys = new List<Key>();
            for (int i = 0; i + 1 < timeValuePairs.Length; i += 2) keys.Add(new Key { t = timeValuePairs[i], v = timeValuePairs[i + 1] });
            keys.Sort((a, b) => a.t.CompareTo(b.t));
            m_Tracks[prop] = keys;
            return this;
        }

        public bool Has(AnimProp prop) => m_Tracks.ContainsKey(prop);

        /// <summary>Valore del canale al tempo normalizzato t (fuori dai keyframe resta costante).</summary>
        public float Evaluate(AnimProp prop, float t, float fallback)
        {
            if (!m_Tracks.TryGetValue(prop, out var keys) || keys.Count == 0) return fallback;
            if (t <= keys[0].t) return keys[0].v;
            for (int i = 1; i < keys.Count; i++)
            {
                if (t > keys[i].t) continue;
                float span = keys[i].t - keys[i - 1].t;
                float u = span > 0f ? (t - keys[i - 1].t) / span : 1f;
                return Mathf.LerpUnclamped(keys[i - 1].v, keys[i].v, Curve.Evaluate(u));
            }
            return keys[keys.Count - 1].v;
        }

        /// <summary>
        /// Avvia su target. Come "fill-mode: both": la posa iniziale vale gia' durante il ritardo e quella finale resta.
        /// loops -1 = infinito; yoyo = "alternate". Durate e ritardi passano da GamePreferences.Scaled.
        /// Ucciso (anche da SetLink alla disattivazione) salta alla posa finale; un loop infinito torna alla posa di riposo.
        /// </summary>
        public Tween Play(RectTransform target, float delay = 0f, int loops = 1, bool yoyo = false)
        {
            if (target == null) return null;
            DOTween.Kill(target);
            var pose = Pose.Capture(target, Has(AnimProp.Alpha));
            Apply(pose, 0f);
            bool infinite = loops < 0;
            float end = yoyo && loops % 2 == 0 ? 0f : 1f;
            return DOVirtual.Float(0f, 1f, GamePreferences.Scaled(Duration), t => Apply(pose, t))
                .SetEase(Ease.Linear)
                .SetDelay(GamePreferences.Scaled(delay))
                .SetLoops(loops, yoyo ? LoopType.Yoyo : LoopType.Restart)
                .SetUpdate(true)
                .SetLink(target.gameObject, LinkBehaviour.KillOnDisable)
                .SetId(target)
                .OnKill(() =>
                {
                    if (infinite) pose.Restore();
                    else Apply(pose, end);
                });
        }

        void Apply(Pose pose, float t)
        {
            pose.Write(
                new Vector2(Evaluate(AnimProp.X, t, 0f), -Evaluate(AnimProp.Y, t, 0f)),
                new Vector3(0f, Evaluate(AnimProp.RotationY, t, 0f), -Evaluate(AnimProp.Rotation, t, 0f)),
                Evaluate(AnimProp.Scale, t, 1f),
                Evaluate(AnimProp.Alpha, t, 1f));
        }

        /// <summary>
        /// Posa di riposo del target. Se l'ultima animazione ha lasciato degli offset (es. ventaglio) e nessuno ha
        /// piu' toccato il transform, la riposo e' quella di prima: rilanciare non accumula spostamenti.
        /// </summary>
        sealed class Pose
        {
            static readonly Dictionary<RectTransform, Pose> s_Last = new Dictionary<RectTransform, Pose>();

            readonly RectTransform m_Rect;
            readonly CanvasGroup m_Group;
            readonly Vector2 m_Position;
            readonly Vector3 m_Euler, m_Scale;
            readonly float m_Alpha;
            Vector2 m_WrotePosition;
            Quaternion m_WroteRotation;
            Vector3 m_WroteScale;

            Pose(RectTransform r, bool alpha, Pose last)
            {
                m_Rect = r;
                m_Position = last != null && r.anchoredPosition == last.m_WrotePosition ? last.m_Position : r.anchoredPosition;
                m_Euler = last != null && Quaternion.Angle(r.localRotation, last.m_WroteRotation) < 0.01f ? last.m_Euler : r.localEulerAngles;
                m_Scale = last != null && r.localScale == last.m_WroteScale ? last.m_Scale : r.localScale;
                if (alpha && !r.TryGetComponent(out m_Group)) m_Group = r.gameObject.AddComponent<CanvasGroup>();
                m_Alpha = m_Group != null ? m_Group.alpha : 1f;
                m_WrotePosition = r.anchoredPosition;
                m_WroteRotation = r.localRotation;
                m_WroteScale = r.localScale;
            }

            public static Pose Capture(RectTransform r, bool alpha)
            {
                s_Last.TryGetValue(r, out var last);
                var pose = new Pose(r, alpha, last);
                if (s_Last.Count > 256)
                {
                    var dead = new List<RectTransform>();
                    foreach (var k in s_Last.Keys) if (k == null) dead.Add(k);
                    foreach (var k in dead) s_Last.Remove(k);
                }
                s_Last[r] = pose;
                return pose;
            }

            public void Write(Vector2 offset, Vector3 euler, float scale, float alpha)
            {
                if (m_Rect == null) return;
                m_Rect.anchoredPosition = m_WrotePosition = m_Position + offset;
                m_Rect.localEulerAngles = m_Euler + euler;
                m_WroteRotation = m_Rect.localRotation;
                m_Rect.localScale = m_WroteScale = m_Scale * scale;
                if (m_Group != null) m_Group.alpha = alpha;
            }

            public void Restore()
            {
                Write(Vector2.zero, Vector3.zero, 1f, m_Alpha);
            }
        }
    }

    /// <summary>Pezzi dell'animazione Accuso al centro del tavolo (SPEC §6). I campi mancanti vengono saltati.</summary>
    [Serializable]
    public class AccusoParts
    {
        public RectTransform root;
        public RectTransform dim;
        public RectTransform fist;
        public RectTransform ring1;
        public RectTransform ring2;
        public RectTransform glow;
        public RectTransform text;
        public RectTransform table;
    }

    /// <summary>
    /// Preset della SPEC §6, valori presi dai @keyframes dei mockup. Tutti indipendenti dal timeScale (SetUpdate true),
    /// scalati da "animazioni veloci" e legati al GameObject (si fermano quando si disattiva).
    /// I loop decorativi (pulse, riflessi, fluttuazioni, raggi) non partono con la grafica ridotta.
    /// </summary>
    public static class UIAnim
    {
        public static bool DecorativeLoops => !GamePreferences.ReducedGraphics;

        // ---------------------------------------------------------------- entrate / uscite

        /// <summary>Pop (dialog, chip, badge): scala from -> peak (a peakAt) -> 1, alpha 0 -> 1. peak &lt;= 1 = senza rimbalzo.</summary>
        public static Tween Pop(RectTransform t, float from = 0.4f, float peak = 1.08f, float duration = 0.3f, float delay = 0f,
            float peakAt = 0.7f, CubicBezier curve = null)
        {
            var k = new UIKeyframes(duration, curve ?? UIEase.EaseOut).Track(AnimProp.Alpha, 0f, 0f, 1f, 1f);
            if (peak > 1f) k.Track(AnimProp.Scale, 0f, from, peakAt, peak, 1f, 1f);
            else k.Track(AnimProp.Scale, 0f, from, 1f, 1f);
            return k.Play(t, delay);
        }

        /// <summary>Pop del dialog/overlay di connessione (.28 s, 0.85 -> 1).</summary>
        public static Tween PopDialog(RectTransform t) => Pop(t, 0.85f, 1f, 0.28f);

        /// <summary>Titolo di fine partita (.5 s, cubic .2,.8,.3,1.2, 0.3 -> 1.1 a 65%).</summary>
        public static Tween PopTitle(RectTransform t, float delay = 0f) => Pop(t, 0.3f, 1.1f, 0.5f, delay, 0.65f, UIEase.PopBack);

        /// <summary>Uscita (non nei mockup, speculare al pop): scala 1 -> 0.85, alpha 1 -> 0.</summary>
        public static Tween PopOut(RectTransform t, float duration = 0.18f) =>
            new UIKeyframes(duration, UIEase.EaseIn).Track(AnimProp.Scale, 0f, 1f, 1f, 0.85f).Track(AnimProp.Alpha, 0f, 1f, 1f, 0f).Play(t);

        /// <summary>SheetUp: translateY 100% -> 0 (.28 s, cubic .2,.8,.3,1).</summary>
        public static Tween SheetUp(RectTransform t, float duration = 0.28f) =>
            new UIKeyframes(duration, UIEase.Sheet).Track(AnimProp.Y, 0f, Height(t), 1f, 0f).Play(t);

        /// <summary>Chiusura del pannello: 0 -> 100% (non nei mockup).</summary>
        public static Tween SheetDown(RectTransform t, float duration = 0.22f) =>
            new UIKeyframes(duration, UIEase.EaseIn).Track(AnimProp.Y, 0f, 0f, 1f, Height(t)).Play(t);

        /// <summary>Dissolvenza (scrim dei pannelli: .35 s ease-out).</summary>
        public static Tween FadeIn(RectTransform t, float duration = 0.35f, float delay = 0f) =>
            new UIKeyframes(duration, UIEase.EaseOut).Track(AnimProp.Alpha, 0f, 0f, 1f, 1f).Play(t, delay);

        public static Tween FadeOut(RectTransform t, float duration = 0.2f) =>
            new UIKeyframes(duration, UIEase.EaseIn).Track(AnimProp.Alpha, 0f, 1f, 1f, 0f).Play(t);

        /// <summary>fadeUp: sale di distance px entrando (fine smazzata 10 px .4 s, fine partita 14 px .45 s).</summary>
        public static Tween FadeUp(RectTransform t, float delay = 0f, float distance = 14f, float duration = 0.45f) =>
            new UIKeyframes(duration, UIEase.EaseOut).Track(AnimProp.Y, 0f, distance, 1f, 0f).Track(AnimProp.Alpha, 0f, 0f, 1f, 1f).Play(t, delay);

        /// <summary>rowIn: righe dei punteggi da sinistra (.35 s, stagger 0.18 s).</summary>
        public static Tween RowIn(RectTransform t, float delay = 0f) =>
            new UIKeyframes(0.35f, UIEase.EaseOut).Track(AnimProp.X, 0f, -14f, 1f, 0f).Track(AnimProp.Alpha, 0f, 0f, 1f, 1f).Play(t, delay);

        /// <summary>RowIn in sequenza con lo stagger della SPEC.</summary>
        public static void RowsIn(IList<RectTransform> rows, float stagger = 0.18f, float delay = 0f)
        {
            for (int i = 0; i < rows.Count; i++) RowIn(rows[i], delay + i * stagger);
        }

        /// <summary>rise (Sorteggio): translateY 14 -> 0 con alpha.</summary>
        public static Tween Rise(RectTransform t, float delay = 0.15f, float duration = 0.4f) => FadeUp(t, delay, 14f, duration);

        /// <summary>slam: entra dall'alto sbattendo (.5 s).</summary>
        public static Tween Slam(RectTransform t, float delay = 0f) =>
            new UIKeyframes(0.5f, UIEase.EaseOut)
                .Track(AnimProp.Y, 0f, -40f, 0.55f, 4f, 1f, 0f)
                .Track(AnimProp.Scale, 0f, 1.5f, 0.55f, 0.9f, 1f, 1f)
                .Track(AnimProp.Alpha, 0f, 0f, 0.55f, 1f, 1f, 1f)
                .Play(t, delay);

        /// <summary>check (Premi): spunta 0 -> 1.2 -> 1.</summary>
        public static Tween Check(RectTransform t, float delay = 0f) =>
            new UIKeyframes(0.3f, UIEase.EaseOut).Track(AnimProp.Scale, 0f, 0f, 0.7f, 1.2f, 1f, 1f).Play(t, delay);

        /// <summary>thump (Collezione, tocco sul forziere/carta): saltello di .6 s.</summary>
        public static Tween Thump(RectTransform t) =>
            new UIKeyframes(0.6f, UIEase.EaseOut)
                .Track(AnimProp.Y, 0f, 0f, 0.25f, -22f, 0.45f, 8f, 0.6f, -4f, 1f, 0f)
                .Track(AnimProp.Scale, 0f, 1f, 0.25f, 1.08f, 0.45f, 0.94f, 0.6f, 1.02f, 1f, 1f)
                .Track(AnimProp.Rotation, 0f, 0f, 0.25f, -6f, 0.45f, 0f, 1f, 0f)
                .Play(t);

        /// <summary>tip (Caricamento): suggerimento che entra, resta e svanisce in 3.2 s.</summary>
        public static Tween Tip(RectTransform t, float duration = 3.2f) =>
            new UIKeyframes(duration, UIEase.EaseInOut)
                .Track(AnimProp.Alpha, 0f, 0f, 0.12f, 1f, 0.88f, 1f, 1f, 0f)
                .Track(AnimProp.Y, 0f, 6f, 0.12f, 0f, 0.88f, 0f, 1f, -6f)
                .Play(t);

        // ---------------------------------------------------------------- carte e mazzo

        /// <summary>
        /// Ventaglio (viewer scope/carte accusate): ogni carta da translateY 90 rotate 0 scale .45 alla sua posa,
        /// con rotazione off*9 gradi e y |off|*6 px (off = distanza dal centro). La posizione attuale e' lo slot;
        /// pivot consigliato (0.5, 0.1) = transform-origin 50% 90%.
        /// </summary>
        public static void Fan(IList<RectTransform> cards, float stagger = 0.06f, float degPerCard = 9f, float yPerCard = 6f)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                float off = i - (cards.Count - 1) * 0.5f;
                new UIKeyframes(0.45f, UIEase.Fan)
                    .Track(AnimProp.Y, 0f, 90f, 1f, Mathf.Abs(off) * yPerCard)
                    .Track(AnimProp.Rotation, 0f, 0f, 1f, off * degPerCard)
                    .Track(AnimProp.Scale, 0f, 0.45f, 1f, 1f)
                    .Track(AnimProp.Alpha, 0f, 0f, 1f, 1f)
                    .Play(cards[i], i * stagger);
            }
        }

        /// <summary>flip: rotateY 90 -> 0 (.35 s dopo .25 s). La faccia va impostata prima di chiamarlo.</summary>
        public static Tween Flip(RectTransform t, float delay = 0.25f) =>
            new UIKeyframes(0.35f, UIEase.EaseOut).Track(AnimProp.RotationY, 0f, 90f, 1f, 0f).Play(t, delay);

        /// <summary>deckLift: il mazzo (ruotato -8) sale di 8 px raddrizzandosi di 6 gradi e torna (.5 s).</summary>
        public static Tween DeckLift(RectTransform deck) =>
            new UIKeyframes(0.5f, UIEase.EaseOut)
                .Track(AnimProp.Rotation, 0f, 0f, 0.35f, 6f, 1f, 0f)
                .Track(AnimProp.Y, 0f, 0f, 0.35f, -8f, 1f, 0f)
                .Track(AnimProp.Scale, 0f, 1f, 0.35f, 1.06f, 1f, 1f)
                .Play(deck);

        /// <summary>deckPop: medaglione del conteggio (.4 s dopo .12 s, overshoot). La y e' gia' moltiplicata per la scala (ordine CSS).</summary>
        public static Tween DeckPop(RectTransform medal, float delay = 0.12f) =>
            new UIKeyframes(0.4f, UIEase.PopBack)
                .Track(AnimProp.Scale, 0f, 0.3f, 0.6f, 1.12f, 1f, 1f)
                .Track(AnimProp.Y, 0f, 3f, 0.6f, -2.24f, 1f, 0f)
                .Track(AnimProp.Alpha, 0f, 0f, 0.6f, 1f, 1f, 1f)
                .Play(medal, delay);

        /// <summary>Conteggio a scatti (medaglione del mazzo: +2 ogni 30 ms).</summary>
        public static Tween CountSteps(TMP_Text text, int from, int to, int step = 2, float interval = 0.03f, string format = "{0}")
        {
            if (text == null) return null;
            DOTween.Kill(text);
            step = Mathf.Max(1, Mathf.Abs(step)) * (to >= from ? 1 : -1);
            int steps = Mathf.Max(1, Mathf.CeilToInt((to - from) / (float)step));
            text.text = string.Format(format, from);
            return DOVirtual.Float(0f, steps, GamePreferences.Scaled(steps * interval), v =>
                {
                    int n = from + step * Mathf.FloorToInt(v);
                    text.text = string.Format(format, step > 0 ? Mathf.Min(n, to) : Mathf.Max(n, to));
                })
                .SetEase(Ease.Linear).SetUpdate(true).SetLink(text.gameObject).SetId(text)
                .OnKill(() => { if (text != null) text.text = string.Format(format, to); });
        }

        // ---------------------------------------------------------------- accusa / accuso

        /// <summary>slideIn del medaglione ACCUSA: da destra ruotando (90 px, 30 gradi) con overshoot, .6 s.</summary>
        public static Tween SlideInAccusa(RectTransform t, float delay = 0f) =>
            new UIKeyframes(0.6f, UIEase.PopBack)
                .Track(AnimProp.X, 0f, 90f, 0.6f, -8f, 1f, 0f)
                .Track(AnimProp.Rotation, 0f, 30f, 0.6f, -6f, 1f, 0f)
                .Track(AnimProp.Alpha, 0f, 0f, 0.6f, 1f, 1f, 1f)
                .Play(t, delay);

        /// <summary>
        /// Riflesso diagonale (shine 2.4 s dopo 1 s, ease-in-out): la striscia (larga 40%, ancorata a sinistra
        /// con pivot x 0, gia' inclinata di -20 gradi) scorre da -60% a 130% della larghezza del contenitore.
        /// </summary>
        public static Tween Shine(RectTransform streak, float period = 2.4f, float delay = 1f)
        {
            if (streak == null || !DecorativeLoops) return null;
            float w = Width(streak.parent as RectTransform);
            return new UIKeyframes(period, UIEase.EaseInOut).Track(AnimProp.X, 0f, -0.6f * w, 1f, 1.3f * w).Play(streak, delay, -1);
        }

        /// <summary>
        /// Anello che pulsa (box-shadow 0 0 0 0 -> spread px e alpha -> 0, ease-in-out, andata e ritorno).
        /// ring = forma dietro l'elemento, stesso rettangolo e raggio dell'elemento, riempita col colore dell'anello.
        /// Tuo turno 1.8 s / 7 px / .55; ACCUSA 1.6 s / 10 px / .6 dopo .6 s; giorno di oggi 1.8 s / 8 px / .55;
        /// vincitore del sorteggio 1.4 s / 12 px / .7.
        /// </summary>
        public static Tween Pulse(UI51Shape ring, float spread = 7f, float period = 1.8f, float alpha = 0.55f, float delay = 0f)
        {
            if (ring == null) return null;
            var rt = ring.rectTransform;
            DOTween.Kill(ring); // il vecchio OnKill rimette dimensione e raggi di riposo
            var baseSize = rt.sizeDelta;
            var baseRadii = ring.radii;
            var color = ring.color;
            void Set(float f)
            {
                if (ring == null) return;
                rt.sizeDelta = baseSize + Vector2.one * (2f * spread * f);
                ring.radii = baseRadii + Vector4.one * (spread * f);
                ring.color = UI51Tokens.WithAlpha(color, alpha * (1f - f));
            }
            void Rest()
            {
                if (ring == null) return;
                rt.sizeDelta = baseSize;
                ring.radii = baseRadii;
                ring.color = UI51Tokens.WithAlpha(color, 0f);
            }
            if (!DecorativeLoops) { Rest(); return null; }
            Set(0f);
            return DOVirtual.Float(0f, 1f, GamePreferences.Scaled(period * 0.5f), Set)
                .SetEase(UIEase.EaseInOut.Ease).SetLoops(-1, LoopType.Yoyo)
                .SetDelay(GamePreferences.Scaled(delay)).SetUpdate(true)
                .SetLink(ring.gameObject, LinkBehaviour.KillOnDisable).SetId(ring)
                .OnKill(Rest);
        }

        /// <summary>
        /// Accuso al centro (SPEC §6): scurisce (0.3 s), pugno che cade e rimbalza (0.6 s), due onde d'urto oro
        /// (0.9 s, la seconda parte a 0.62 s), bagliore, tavolo che trema (0.35 s, +-5 px), testo (0.35 s dopo 0.45 s).
        /// Si chiude a 2.6 s (root disattivato) e chiama onDone.
        /// </summary>
        public static Sequence Accuso(AccusoParts p, Action onDone = null)
        {
            if (p == null) return null;
            if (p.root != null) p.root.gameObject.SetActive(true);
            if (p.dim != null) new UIKeyframes(0.3f, UIEase.EaseOut).Track(AnimProp.Alpha, 0f, 0f, 1f, 1f).Play(p.dim);
            if (p.fist != null)
                new UIKeyframes(0.6f, UIEase.Fist)
                    .Track(AnimProp.Y, 0f, -120f, 0.45f, 6f, 0.6f, -4f, 1f, 0f)
                    .Track(AnimProp.Scale, 0f, 2.2f, 0.45f, 0.88f, 0.6f, 1.04f, 1f, 1f)
                    .Track(AnimProp.Rotation, 0f, -18f, 0.45f, 4f, 0.6f, 0f, 1f, 0f)
                    .Track(AnimProp.Alpha, 0f, 0f, 0.45f, 1f, 1f, 1f)
                    .Play(p.fist);
            AccusoRing(p.ring1, 0.28f);
            AccusoRing(p.ring2, 0.62f); // animation-delay .62s del mockup
            if (p.glow != null)
                new UIKeyframes(0.8f, UIEase.EaseOut)
                    .Track(AnimProp.Alpha, 0f, 0f, 0.4f, 0.95f, 1f, 0.6f)
                    .Track(AnimProp.Scale, 0f, 0.4f, 0.4f, 1.1f, 1f, 1f)
                    .Play(p.glow, 0.25f);
            if (p.table != null) Shake(p.table, 0.3f);
            if (p.text != null)
                new UIKeyframes(0.35f, UIEase.EaseOut)
                    .Track(AnimProp.Y, 0f, 12f, 1f, 0f)
                    .Track(AnimProp.Scale, 0f, 0.9f, 1f, 1f)
                    .Track(AnimProp.Alpha, 0f, 0f, 1f, 1f)
                    .Play(p.text, 0.45f);

            var seq = DOTween.Sequence().AppendInterval(GamePreferences.Scaled(2.6f)).AppendCallback(() =>
            {
                if (p.root != null) p.root.gameObject.SetActive(false);
                onDone?.Invoke();
            });
            seq.SetUpdate(true);
            if (p.root != null) seq.SetLink(p.root.gameObject);
            return seq;
        }

        static void AccusoRing(RectTransform ring, float delay)
        {
            if (ring == null) return;
            new UIKeyframes(0.9f, UIEase.EaseOut)
                .Track(AnimProp.Scale, 0f, 0.3f, 1f, 2.3f)
                .Track(AnimProp.Alpha, 0f, 0f, 0.1f, 1f, 1f, 0f)
                .Play(ring, delay);
        }

        static readonly UIKeyframes ShakeKeys = new UIKeyframes(0.35f, UIEase.EaseInOut)
            .Track(AnimProp.X, 0f, 0f, 0.2f, -5f, 0.4f, 4f, 0.6f, -3f, 0.8f, 2f, 1f, 0f)
            .Track(AnimProp.Y, 0f, 0f, 0.2f, 2f, 0.4f, -3f, 0.6f, 1f, 0.8f, -1f, 1f, 0f);

        /// <summary>shake del tavolo (.35 s ease-in-out).</summary>
        public static Tween Shake(RectTransform t, float delay = 0f) => ShakeKeys.Play(t, delay);

        /// <summary>Spostamento dello shake al tempo normalizzato t, in px CSS (y in giu').</summary>
        public static Vector2 ShakeOffset(float t) =>
            new Vector2(ShakeKeys.Evaluate(AnimProp.X, t, 0f), ShakeKeys.Evaluate(AnimProp.Y, t, 0f));

        /// <summary>
        /// Lo stesso shake su un oggetto del mondo (il tavolo, fuori dalla UI): unit = mondo per px del mockup.
        /// Riparte dalla posa di riposo anche se ne interrompe uno in corso.
        /// </summary>
        public static Tween ShakeWorld(Transform t, float unit, float delay = 0f)
        {
            if (t == null || unit <= 0f) return null;
            DOTween.Kill(t); // l'OnKill del precedente rimette la posa di riposo
            var rest = t.localPosition;
            return DOVirtual.Float(0f, 1f, GamePreferences.Scaled(ShakeKeys.Duration), v =>
                {
                    if (t == null) return;
                    var o = ShakeOffset(v) * unit;
                    t.localPosition = rest + new Vector3(o.x, -o.y, 0f);
                })
                .SetEase(Ease.Linear).SetDelay(GamePreferences.Scaled(delay)).SetUpdate(true)
                .SetLink(t.gameObject).SetId(t)
                .OnKill(() => { if (t != null) t.localPosition = rest; });
        }

        /// <summary>shake orizzontale (overlay connessione, errore: .45 s dopo .15 s).</summary>
        public static Tween ShakeX(RectTransform t, float delay = 0.15f) =>
            new UIKeyframes(0.45f, UIEase.EaseInOut)
                .Track(AnimProp.X, 0f, 0f, 0.2f, -5f, 0.4f, 5f, 0.6f, -3f, 0.8f, 3f, 1f, 0f)
                .Play(t, delay);

        // ---------------------------------------------------------------- sorteggio

        /// <summary>Angolo CSS finale della ruota: 6 giri, spicchio del vincitore sotto la lancetta, piccolo scarto casuale.</summary>
        public static float WheelTarget(int winner, int slices, float turns = 6f)
        {
            float seg = 360f / Mathf.Max(1, slices);
            return 360f * turns - (winner * seg + seg * 0.5f) + UnityEngine.Random.Range(-seg * 0.25f, seg * 0.25f);
        }

        /// <summary>
        /// Ruota del mazziere: da 0 all'angolo CSS finale in 3.4 s (cubic .12,.75,.18,1); la lancetta ticchetta
        /// (0 -> -14 -> 0 gradi ogni 0.12 s) finche' gira. delay = partenza automatica (~1.2 s nel mockup).
        /// </summary>
        public static Sequence DealerWheel(RectTransform wheel, RectTransform needle, float targetCssDeg, float delay = 1.2f,
            float duration = 3.4f, Action onDone = null)
        {
            if (wheel == null) return null;
            DOTween.Kill(wheel);
            var baseEuler = wheel.localEulerAngles;
            var seq = DOTween.Sequence().SetId(wheel);
            seq.AppendInterval(GamePreferences.Scaled(delay));
            if (needle != null)
                seq.AppendCallback(() =>
                    new UIKeyframes(0.12f, UIEase.Linear).Track(AnimProp.Rotation, 0f, 0f, 0.5f, -14f, 1f, 0f).Play(needle, 0f, -1));
            seq.Append(DOVirtual.Float(0f, targetCssDeg, GamePreferences.Scaled(duration),
                a => wheel.localEulerAngles = baseEuler + new Vector3(0f, 0f, -a)).SetEase(UIEase.Wheel.Ease));
            seq.AppendCallback(() =>
            {
                if (needle != null) DOTween.Kill(needle);
                onDone?.Invoke();
            });
            return seq.SetUpdate(true).SetLink(wheel.gameObject);
        }

        // ---------------------------------------------------------------- caricamento / connessione

        /// <summary>Onda di caricamento: 5 dorsi in fila, 1.4 s in loop, sfasati di 0.12 s.</summary>
        public static void LoadingWave(IList<RectTransform> backs, float period = 1.4f, float stagger = 0.12f)
        {
            for (int i = 0; i < backs.Count; i++)
                new UIKeyframes(period, UIEase.EaseInOut)
                    .Track(AnimProp.Y, 0f, 8f, 0.25f, -10f, 0.5f, -14f, 0.75f, -6f, 1f, 8f)
                    .Track(AnimProp.Rotation, 0f, 0f, 0.25f, -4f, 0.5f, 0f, 0.75f, 4f, 1f, 0f)
                    .Play(backs[i], i * stagger, -1);
        }

        /// <summary>Rotazione continua in senso orario (spinner riconnessione 1.1 s, raggi di fine partita 18 s).</summary>
        public static Tween Spin(RectTransform t, float period = 1.1f, bool decorative = false)
        {
            if (t == null || (decorative && !DecorativeLoops)) return null;
            return new UIKeyframes(period, UIEase.Linear).Track(AnimProp.Rotation, 0f, 0f, 1f, 360f).Play(t, 0f, -1);
        }

        /// <summary>Raggi di luce di fine partita (18 s a giro).</summary>
        public static Tween Rays(RectTransform t) => Spin(t, 18f, true);

        /// <summary>Alpha che pulsa (archi Wi-Fi: .25 -> 1, 1.2 s, sfasati 0.2 s; bagliore del logo .55 -> .95, 3 s).</summary>
        public static Tween Blink(RectTransform t, float from = 0.25f, float to = 1f, float period = 1.2f, float delay = 0f) =>
            new UIKeyframes(period, UIEase.EaseInOut).Track(AnimProp.Alpha, 0f, from, 0.5f, to, 1f, from).Play(t, delay, -1);

        /// <summary>Archi Wi-Fi dell'overlay di connessione, sfasati di 0.2 s.</summary>
        public static void WifiWave(IList<RectTransform> arcs, float stagger = 0.2f)
        {
            for (int i = 0; i < arcs.Count; i++) Blink(arcs[i], 0.25f, 1f, 1.2f, i * stagger);
        }

        // ---------------------------------------------------------------- decorativi

        /// <summary>Fluttua su e giu' (forziere Negozio 8 px / 3.2 s, premio 5 px / 2.6 s).</summary>
        public static Tween Float(RectTransform t, float px = 5f, float period = 2.6f)
        {
            if (t == null || !DecorativeLoops) return null;
            return new UIKeyframes(period, UIEase.EaseInOut).Track(AnimProp.Y, 0f, 0f, 0.5f, -px, 1f, 0f).Play(t, 0f, -1);
        }

        /// <summary>Respiro del logo di caricamento (scala 1 -> 1.04, 3 s).</summary>
        public static Tween Breathe(RectTransform t, float amount = 1.04f, float period = 3f)
        {
            if (t == null || !DecorativeLoops) return null;
            return new UIKeyframes(period, UIEase.EaseInOut).Track(AnimProp.Scale, 0f, 1f, 0.5f, amount, 1f, 1f).Play(t, 0f, -1);
        }

        // ---------------------------------------------------------------- premi / barre / emoticon

        /// <summary>Esplosione del bagliore al riscatto (1.1 s): scala 0.2 -> 1.4, alpha 0 -> 1 -> 0.</summary>
        public static Tween RewardBurst(RectTransform glow) =>
            new UIKeyframes(1.1f, UIEase.EaseOut)
                .Track(AnimProp.Scale, 0f, 0.2f, 1f, 1.4f)
                .Track(AnimProp.Alpha, 0f, 0f, 0.3f, 1f, 1f, 0f)
                .Play(glow);

        /// <summary>"+150" che sale e svanisce (1.8 s).</summary>
        public static Tween RewardRise(RectTransform label) =>
            new UIKeyframes(1.8f, UIEase.EaseOut)
                .Track(AnimProp.Y, 0f, 30f, 0.25f, 0f, 0.35f, 0f, 1f, -30f)
                .Track(AnimProp.Scale, 0f, 0.6f, 0.25f, 1.1f, 0.35f, 1f, 1f, 1f)
                .Track(AnimProp.Alpha, 0f, 0f, 0.25f, 1f, 0.8f, 1f, 1f, 0f)
                .Play(label);

        /// <summary>
        /// Barra che si riempie da una frazione all'altra (corsa al 51: 1 s dopo 1.9 s; XP: 1.3 s dopo 1.2 s; cubic .2,.8,.3,1).
        /// La barra e' ancorata a sinistra e si allarga muovendo anchorMax.x.
        /// </summary>
        public static Tween Fill(RectTransform bar, float from, float to, float duration = 1f, float delay = 1.9f)
        {
            if (bar == null) return null;
            DOTween.Kill(bar);
            void Set(float f)
            {
                if (bar == null) return;
                var max = bar.anchorMax;
                max.x = bar.anchorMin.x + Mathf.Clamp01(f) * (1f - bar.anchorMin.x);
                bar.anchorMax = max;
            }
            Set(from);
            return DOVirtual.Float(from, to, GamePreferences.Scaled(duration), Set)
                .SetEase(UIEase.Sheet.Ease).SetDelay(GamePreferences.Scaled(delay)).SetUpdate(true)
                .SetLink(bar.gameObject, LinkBehaviour.KillOnDisable).SetId(bar)
                .OnKill(() => Set(to));
        }

        /// <summary>Barra XP di fine partita (1.3 s dopo 1.2 s).</summary>
        public static Tween XpBar(RectTransform bar, float from, float to) => Fill(bar, from, to, 1.3f, 1.2f);

        /// <summary>
        /// Emoticon in volo (emoFly 2.6 s): sale da 130 px sotto, rimbalza, resta e svanisce salendo.
        /// Il fumetto contiene un EmoticonPlayer.
        /// </summary>
        public static Tween EmoticonBubble(RectTransform bubble) =>
            new UIKeyframes(2.6f, UIEase.EaseOut)
                .Track(AnimProp.Y, 0f, 130f, 0.18f, -6f, 0.28f, 0f, 0.82f, -10f, 1f, -34f)
                .Track(AnimProp.Scale, 0f, 0.35f, 0.18f, 1.08f, 0.28f, 1f, 0.82f, 1f, 1f, 0.9f)
                .Track(AnimProp.Alpha, 0f, 0f, 0.18f, 1f, 0.82f, 1f, 1f, 0f)
                .Play(bubble);

        // ---------------------------------------------------------------- util

        public static void Stop(Component target) { if (target != null) DOTween.Kill(target); }

        static float Height(RectTransform t) => t != null ? t.rect.height : 0f;
        static float Width(RectTransform t) => t != null ? t.rect.width : 0f;
    }
}
