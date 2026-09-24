using DG.Tweening;
using Project51.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Animations
{
    /// <summary>
    /// Micro-movimento d'ambiente (sfondo Home): dalla posa iniziale fino a posa + escursione e
    /// ritorno, all'infinito, InOutSine. Un solo tween per elemento, niente Update.
    /// La posa iniziale torna esatta quando si spegne. Fermo con Grafica ridotta.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIV2AmbientFloat : MonoBehaviour
    {
        [SerializeField] private float moveX;
        [SerializeField] private float moveY;
        [SerializeField, Tooltip("Gradi")] private float rotation;
        [SerializeField, Tooltip("0.018 = da 1 a 1.018")] private float scaleAmount;
        [SerializeField, Tooltip("Alpha aggiunto alla Graphic (glow)")] private float alphaAmount;
        [SerializeField, Min(0.1f), Tooltip("Secondi per mezzo ciclo")] private float duration = 8f;
        [SerializeField, Min(0f), Tooltip("Secondi di vantaggio: elementi mai sincronizzati")] private float phase;

        private Tween motion;
        private Vector2 restPosition;
        private Vector3 restEuler, restScale;
        private float restAlpha;
        private Graphic graphic;

        private void OnEnable()
        {
            GamePreferences.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GamePreferences.Changed -= Refresh;
            Stop();
        }

        private void Refresh()
        {
            bool wanted = Application.isPlaying && !GamePreferences.ReducedGraphics;
            if (wanted == (motion != null)) return;
            if (!wanted) { Stop(); return; }

            var rect = (RectTransform)transform;
            restPosition = rect.anchoredPosition;
            restEuler = rect.localEulerAngles;
            restScale = rect.localScale;
            graphic = GetComponent<Graphic>();
            if (graphic != null) restAlpha = graphic.color.a;

            var sequence = DOTween.Sequence().SetUpdate(true);
            if (moveX != 0f || moveY != 0f)
                sequence.Join(rect.DOAnchorPos(restPosition + new Vector2(moveX, moveY), duration).SetEase(Ease.InOutSine));
            if (rotation != 0f)
                sequence.Join(rect.DOLocalRotate(restEuler + new Vector3(0f, 0f, rotation), duration).SetEase(Ease.InOutSine));
            if (scaleAmount != 0f)
                sequence.Join(rect.DOScale(restScale * (1f + scaleAmount), duration).SetEase(Ease.InOutSine));
            if (alphaAmount != 0f && graphic != null)
                sequence.Join(graphic.DOFade(restAlpha + alphaAmount, duration).SetEase(Ease.InOutSine));
            sequence.SetLoops(-1, LoopType.Yoyo);
            if (phase > 0f) sequence.Goto(phase, true);
            motion = sequence;
        }

        private void Stop()
        {
            if (motion == null) return;
            UIV2Motion.Cancel(ref motion);
            var rect = (RectTransform)transform;
            rect.anchoredPosition = restPosition;
            rect.localEulerAngles = restEuler;
            rect.localScale = restScale;
            if (graphic != null) { var c = graphic.color; c.a = restAlpha; graphic.color = c; }
        }
    }
}
