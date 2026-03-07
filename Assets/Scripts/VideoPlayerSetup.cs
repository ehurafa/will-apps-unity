using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// Attach this script to the Main Camera in the VideoPlayer scene.
/// It creates the Video Player and UI controls programmatically.
/// </summary>
public class VideoPlayerSetup : MonoBehaviour
{
    private readonly Color bgColor = new Color(0.1f, 0.1f, 0.18f, 1f);
    private readonly Color cardColor = new Color(0.086f, 0.129f, 0.243f, 1f);

    private VideoPlayer videoPlayer;
    private TextMeshProUGUI statusText;
    private Button playPauseBtn;
    private TextMeshProUGUI playPauseBtnText;

    private string videoUrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";

    private void Start()
    {
        SetupCamera();
        SetupVideoPlayer();
        CreateUI();
    }

    private void SetupCamera()
    {
        Camera.main.backgroundColor = Color.black;
        Camera.main.orthographic = true;
    }

    private void SetupVideoPlayer()
    {
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.targetCamera = Camera.main;
        videoPlayer.url = videoUrl;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.Play();

        videoPlayer.prepareCompleted += (vp) =>
        {
            if (statusText != null) statusText.text = "Reproduzindo...";
        };

        videoPlayer.errorReceived += (vp, msg) =>
        {
            if (statusText != null) statusText.text = "Erro: " + msg;
        };
    }

    private void CreateUI()
    {
        // --- Canvas ---
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // --- Event System ---
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // --- Top Bar (Back button + Title) ---
        GameObject topBar = new GameObject("TopBar");
        topBar.transform.SetParent(canvasObj.transform, false);
        RectTransform topRect = topBar.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1);
        topRect.sizeDelta = new Vector2(0, 120);
        topRect.anchoredPosition = Vector2.zero;

        Image topBg = topBar.AddComponent<Image>();
        topBg.color = new Color(0, 0, 0, 0.7f);

        HorizontalLayoutGroup hlg = topBar.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 20;
        hlg.padding = new RectOffset(30, 30, 10, 10);
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Back button
        CreateIconButton(topBar.transform, "Btn_Back", "←", 80, 80, () => SceneManager.LoadScene("MainMenu"));

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(topBar.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(600, 80);
        LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
        titleLE.preferredWidth = 600;

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Video Player";
        titleText.fontSize = 40;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;

        // --- Bottom Controls ---
        GameObject bottomBar = new GameObject("BottomBar");
        bottomBar.transform.SetParent(canvasObj.transform, false);
        RectTransform bottomRect = bottomBar.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0, 0);
        bottomRect.anchorMax = new Vector2(1, 0);
        bottomRect.pivot = new Vector2(0.5f, 0);
        bottomRect.sizeDelta = new Vector2(0, 200);
        bottomRect.anchoredPosition = Vector2.zero;

        Image bottomBg = bottomBar.AddComponent<Image>();
        bottomBg.color = new Color(0, 0, 0, 0.7f);

        VerticalLayoutGroup vlg = bottomBar.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 15;
        vlg.padding = new RectOffset(40, 40, 20, 20);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;

        // Status text
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(bottomBar.transform, false);
        RectTransform statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.sizeDelta = new Vector2(900, 40);

        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "Carregando vídeo...";
        statusText.fontSize = 28;
        statusText.color = new Color(1f, 1f, 1f, 0.7f);
        statusText.alignment = TextAlignmentOptions.Center;

        // Play/Pause button
        GameObject ppBtnObj = new GameObject("Btn_PlayPause");
        ppBtnObj.transform.SetParent(bottomBar.transform, false);
        RectTransform ppRect = ppBtnObj.AddComponent<RectTransform>();
        ppRect.sizeDelta = new Vector2(400, 80);

        Image ppBg = ppBtnObj.AddComponent<Image>();
        ppBg.color = new Color(0.306f, 0.804f, 0.769f, 1f); // Secondary color

        playPauseBtn = ppBtnObj.AddComponent<Button>();
        playPauseBtn.onClick.AddListener(TogglePlayPause);

        GameObject ppLabel = new GameObject("Label");
        ppLabel.transform.SetParent(ppBtnObj.transform, false);
        RectTransform ppLabelRect = ppLabel.AddComponent<RectTransform>();
        ppLabelRect.anchorMin = Vector2.zero;
        ppLabelRect.anchorMax = Vector2.one;
        ppLabelRect.offsetMin = Vector2.zero;
        ppLabelRect.offsetMax = Vector2.zero;

        playPauseBtnText = ppLabel.AddComponent<TextMeshProUGUI>();
        playPauseBtnText.text = "⏸  Pausar";
        playPauseBtnText.fontSize = 32;
        playPauseBtnText.fontStyle = FontStyles.Bold;
        playPauseBtnText.color = Color.white;
        playPauseBtnText.alignment = TextAlignmentOptions.Center;
    }

    private void TogglePlayPause()
    {
        if (videoPlayer == null) return;

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            if (playPauseBtnText) playPauseBtnText.text = "▶  Play";
            if (statusText) statusText.text = "Pausado";
        }
        else
        {
            videoPlayer.Play();
            if (playPauseBtnText) playPauseBtnText.text = "⏸  Pausar";
            if (statusText) statusText.text = "Reproduzindo...";
        }
    }

    private void CreateIconButton(Transform parent, string name, string icon, float w, float h, System.Action onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(w, h);

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredWidth = w;
        le.preferredHeight = h;

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.15f);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());

        GameObject labelObj = new GameObject("Icon");
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = labelObj.AddComponent<TextMeshProUGUI>();
        text.text = icon;
        text.fontSize = 40;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
    }
}
