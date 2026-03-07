using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this script to the Main Camera in the MainMenu scene.
/// It creates the entire menu UI programmatically at runtime.
/// </summary>
public class MainMenuSetup : MonoBehaviour
{
    // Colors from the original PWA
    private readonly Color bgColor = new Color(0.1f, 0.1f, 0.18f, 1f);        // #1A1A2E
    private readonly Color cardColor = new Color(0.086f, 0.129f, 0.243f, 1f);  // #16213E
    private readonly Color accentColor = new Color(1f, 0.902f, 0.427f, 1f);    // #FFE66D
    private readonly Color primaryColor = new Color(1f, 0.42f, 0.42f, 1f);     // #FF6B6B
    private readonly Color secondaryColor = new Color(0.306f, 0.804f, 0.769f, 1f); // #4ECDC4
    private readonly Color textSecondaryColor = new Color(0.659f, 0.855f, 0.863f, 1f); // #A8DADC

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
        GameObject bgPanel = CreatePanel(canvasObj.transform, "Background", bgColor);
        RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // --- Content Container ---
        GameObject content = new GameObject("Content");
        content.transform.SetParent(canvasObj.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 40;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(80, 80, 200, 100);

        // --- Title ---
        CreateTitle(content.transform);

        // --- Subtitle ---
        CreateSubtitle(content.transform);

        // --- Spacer ---
        CreateSpacer(content.transform, 60);

        // --- Buttons ---
        CreateMenuButton(content.transform, "\ud83c\udfac  ASSISTIR", "Animes e Filmes",
            primaryColor, new Color(1f, 0.557f, 0.325f), () => SceneManager.LoadScene("VideoPlayer"));

        CreateMenuButton(content.transform, "\ud83c\udfae  JOGAR", "Jogos Divertidos",
            secondaryColor, new Color(0.267f, 0.627f, 0.553f), () => SceneManager.LoadScene("FlappyBird"));

        // --- Spacer ---
        CreateSpacer(content.transform, 40);

        // --- Game Selection Buttons (smaller) ---
        CreateSmallButton(content.transform, "\ud83d\udc26  Flappy Bird", () => SceneManager.LoadScene("FlappyBird"));
        CreateSmallButton(content.transform, "⭕  Jogo da Velha", () => SceneManager.LoadScene("TicTacToe"));

        // --- Footer ---
        CreateFooter(content.transform);
    }

    private void CreateTitle(Transform parent)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);
        RectTransform rect = titleObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900, 120);

        TextMeshProUGUI text = titleObj.AddComponent<TextMeshProUGUI>();
        text.text = "Will Apps";
        text.fontSize = 80;
        text.fontStyle = FontStyles.Bold;
        text.color = accentColor;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
    }

    private void CreateSubtitle(Transform parent)
    {
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(parent, false);
        RectTransform rect = subObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900, 60);

        TextMeshProUGUI text = subObj.AddComponent<TextMeshProUGUI>();
        text.text = "Diversão Garantida! \ud83c\udfae\ud83c\udfac";
        text.fontSize = 36;
        text.color = textSecondaryColor;
        text.alignment = TextAlignmentOptions.Center;
    }

    private void CreateMenuButton(Transform parent, string label, string sublabel,
        Color color1, Color color2, System.Action onClick)
    {
        GameObject btnObj = new GameObject("Btn_" + label.Replace(" ", ""));
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800, 200);

        Image img = btnObj.AddComponent<Image>();
        img.color = color1;

        // Rounded corners effect (using sprite if available, otherwise solid)
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick());

        // Layout for text content
        VerticalLayoutGroup vlg = btnObj.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 8;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(20, 20, 20, 20);

        // Main label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(700, 70);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 48;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.Center;

        // Sub label
        GameObject subObj = new GameObject("SubLabel");
        subObj.transform.SetParent(btnObj.transform, false);
        RectTransform subRect = subObj.AddComponent<RectTransform>();
        subRect.sizeDelta = new Vector2(700, 40);

        TextMeshProUGUI subText = subObj.AddComponent<TextMeshProUGUI>();
        subText.text = sublabel;
        subText.fontSize = 28;
        subText.color = new Color(1f, 1f, 1f, 0.8f);
        subText.alignment = TextAlignmentOptions.Center;
    }

    private void CreateSmallButton(Transform parent, string label, System.Action onClick)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800, 100);

        Image img = btnObj.AddComponent<Image>();
        img.color = cardColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick());

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = labelObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 32;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
    }

    private void CreateFooter(Transform parent)
    {
        GameObject footerObj = new GameObject("Footer");
        footerObj.transform.SetParent(parent, false);
        RectTransform rect = footerObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900, 40);

        TextMeshProUGUI text = footerObj.AddComponent<TextMeshProUGUI>();
        text.text = "v2.0 Unity";
        text.fontSize = 24;
        text.color = new Color(1f, 1f, 1f, 0.3f);
        text.alignment = TextAlignmentOptions.Center;
    }

    private GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        panel.AddComponent<RectTransform>();
        Image img = panel.AddComponent<Image>();
        img.color = color;
        return panel;
    }

    private void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(parent, false);
        RectTransform rect = spacer.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, height);
        LayoutElement le = spacer.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
    }
}
