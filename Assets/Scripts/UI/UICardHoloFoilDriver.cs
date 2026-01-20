using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Project51.Unity
{
    /// <summary>
    /// Variante UI (Image) dei vecchi driver SpriteRenderer:
    /// - Material instance per effetto holo (tilt su proprietà)
    /// - Drop shadow via Image figlia
    /// - Idle float (DOTween) su RectTransform
    /// 
    /// Mettilo sul GameObject che ha un Image (la carta).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class UICardHoloFoilDriver : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Image cardImage;

        [Header("Material")]
        [Tooltip("If assigned, we instantiate and use this material. Otherwise we clone the current Image material.")]
        [SerializeField] private Material holoMaterial;

        [Header("Tilt")]
        [SerializeField] private float maxZDegreesForTilt = 3f;
        [SerializeField] private float smoothing = 10f;
        [SerializeField] private string tiltProperty = "_Tilt";

        [Header("Shadow (UI)")]
        [SerializeField] private bool enableShadow = true;
        [SerializeField] private Vector2 shadowOffset = new Vector2(10f, -12f);
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private float shadowScale = 1.02f;

        [Header("Floating")]
        [SerializeField] private bool enableIdleFloat = true;
        [SerializeField] private float floatAmplitude = 12f; // UI units
        [SerializeField] private float floatDuration = 2.1f;
        [SerializeField] private float floatRandomOffset = 0.2f;

        [Header("Subtle Z Rotation")]
        [SerializeField] private float zRotationAmplitude = 0.35f;
        [SerializeField] private float zRotationDuration = 0.75f;
        [SerializeField] private float rotationRandomOffset = 0.15f;

        private RectTransform _rt;
        private Material _runtimeMat;
        private float _currentTilt;
        private Sequence _seq;

        private Vector3 _baseLocalPos;
        private Quaternion _baseLocalRot;

        private Image _shadowImg;
        private RectTransform _shadowRT;

        private void Awake()
        {
            if (cardImage == null) cardImage = GetComponent<Image>();
            _rt = transform as RectTransform;
            CacheBasePose();
        }

        private void OnEnable()
        {
            EnsureMaterialInstance();
            EnsureShadow();
            CacheBasePose();

            if (enableIdleFloat)
                PlayIdle();
        }

        private void OnDisable()
        {
            StopIdle();
            RestoreBasePose();

            if (_runtimeMat != null)
            {
                Destroy(_runtimeMat);
                _runtimeMat = null;
            }
        }

        private void LateUpdate()
        {
            if (_runtimeMat == null) return;
            if (_rt == null) return;

            SyncShadowTransform();

            float z = _rt.localEulerAngles.z;
            if (z > 180f) z -= 360f;

            float targetTilt = 0f;
            if (maxZDegreesForTilt > 0.001f)
                targetTilt = Mathf.Clamp(z / maxZDegreesForTilt, -1f, 1f);

            _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));

            if (_runtimeMat.HasProperty(tiltProperty))
                _runtimeMat.SetFloat(tiltProperty, _currentTilt);
        }

        private void EnsureMaterialInstance()
        {
            if (cardImage == null) cardImage = GetComponent<Image>();
            if (cardImage == null) return;

            if (_runtimeMat != null) return;

            var src = holoMaterial != null ? holoMaterial : cardImage.material;
            if (src == null) return;

            _runtimeMat = Instantiate(src);
            cardImage.material = _runtimeMat;
        }

        private void EnsureShadow()
        {
            if (!enableShadow || cardImage == null) return;

            if (_shadowImg == null)
            {
                var go = new GameObject("_Shadow", typeof(RectTransform));
                go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

                _shadowRT = (RectTransform)go.transform;

                // Importante: la shadow deve stare SOTTO la carta in ordine di rendering.
                // Se la shadow è figlia, può finire sopra per via di grafica/mesh/material; come sibling è deterministico.
                var parent = transform.parent as RectTransform;
                if (parent != null)
                    _shadowRT.SetParent(parent, false);
                else
                    _shadowRT.SetParent(transform, false);

                _shadowRT.anchorMin = new Vector2(0.5f, 0.5f);
                _shadowRT.anchorMax = new Vector2(0.5f, 0.5f);
                _shadowRT.pivot = new Vector2(0.5f, 0.5f);

                _shadowImg = go.AddComponent<Image>();
                _shadowImg.raycastTarget = false;
            }

            _shadowImg.sprite = cardImage.sprite;
            _shadowImg.color = shadowColor;
            _shadowImg.preserveAspect = true;

            SyncShadowTransform();

            // Metti la shadow subito dietro la carta nei sibling.
            if (_shadowRT != null)
            {
                int idx = transform.GetSiblingIndex();
                _shadowRT.SetSiblingIndex(Mathf.Max(0, idx - 1));
            }
        }

        private void SyncShadowTransform()
        {
            if (!enableShadow || _shadowImg == null) return;
            if (_shadowRT == null) _shadowRT = _shadowImg.transform as RectTransform;
            if (_shadowRT == null) return;

            // Aggiorna sprite se cambia faccia
            if (cardImage != null && _shadowImg.sprite != cardImage.sprite)
                _shadowImg.sprite = cardImage.sprite;

            if (_rt == null) _rt = transform as RectTransform;
            if (_rt == null) return;

            // Match size/rot/pos della carta, con offset.
            _shadowRT.sizeDelta = _rt.sizeDelta;
            _shadowRT.localScale = _rt.localScale;
            _shadowRT.localRotation = _rt.localRotation;
            _shadowRT.anchoredPosition = _rt.anchoredPosition + shadowOffset;

            // Applica scale extra della shadow
            _shadowRT.localScale = _shadowRT.localScale * shadowScale;

            // Mantieni la shadow immediatamente sotto la carta.
            if (_shadowRT.parent == _rt.parent)
            {
                int idx = _rt.GetSiblingIndex();
                _shadowRT.SetSiblingIndex(Mathf.Max(0, idx - 1));
            }
        }

        private void CacheBasePose()
        {
            if (_rt == null) _rt = transform as RectTransform;
            _baseLocalPos = transform.localPosition;
            _baseLocalRot = transform.localRotation;
        }

        private void RestoreBasePose()
        {
            transform.localPosition = _baseLocalPos;
            transform.localRotation = _baseLocalRot;
        }

        public void PlayIdle()
        {
            StopIdle();
            CacheBasePose();

            float floatDelay = Random.Range(0f, floatRandomOffset);
            float rotDelay = Random.Range(0f, rotationRandomOffset);

            _seq = DOTween.Sequence();
            _seq.SetUpdate(true);
            _seq.SetTarget(this);

            _seq.Join(
                transform.DOLocalMoveY(_baseLocalPos.y + floatAmplitude, floatDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetDelay(floatDelay)
            );

            _seq.Join(
                _rt.DOLocalRotate(
                        new Vector3(0f, 0f, zRotationAmplitude),
                        zRotationDuration,
                        RotateMode.Fast)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetDelay(rotDelay)
            );

            _seq.Play();
        }

        public void StopIdle()
        {
            if (_seq != null)
            {
                _seq.Kill();
                _seq = null;
            }

            DOTween.Kill(this);
        }

        private void OnValidate()
        {
            maxZDegreesForTilt = Mathf.Max(0.1f, maxZDegreesForTilt);
            smoothing = Mathf.Max(0f, smoothing);

            shadowScale = Mathf.Max(0.01f, shadowScale);

            floatDuration = Mathf.Max(0.05f, floatDuration);
            zRotationDuration = Mathf.Max(0.05f, zRotationDuration);
            floatRandomOffset = Mathf.Max(0f, floatRandomOffset);
            rotationRandomOffset = Mathf.Max(0f, rotationRandomOffset);
        }
    }
}
