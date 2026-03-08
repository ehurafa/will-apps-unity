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

    private void Start()
    {
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
        CreateGameCard(content.transform, "\ud83d\udc26", "Flappy Bird", "Voe e desvie dos canos!",
            true, () => SceneManager.LoadScene("FlappyBird"));

        CreateGameCard(content.transform, "⭕", "Jogo da Velha", "Clássico jogo contra a IA!",
            true, () => SceneManager.LoadScene("TicTacToe"));

        CreateGameCard(content.transform, "\ud83e\udd77", "Flappy Ninja", "Voe como um ninja!",
            true, () => SceneManager.LoadScene("FlappyNinja"));

        CreateGameCard(content.transform, "\ud83c\udfb1", "Sinuca", "Em breve...",
            false, null);

        CreateGameCard(content.transform, "\ud83c\udccf", "UNO", "Em breve...",
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

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(header.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(600, 80);
        LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
        titleLE.preferredWidth = 600;

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "\ud83c\udfae  Jogar";
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void CreateGameCard(Transform parent, string icon, string name, string description,
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
        iconRect.sizeDelta = new Vector2(800, 50);

        TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
        iconText.text = icon;
        iconText.fontSize = 42;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.color = available ? new Color(0.1f, 0.1f, 0.1f, 1f) : new Color(1f, 1f, 1f, 0.5f);

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

            TextMeshProUGUI badgeText = CreateChildText(badge.transform, "\ud83d\udd12 Bloqueado", 22,
                new Color(1f, 1f, 1f, 0.5f));
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
}
