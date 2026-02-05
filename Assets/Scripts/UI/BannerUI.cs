using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BannerUI : MonoBehaviour
{
    [Header("Banner Components")]
    public Button bannerButton; // The button for the banner
    public Image bannerImage; // The image for the banner background
    public TextMeshProUGUI playerNameText; // The text for the player's name
    public Image playerNameBackground; // The background image for the player's name

    [Header("References")]
    public RectTransform topBar; // Reference to the Top Bar
    public Canvas topBarCanvas; // Canvas of the Top Bar
    public Canvas bannerCanvas; // Canvas of the Banner

    [Header("Banner Settings")]
    [Range(0.2f, 1f)]
    public float bannerWidthPercentage = 0.5f; // Width relative to the banner canvas width

    [Tooltip("Banner size ratio. Height = Width * aspectRatio")]
    public float bannerAspectRatio = 0.35f;

    [Header("Optional Fixed Size")]
    public bool useFixedSize = false;
    public Vector2 fixedSize = new Vector2(650f, 300f);

    [Tooltip("Extra spacing below the TopBar (UI units in the Banner Canvas)")]
    public float topBarBottomPadding = 24f;

    [Tooltip("If true, position the banner using the TopBar height (recommended when both canvases are ScreenSpaceOverlay).")]
    public bool useTopBarHeightFallback = true;

    [Header("Debug")]
    [SerializeField] private bool logDebug = true;

    private RectTransform _rt;
    private RectTransform _bannerCanvasRt;
    private Vector2Int _lastScreen;
    private Vector2 _lastCanvasSize;

    private Project51.Auth.AuthBootstrapper _auth;
    private bool _nameApplied;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (bannerCanvas != null)
            _bannerCanvasRt = bannerCanvas.GetComponent<RectTransform>();

        // Prevent stretching the banner sprite.
        if (bannerImage != null)
            bannerImage.preserveAspect = false;
    }

    private void OnEnable()
    {
        _nameApplied = false;
        TryBindAuth();
    }

    private void OnDisable()
    {
        if (_auth != null)
        {
            _auth.PlayFabAuth.OnDisplayNameChanged -= HandleDisplayNameChanged;
        }
    }

    private void Update()
    {
        // Banner UI can enable before AuthBootstrapper exists (scene init ordering).
        // Retry a few frames until we can bind and apply the name.
        if (_nameApplied) return;
        TryBindAuth();
    }

    private void TryBindAuth()
    {
        if (playerNameText == null) return;

        if (_auth == null)
            _auth = Project51.Auth.AuthBootstrapper.Instance;

        if (_auth == null || _auth.PlayFabAuth == null)
            return;

        _auth.PlayFabAuth.OnDisplayNameChanged -= HandleDisplayNameChanged;
        _auth.PlayFabAuth.OnDisplayNameChanged += HandleDisplayNameChanged;

        SetPlayerName(_auth.PlayFabAuth.GetBestDisplayName());
        _nameApplied = true;
    }

    private void HandleDisplayNameChanged(string _)
    {
        if (_auth == null) return;
        SetPlayerName(_auth.PlayFabAuth.GetBestDisplayName());
    }

    private void LateUpdate()
    {
        var screen = new Vector2Int(Screen.width, Screen.height);
        var canvasSize = _bannerCanvasRt != null ? _bannerCanvasRt.rect.size : Vector2.zero;
        if (screen != _lastScreen || canvasSize != _lastCanvasSize)
        {
            _lastScreen = screen;
            _lastCanvasSize = canvasSize;
            AdjustBannerPositionAndSize();
        }
    }

    public void RefreshLayout()
    {
        _lastScreen = default;
        _lastCanvasSize = default;
        AdjustBannerPositionAndSize();
    }

    private void AdjustBannerPositionAndSize()
    {
        if (_rt == null)
            _rt = GetComponent<RectTransform>();
        if (_bannerCanvasRt == null && bannerCanvas != null)
            _bannerCanvasRt = bannerCanvas.GetComponent<RectTransform>();

        if (_rt != null && topBar != null && topBarCanvas != null && bannerCanvas != null && _bannerCanvasRt != null)
        {
            // Anchor banner to top-left.
            _rt.anchorMin = new Vector2(0f, 1f);
            _rt.anchorMax = new Vector2(0f, 1f);
            _rt.pivot = new Vector2(0f, 1f);

            float yFromTop;
            if (useTopBarHeightFallback && topBarCanvas.renderMode == RenderMode.ScreenSpaceOverlay && bannerCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // In overlay mode, the most stable solution is to position using UI units.
                // TopBarSafeAreaFitter keeps a fixed UI height, so this stays consistent across devices.
                yFromTop = topBar.rect.height;
            }
            else
            {
                // Fallback to screen-point conversion (for mixed canvas modes).
                Vector3[] topBarCorners = new Vector3[4];
                topBar.GetWorldCorners(topBarCorners);
                Vector3 topBarBottomLeft = topBarCorners[0];

                Camera topBarCam = topBarCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : topBarCanvas.worldCamera;
                Camera bannerCam = bannerCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : bannerCanvas.worldCamera;

                Vector2 topBarBottomLeftScreen = RectTransformUtility.WorldToScreenPoint(topBarCam, topBarBottomLeft);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _bannerCanvasRt,
                    topBarBottomLeftScreen,
                    bannerCam,
                    out Vector2 local);

                float canvasHalfHeight = _bannerCanvasRt.rect.height * 0.5f;
                yFromTop = canvasHalfHeight - local.y;
            }

            _rt.anchoredPosition = new Vector2(0f, -(yFromTop + topBarBottomPadding));

            // Sizing in canvas units (NOT Screen.width)
            if (useFixedSize)
            {
                _rt.sizeDelta = fixedSize;
            }
            else
            {
                float bannerWidth = Mathf.Max(0f, _bannerCanvasRt.rect.width * bannerWidthPercentage);
                float bannerHeight = Mathf.Max(0f, bannerWidth * (bannerAspectRatio <= 0f ? 1f : bannerAspectRatio));
                _rt.sizeDelta = new Vector2(bannerWidth, bannerHeight);
            }

            if (logDebug)
            {
                Debug.Log($"TopBar canvas renderMode={topBarCanvas.renderMode} | Banner canvas renderMode={bannerCanvas.renderMode}");
                Debug.Log($"BannerCanvas rect size: {_bannerCanvasRt.rect.size} scaleFactor={bannerCanvas.scaleFactor} | topBarHeight={topBar.rect.height} yFromTop={yFromTop} pad={topBarBottomPadding}");
                Debug.Log($"Banner anchoredPosition={_rt.anchoredPosition} sizeDelta={_rt.sizeDelta}");
            }
        }
        else
        {
            Debug.LogError("Missing references: Ensure Top Bar, Top Bar Canvas, and Banner Canvas are assigned.");
        }
    }

    public void SetBannerImage(Sprite image)
    {
        if (bannerImage != null)
        {
            bannerImage.sprite = image;
        }
    }

    public void SetPlayerName(string playerName)
    {
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }
    }

    public void SetPlayerNameBackground(Sprite background)
    {
        if (playerNameBackground != null)
        {
            playerNameBackground.sprite = background;
        }
    }
}