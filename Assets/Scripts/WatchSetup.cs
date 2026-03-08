using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Attach this script to the Main Camera in the Watch scene.
/// It creates the video listing UI with Animes/Filmes tabs programmatically.
/// </summary>
public class WatchSetup : MonoBehaviour
{
    // Colors
    private readonly Color bgColor = new Color(0.1f, 0.1f, 0.18f, 1f);        // #1A1A2E
    private readonly Color cardColor = new Color(0.086f, 0.129f, 0.243f, 1f);  // #16213E
    private readonly Color primaryColor = new Color(1f, 0.42f, 0.42f, 1f);     // #FF6B6B
    private readonly Color secondaryColor = new Color(0.306f, 0.804f, 0.769f, 1f); // #4ECDC4
    private readonly Color accentColor = new Color(1f, 0.902f, 0.427f, 1f);    // #FFE66D
    private readonly Color textSecondaryColor = new Color(0.659f, 0.855f, 0.863f, 1f); // #A8DADC

    // Static field to pass video URL between scenes
    public static string SelectedVideoUrl = "";
    public static string SelectedVideoTitle = "";

    // Video data model
    private struct VideoData
    {
        public string id;
        public string title;
        public string thumbnailUrl;
        public string videoUrl;
        public string category; // "animes" or "filmes"
        public string duration;
    }

    // Mock video data (matching the PWA)
    private List<VideoData> allVideos = new List<VideoData>
    {
        new VideoData
        {
            id = "demon-slayer-mugen-train",
            title = "Demon Slayer: Mugen Train",
            thumbnailUrl = "https://www.agenciacardeal.com.br/will-apps/filmes/kimetsu/demon_slayer_thumb.png",
            videoUrl = "https://www.agenciacardeal.com.br/will-apps/filmes/kimetsu/kimetsu.mp4",
            category = "filmes",
            duration = "1:56:00"
        },
        new VideoData
        {
            id = "big-buck-bunny",
            title = "Big Buck Bunny",
            thumbnailUrl = "",
            videoUrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
            category = "filmes",
            duration = "9:56"
        },
        new VideoData
        {
            id = "naruto-ep1",
            title = "Naruto - Episódio 1",
            thumbnailUrl = "",
            videoUrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4",
            category = "animes",
            duration = "23:00"
        },
        new VideoData
        {
            id = "one-piece-ep1",
            title = "One Piece - Episódio 1",
            thumbnailUrl = "",
            videoUrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4",
            category = "animes",
            duration = "24:00"
        }
    };

    // UI state
    private string activeTab = "animes";
    private Transform contentContainer;
    private Image animesTabBg;
    private Image filmesTabBg;
    private TextMeshProUGUI animesTabText;
    private TextMeshProUGUI filmesTabText;

    private void Start()
    {
        SetupCamera();
        CreateUI();
        RefreshVideoList();
    }

    private void SetupCamera()
    {
        Camera.main.backgroundColor = bgColor;
        Camera.main.orthographic = true;
    }

    private void CreateUI()
    {
        // --- Canvas ---
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

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

        // --- Background Panel ---
        GameObject bgPanel = new GameObject("Background");
        bgPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgPanel.AddComponent<Image>();
        bgImg.color = bgColor;

        // --- Header ---
        CreateHeader(canvasObj.transform);

        // --- Tabs ---
        CreateTabs(canvasObj.transform);

        // --- Scroll Area for Videos ---
        GameObject scrollArea = new GameObject("ScrollArea");
        scrollArea.transform.SetParent(canvasObj.transform, false);
        RectTransform scrollRect = scrollArea.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(0, 0);
        scrollRect.offsetMax = new Vector2(0, -220); // Header (130) + Tabs (90)

        ScrollRect scroll = scrollArea.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // RectMask2D clips children to scroll area bounds (no stencil/alpha issues)
        scrollArea.AddComponent<RectMask2D>();

        // Content container
        GameObject content = new GameObject("Content");
        content.transform.SetParent(scrollArea.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 25;
        vlg.padding = new RectOffset(40, 40, 30, 60);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;
        contentContainer = content.transform;
    }

    private void CreateHeader(Transform parent)
    {
        GameObject header = new GameObject("Header");
        header.transform.SetParent(parent, false);
        RectTransform rect = header.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = new Vector2(0, 130);
        rect.anchoredPosition = Vector2.zero;

        Image headerBg = header.AddComponent<Image>();
        headerBg.color = cardColor;

        HorizontalLayoutGroup hlg = header.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 20;
        hlg.padding = new RectOffset(30, 30, 10, 10);
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Back button
        CreateIconButton(header.transform, "Btn_Back", "←", 80, 80,
            () => SceneManager.LoadScene("MainMenu"));

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(header.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(600, 80);
        LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
        titleLE.preferredWidth = 600;

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "\ud83c\udfac  Assistir";
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void CreateTabs(Transform parent)
    {
        GameObject tabsContainer = new GameObject("Tabs");
        tabsContainer.transform.SetParent(parent, false);
        RectTransform tabsRect = tabsContainer.AddComponent<RectTransform>();
        tabsRect.anchorMin = new Vector2(0, 1);
        tabsRect.anchorMax = new Vector2(1, 1);
        tabsRect.pivot = new Vector2(0.5f, 1);
        tabsRect.sizeDelta = new Vector2(0, 90);
        tabsRect.anchoredPosition = new Vector2(0, -130); // Below header

        Image tabsBg = tabsContainer.AddComponent<Image>();
        tabsBg.color = new Color(0.075f, 0.1f, 0.2f, 1f);

        HorizontalLayoutGroup hlg = tabsContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 20;
        hlg.padding = new RectOffset(40, 40, 10, 10);
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;

        // Animes tab
        GameObject animesTab = CreateTabButton(tabsContainer.transform, "Btn_Animes", "Animes",
            () => SwitchTab("animes"));
        animesTabBg = animesTab.GetComponent<Image>();
        animesTabText = animesTab.GetComponentInChildren<TextMeshProUGUI>();

        // Filmes tab
        GameObject filmesTab = CreateTabButton(tabsContainer.transform, "Btn_Filmes", "Filmes",
            () => SwitchTab("filmes"));
        filmesTabBg = filmesTab.GetComponent<Image>();
        filmesTabText = filmesTab.GetComponentInChildren<TextMeshProUGUI>();

        UpdateTabVisuals();
    }

    private GameObject CreateTabButton(Transform parent, string name, string label, System.Action onClick)
    {
        GameObject tabObj = new GameObject(name);
        tabObj.transform.SetParent(parent, false);
        RectTransform rect = tabObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 60);

        Image bg = tabObj.AddComponent<Image>();
        bg.color = cardColor;

        Button btn = tabObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick());

        TextMeshProUGUI text = CreateChildText(tabObj.transform, label, 30, Color.white);
        text.fontStyle = FontStyles.Bold;

        return tabObj;
    }

    private void SwitchTab(string tab)
    {
        activeTab = tab;
        UpdateTabVisuals();
        RefreshVideoList();
    }

    private void UpdateTabVisuals()
    {
        if (animesTabBg != null)
            animesTabBg.color = activeTab == "animes" ? primaryColor : cardColor;
        if (filmesTabBg != null)
            filmesTabBg.color = activeTab == "filmes" ? primaryColor : cardColor;
    }

    private void RefreshVideoList()
    {
        if (contentContainer == null) return;

        // Clear existing cards
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(contentContainer.GetChild(i).gameObject);
        }

        // Filter and create cards
        foreach (var video in allVideos)
        {
            if (video.category == activeTab)
            {
                CreateVideoCard(contentContainer, video);
            }
        }

        // Show empty message if no videos
        bool hasVideos = false;
        foreach (var video in allVideos)
        {
            if (video.category == activeTab) { hasVideos = true; break; }
        }

        if (!hasVideos)
        {
            CreateText(contentContainer, "EmptyMsg", "Nenhum vídeo disponível", 32, textSecondaryColor);
        }
    }

    private void CreateVideoCard(Transform parent, VideoData video)
    {
        GameObject card = new GameObject("VideoCard_" + video.id);
        card.transform.SetParent(parent, false);
        RectTransform rect = card.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(950, 180);

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.preferredHeight = 180;

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = cardColor;

        Button btn = card.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = colors;

        string capturedUrl = video.videoUrl;
        string capturedTitle = video.title;
        btn.onClick.AddListener(() =>
        {
            SelectedVideoUrl = capturedUrl;
            SelectedVideoTitle = capturedTitle;
            SceneManager.LoadScene("VideoPlayer");
        });

        HorizontalLayoutGroup hlg = card.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 20;
        hlg.padding = new RectOffset(15, 15, 15, 15);
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;

        // Thumbnail placeholder
        GameObject thumbObj = new GameObject("Thumbnail");
        thumbObj.transform.SetParent(card.transform, false);
        RectTransform thumbRect = thumbObj.AddComponent<RectTransform>();
        thumbRect.sizeDelta = new Vector2(200, 150);
        LayoutElement thumbLE = thumbObj.AddComponent<LayoutElement>();
        thumbLE.preferredWidth = 200;
        thumbLE.preferredHeight = 150;

        Image thumbImg = thumbObj.AddComponent<Image>();
        thumbImg.color = new Color(0.15f, 0.15f, 0.25f, 1f);

        // Play icon on thumbnail
        TextMeshProUGUI playIcon = CreateChildText(thumbObj.transform, "▶", 48, new Color(1f, 1f, 1f, 0.6f));

        // Try to load thumbnail from URL
        if (!string.IsNullOrEmpty(video.thumbnailUrl))
        {
            StartCoroutine(LoadThumbnail(video.thumbnailUrl, thumbImg));
        }

        // Info container
        GameObject infoObj = new GameObject("Info");
        infoObj.transform.SetParent(card.transform, false);
        RectTransform infoRect = infoObj.AddComponent<RectTransform>();
        infoRect.sizeDelta = new Vector2(600, 150);
        LayoutElement infoLE = infoObj.AddComponent<LayoutElement>();
        infoLE.preferredWidth = 600;
        infoLE.flexibleWidth = 1;

        VerticalLayoutGroup infoVlg = infoObj.AddComponent<VerticalLayoutGroup>();
        infoVlg.childAlignment = TextAnchor.MiddleLeft;
        infoVlg.spacing = 8;
        infoVlg.childControlWidth = true;
        infoVlg.childControlHeight = false;

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(infoObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(500, 50);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = video.title;
        titleText.fontSize = 30;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.enableWordWrapping = true;
        titleText.overflowMode = TextOverflowModes.Ellipsis;

        // Duration
        if (!string.IsNullOrEmpty(video.duration))
        {
            GameObject durObj = new GameObject("Duration");
            durObj.transform.SetParent(infoObj.transform, false);
            RectTransform durRect = durObj.AddComponent<RectTransform>();
            durRect.sizeDelta = new Vector2(500, 30);

            TextMeshProUGUI durText = durObj.AddComponent<TextMeshProUGUI>();
            durText.text = "⏱ " + video.duration;
            durText.fontSize = 24;
            durText.color = textSecondaryColor;
            durText.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }

    private IEnumerator LoadThumbnail(string url, Image targetImage)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(request);
                if (tex != null && targetImage != null)
                {
                    Sprite sprite = Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f));
                    targetImage.sprite = sprite;
                    targetImage.color = Color.white;
                    targetImage.preserveAspect = true;
                }
            }
        }
    }

    // --- Helpers ---

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

        TextMeshProUGUI text = CreateChildText(btnObj.transform, icon, 40, Color.white);
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string content, int fontSize, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900, fontSize + 20);

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private TextMeshProUGUI CreateChildText(Transform parent, string content, int fontSize, Color color)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }
}
