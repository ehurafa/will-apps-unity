using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this script to the Main Camera in the Play scene.
/// It creates the game selection hub UI programmatically at runtime.
/// </summary>
public class PlaySetup : MonoBehaviour
{
    // Colors from the original PWA
    private readonly Color bgColor = new Color(0.1f, 0.1f, 0.18f, 1f);        // #1A1A2E
    private readonly Color cardColor = new Color(0.086f, 0.129f, 0.243f, 1f);  // #16213E
    private readonly Color accentColor = new Color(1f, 0.902f, 0.427f, 1f);    // #FFE66D
    private readonly Color accentColor2 = new Color(1f, 0.671f, 0.298f, 1f);   // #FFAB4C
    private readonly Color textSecondaryColor = new Color(0.659f, 0.855f, 0.863f, 1f); // #A8DADC
    private readonly Color lockedBgColor = new Color(0.267f, 0.267f, 0.267f, 1f); // #444
    private readonly Color lockedBgColor2 = new Color(0.2f, 0.2f, 0.2f, 1f);     // #333

    private Sprite gamepadSpriteCache;
    private Sprite lockSpriteCache;
    private Sprite birdSpriteCache;
    private Sprite tictactoeSpriteCache;
    private Sprite ninjaSpriteCache;
    private Sprite guitarSpriteCache;
    private Sprite unoSpriteCache;

    private void Start()
    {
        gamepadSpriteCache = CreateGamepadSprite(128, 128);
        lockSpriteCache = CreateLockSprite(128, 128);
        birdSpriteCache = CreateBirdSprite(128, 128);
        tictactoeSpriteCache = CreateTicTacToeSprite(128, 128);
        ninjaSpriteCache = CreateNinjaSprite(128, 128);
        guitarSpriteCache = CreateGuitarSprite(128, 128);
        unoSpriteCache = CreateUnoCardsSprite(128, 128);

        SetupCamera();
        CreateUI();
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

        // --- Scroll Area for Games ---
        GameObject scrollArea = new GameObject("ScrollArea");
        scrollArea.transform.SetParent(canvasObj.transform, false);
        RectTransform scrollRect = scrollArea.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(0, 0);
        scrollRect.offsetMax = new Vector2(0, -130); // Leave space for header

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
        contentRect.sizeDelta = new Vector2(0, 0); // Will be auto-sized

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 30;
        vlg.padding = new RectOffset(60, 60, 40, 60);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;

        // --- Game Cards ---
        CreateGameCard(content.transform, birdSpriteCache, "Flappy Bird", "Voe e desvie dos canos!",
            true, () => SceneManager.LoadScene("FlappyBird"));

        CreateGameCard(content.transform, tictactoeSpriteCache, "Jogo da Velha", "Clássico jogo contra a IA!",
            true, () => SceneManager.LoadScene("TicTacToe"));

        CreateGameCard(content.transform, ninjaSpriteCache, "Flappy Ninja", "Voe como um ninja!",
            true, () => SceneManager.LoadScene("FlappyNinja"));

        CreateGameCard(content.transform, guitarSpriteCache, "Guitar Flash", "Jogo de ritmo!",
            true, () => SceneManager.LoadScene("GuitarFlash"));

        CreateGameCard(content.transform, unoSpriteCache, "UNO", "Em breve...",
            false, null);
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

        // Gamepad Icon
        GameObject gamepadIconObj = new GameObject("Icon_Gamepad");
        gamepadIconObj.transform.SetParent(header.transform, false);
        RectTransform gamepadRect = gamepadIconObj.AddComponent<RectTransform>();
        gamepadRect.sizeDelta = new Vector2(60, 60);
        LayoutElement gamepadLE = gamepadIconObj.AddComponent<LayoutElement>();
        gamepadLE.preferredWidth = 60;
        gamepadLE.preferredHeight = 60;
        Image gamepadImg = gamepadIconObj.AddComponent<Image>();
        gamepadImg.sprite = gamepadSpriteCache;
        gamepadImg.color = accentColor2;
        gamepadImg.preserveAspect = true;

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(header.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(600, 80);
        LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
        titleLE.preferredWidth = 600;

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Jogar";
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void CreateGameCard(Transform parent, Sprite iconSprite, string name, string description,
        bool available, System.Action onClick)
    {
        GameObject card = new GameObject("Card_" + name.Replace(" ", ""));
        card.transform.SetParent(parent, false);
        RectTransform rect = card.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900, 220);

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.preferredHeight = 220;

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = available ? accentColor : lockedBgColor;

        if (available)
        {
            Button btn = card.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        // Card layout
        VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 8;
        vlg.padding = new RectOffset(30, 30, 15, 15);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(card.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(60, 60);
        LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
        iconLE.preferredWidth = 60;
        iconLE.preferredHeight = 60;

        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = iconSprite;
        iconImg.preserveAspect = true;
        iconImg.color = available ? new Color(0.1f, 0.1f, 0.1f, 1f) : new Color(1f, 1f, 1f, 0.5f);

        // Name
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(card.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.sizeDelta = new Vector2(800, 45);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = name;
        nameText.fontSize = 38;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = available ? new Color(0.1f, 0.1f, 0.1f, 1f) : new Color(1f, 1f, 1f, 0.7f);

        // Description
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(card.transform, false);
        RectTransform descRect = descObj.AddComponent<RectTransform>();
        descRect.sizeDelta = new Vector2(800, 35);

        TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = description;
        descText.fontSize = 26;
        descText.alignment = TextAlignmentOptions.Center;
        descText.color = available ? new Color(0.1f, 0.1f, 0.1f, 0.7f) : new Color(1f, 1f, 1f, 0.4f);

        // Badge (JOGAR or Bloqueado)
        if (available)
        {
            GameObject badge = new GameObject("PlayBadge");
            badge.transform.SetParent(card.transform, false);
            RectTransform badgeRect = badge.AddComponent<RectTransform>();
            badgeRect.sizeDelta = new Vector2(200, 40);

            Image badgeBg = badge.AddComponent<Image>();
            badgeBg.color = new Color(0.1f, 0.1f, 0.18f, 0.3f);

            TextMeshProUGUI badgeText = CreateChildText(badge.transform, "JOGAR", 22, Color.white);
            badgeText.fontStyle = FontStyles.Bold;
        }
        else
        {
            GameObject badge = new GameObject("LockedBadge");
            badge.transform.SetParent(card.transform, false);
            RectTransform badgeRect = badge.AddComponent<RectTransform>();
            badgeRect.sizeDelta = new Vector2(250, 40);

            Image badgeBg = badge.AddComponent<Image>();
            badgeBg.color = new Color(0f, 0f, 0f, 0.3f);

            HorizontalLayoutGroup badgeHlg = badge.AddComponent<HorizontalLayoutGroup>();
            badgeHlg.childAlignment = TextAnchor.MiddleCenter;
            badgeHlg.spacing = 8;
            badgeHlg.childControlWidth = false;
            badgeHlg.childControlHeight = false;

            // Lock Icon
            GameObject lockIconObj = new GameObject("Icon_Lock");
            lockIconObj.transform.SetParent(badge.transform, false);
            RectTransform lockIconRect = lockIconObj.AddComponent<RectTransform>();
            lockIconRect.sizeDelta = new Vector2(20, 20);
            Image lockIconImg = lockIconObj.AddComponent<Image>();
            lockIconImg.sprite = lockSpriteCache;
            lockIconImg.color = new Color(1f, 1f, 1f, 0.5f);
            lockIconImg.preserveAspect = true;

            // Text
            GameObject badgeTextObj = new GameObject("Text");
            badgeTextObj.transform.SetParent(badge.transform, false);
            RectTransform badgeTextRect = badgeTextObj.AddComponent<RectTransform>();
            badgeTextRect.sizeDelta = new Vector2(120, 30);
            TextMeshProUGUI badgeText = badgeTextObj.AddComponent<TextMeshProUGUI>();
            badgeText.text = "Bloqueado";
            badgeText.fontSize = 22;
            badgeText.color = new Color(1f, 1f, 1f, 0.5f);
            badgeText.alignment = TextAlignmentOptions.Center;
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

        TextMeshProUGUI text = CreateChildText(btnObj.transform, icon, 40, Color.white);
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

    private Sprite CreateGamepadSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;
        float cx = width / 2f;
        float cy = height / 2f;
        float rx = width * 0.4f;
        float ry = height * 0.25f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float d1 = Mathf.Sqrt((dx - (rx - ry)) * (dx - (rx - ry)) + dy * dy);
                float d2 = Mathf.Sqrt((dx + (rx - ry)) * (dx + (rx - ry)) + dy * dy);
                bool inBody = (d1 <= ry || d2 <= ry || (Mathf.Abs(dx) <= rx - ry && Mathf.Abs(dy) <= ry));
                bool inDetails = false;
                if (inBody)
                {
                    float dpadCx = -rx * 0.5f;
                    float dpadCy = 0f;
                    float dpadW = width * 0.06f;
                    float dpadH = height * 0.18f;
                    bool inDpadH = (Mathf.Abs(dx - dpadCx) <= dpadW && Mathf.Abs(dy - dpadCy) <= dpadH / 2f);
                    bool inDpadV = (Mathf.Abs(dx - dpadCx) <= dpadH / 2f && Mathf.Abs(dy - dpadCy) <= dpadW);
                    float btnCx = rx * 0.5f;
                    float btnCy = 0f;
                    float btnDist = Mathf.Sqrt((dx - btnCx) * (dx - btnCx) + dy * dy);
                    bool inButtons = (btnDist <= width * 0.08f);
                    inDetails = inDpadH || inDpadV || inButtons;
                }
                if (inBody && !inDetails)
                    pixels[y * width + x] = white;
                else
                    pixels[y * width + x] = transparent;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateLockSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color transparent = new Color(0, 0, 0, 0);
        Color gold = new Color(0.9f, 0.75f, 0.2f, 1f);
        Color shackleColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        float cx = width / 2f;
        float cy = height * 0.45f;
        float bodyW = width * 0.42f;
        float bodyH = height * 0.32f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                bool isShackle = false;
                float shackleRadiusOuter = width * 0.16f;
                float shackleRadiusInner = width * 0.08f;
                float shackleCy = cy + bodyH * 0.4f;
                float sDx = x - cx;
                float sDy = y - shackleCy;
                if (sDy >= 0)
                {
                    float dist = Mathf.Sqrt(sDx * sDx + sDy * sDy);
                    if (dist >= shackleRadiusInner && dist <= shackleRadiusOuter)
                        isShackle = true;
                }
                else if (y >= cy && y <= shackleCy)
                {
                    if (Mathf.Abs(sDx) >= shackleRadiusInner && Mathf.Abs(sDx) <= shackleRadiusOuter)
                        isShackle = true;
                }
                bool isBody = (Mathf.Abs(dx) <= bodyW / 2f && Mathf.Abs(dy) <= bodyH / 2f);
                bool isKeyhole = false;
                if (isBody)
                {
                    float khDist = Mathf.Sqrt(dx * dx + (dy + height * 0.02f) * (dy + height * 0.02f));
                    if (khDist <= width * 0.04f)
                        isKeyhole = true;
                    else if (Mathf.Abs(dx) <= width * 0.02f && dy <= -height * 0.02f && dy >= -height * 0.1f)
                        isKeyhole = true;
                }
                if (isKeyhole)
                    pixels[y * width + x] = Color.black;
                else if (isBody)
                    pixels[y * width + x] = gold;
                else if (isShackle)
                    pixels[y * width + x] = shackleColor;
                else
                    pixels[y * width + x] = transparent;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateBirdSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color transparent = new Color(0, 0, 0, 0);
        Color bodyColor = new Color(1f, 0.9f, 0.43f, 1f);
        Color beakColor = new Color(1f, 0.67f, 0.3f, 1f);
        Color white = Color.white;
        float cx = width * 0.45f;
        float cy = height * 0.5f;
        float r = width * 0.28f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                bool isBeak = false;
                if (x >= cx + r * 0.7f && x <= cx + r * 1.4f)
                {
                    float beakHeight = (cx + r * 1.4f - x) * 0.4f;
                    if (Mathf.Abs(dy) <= beakHeight)
                        isBeak = true;
                }
                bool isBody = (dist <= r);
                bool isEye = false;
                float eyeCx = cx + r * 0.4f;
                float eyeCy = cy + r * 0.4f;
                float eyeDist = Mathf.Sqrt((x - eyeCx) * (x - eyeCx) + (y - eyeCy) * (y - eyeCy));
                if (eyeDist <= r * 0.25f)
                    isEye = true;
                bool isPupil = false;
                if (eyeDist <= r * 0.12f)
                    isPupil = true;
                bool isWing = false;
                float wingCx = cx - r * 0.4f;
                float wingCy = cy - r * 0.2f;
                float wingW = r * 0.5f;
                float wingH = r * 0.3f;
                float wingDist = ((x - wingCx) * (x - wingCx)) / (wingW * wingW) + ((y - wingCy) * (y - wingCy)) / (wingH * wingH);
                if (wingDist <= 1.0f)
                    isWing = true;

                if (isPupil)
                    pixels[y * width + x] = Color.black;
                else if (isEye)
                    pixels[y * width + x] = white;
                else if (isWing)
                    pixels[y * width + x] = new Color(0.95f, 0.8f, 0.3f, 1f);
                else if (isBody)
                    pixels[y * width + x] = bodyColor;
                else if (isBeak)
                    pixels[y * width + x] = beakColor;
                else
                    pixels[y * width + x] = transparent;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateTicTacToeSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color transparent = new Color(0, 0, 0, 0);
        Color colorX = new Color(1f, 0.42f, 0.42f, 1f);
        Color colorO = new Color(0.306f, 0.804f, 0.769f, 1f);
        float cx = width / 2f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float xCx = width * 0.3f;
                float xCy = height * 0.5f;
                float xSize = width * 0.15f;
                bool isX = false;
                if (Mathf.Abs(x - xCx) <= xSize && Mathf.Abs(y - xCy) <= xSize)
                {
                    float dist1 = Mathf.Abs((x - xCx) - (y - xCy)) / Mathf.Sqrt(2f);
                    float dist2 = Mathf.Abs((x - xCx) + (y - xCy)) / Mathf.Sqrt(2f);
                    if ((dist1 <= width * 0.03f || dist2 <= width * 0.03f) && 
                        Mathf.Sqrt((x-xCx)*(x-xCx) + (y-xCy)*(y-xCy)) <= xSize)
                        isX = true;
                }
                float oCx = width * 0.7f;
                float oCy = height * 0.5f;
                float oRadiusOuter = width * 0.15f;
                float oRadiusInner = width * 0.09f;
                float oDist = Mathf.Sqrt((x - oCx) * (x - oCx) + (y - oCy) * (y - oCy));
                bool isO = (oDist >= oRadiusInner && oDist <= oRadiusOuter);
                bool isSeparator = (x >= cx - 2 && x <= cx + 2 && y >= height * 0.2f && y <= height * 0.8f);
                if (isX)
                    pixels[y * width + x] = colorX;
                else if (isO)
                    pixels[y * width + x] = colorO;
                else if (isSeparator)
                    pixels[y * width + x] = new Color(1f, 1f, 1f, 0.2f);
                else
                    pixels[y * width + x] = transparent;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateNinjaSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color transparent = new Color(0, 0, 0, 0);
        Color maskColor = new Color(0.1f, 0.1f, 0.15f, 1f);
        Color skinColor = new Color(1f, 0.8f, 0.6f, 1f);
        Color redBand = new Color(1f, 0.3f, 0.3f, 1f);
        float cx = width / 2f;
        float r = width * 0.32f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - cx;
                float dy = y - (height / 2f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                bool isHead = (dist <= r);
                bool isSkin = false;
                if (isHead)
                {
                    float slitW = r * 0.7f;
                    float slitH = r * 0.22f;
                    float slitY = height / 2f + r * 0.1f;
                    float sDistY = Mathf.Abs(y - slitY);
                    float sDistX = Mathf.Abs(dx);
                    if (sDistX <= slitW - slitH)
                    {
                        if (sDistY <= slitH) isSkin = true;
                    }
                    else if (sDistX <= slitW)
                    {
                        float edgeDist = Mathf.Sqrt((sDistX - (slitW - slitH)) * (sDistX - (slitW - slitH)) + (y - slitY) * (y - slitY));
                        if (edgeDist <= slitH) isSkin = true;
                    }
                }
                bool isEye = false;
                if (isSkin)
                {
                    float eyeOffset = r * 0.3f;
                    float eyeY = height / 2f + r * 0.1f;
                    float dLeftEye = Mathf.Sqrt((x - (cx - eyeOffset)) * (x - (cx - eyeOffset)) + (y - eyeY) * (y - eyeY));
                    float dRightEye = Mathf.Sqrt((x - (cx + eyeOffset)) * (x - (cx + eyeOffset)) + (y - eyeY) * (y - eyeY));
                    if (dLeftEye <= width * 0.03f || dRightEye <= width * 0.03f)
                        isEye = true;
                }
                bool isBand = false;
                if (isHead)
                {
                    float bandYMin = height / 2f + r * 0.35f;
                    float bandYMax = height / 2f + r * 0.65f;
                    if (y >= bandYMin && y <= bandYMax)
                        isBand = true;
                }
                if (isEye)
                    pixels[y * width + x] = Color.black;
                else if (isSkin)
                    pixels[y * width + x] = skinColor;
                else if (isBand)
                    pixels[y * width + x] = redBand;
                else if (isHead)
                    pixels[y * width + x] = maskColor;
                else
                    pixels[y * width + x] = transparent;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateGuitarSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color transparent = new Color(0, 0, 0, 0);
        Color guitarRed = new Color(1f, 0.3f, 0.4f, 1f);
        Color neckWood = new Color(0.6f, 0.4f, 0.2f, 1f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float rx = x - width * 0.35f;
                float ry = y - height * 0.35f;
                float distToNeckLine = Mathf.Abs(rx - ry) / Mathf.Sqrt(2f);
                float projection = (rx + ry) / 2f;
                bool isNeck = (distToNeckLine <= width * 0.025f && projection >= -width * 0.1f && projection <= width * 0.35f);
                float bodyCx1 = width * 0.35f - width * 0.12f;
                float bodyCy1 = height * 0.35f - height * 0.12f;
                float distBody1 = Mathf.Sqrt((x - bodyCx1) * (x - bodyCx1) + (y - bodyCy1) * (y - bodyCy1));
                float bodyCx2 = width * 0.35f - width * 0.22f;
                float bodyCy2 = height * 0.35f - height * 0.22f;
                float distBody2 = Mathf.Sqrt((x - bodyCx2) * (x - bodyCx2) + (y - bodyCy2) * (y - bodyCy2));
                bool isBody = (distBody1 <= width * 0.16f || distBody2 <= width * 0.12f);
                bool isHole = false;
                if (isBody)
                {
                    float holeDist = Mathf.Sqrt((x - bodyCx1) * (x - bodyCx1) + (y - bodyCy1) * (y - bodyCy1));
                    if (holeDist <= width * 0.05f)
                        isHole = true;
                }
                if (isHole)
                    pixels[y * width + x] = Color.black;
                else if (isBody)
                    pixels[y * width + x] = guitarRed;
                else if (isNeck)
                    pixels[y * width + x] = neckWood;
                else
                    pixels[y * width + x] = transparent;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateUnoCardsSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color transparent = new Color(0, 0, 0, 0);
        Color cardRed = new Color(1f, 0.3f, 0.3f, 1f);
        Color cardBlue = new Color(0.2f, 0.5f, 1f, 1f);
        Color white = Color.white;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float cx1 = width * 0.42f;
                float cy1 = height * 0.5f;
                float rx1 = (x - cx1) * Mathf.Cos(0.15f) - (y - cy1) * Mathf.Sin(0.15f);
                float ry1 = (x - cx1) * Mathf.Sin(0.15f) + (y - cy1) * Mathf.Cos(0.15f);
                bool inCard1 = (Mathf.Abs(rx1) <= width * 0.18f && Mathf.Abs(ry1) <= height * 0.26f);
                float cx2 = width * 0.58f;
                float cy2 = height * 0.5f;
                float rx2 = (x - cx2) * Mathf.Cos(-0.15f) - (y - cy2) * Mathf.Sin(-0.15f);
                float ry2 = (x - cx2) * Mathf.Sin(-0.15f) + (y - cy2) * Mathf.Cos(-0.15f);
                bool inCard2 = (Mathf.Abs(rx2) <= width * 0.18f && Mathf.Abs(ry2) <= height * 0.26f);
                if (inCard2)
                {
                    if (Mathf.Abs(rx2) >= width * 0.16f || Mathf.Abs(ry2) >= height * 0.24f)
                        pixels[y * width + x] = white;
                    else
                        pixels[y * width + x] = cardRed;
                }
                else if (inCard1)
                {
                    if (Mathf.Abs(rx1) >= width * 0.16f || Mathf.Abs(ry1) >= height * 0.24f)
                        pixels[y * width + x] = white;
                    else
                        pixels[y * width + x] = cardBlue;
                }
                else
                {
                    pixels[y * width + x] = transparent;
                }
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
}
