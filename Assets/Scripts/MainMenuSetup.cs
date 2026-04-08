using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Attach this script to the Main Camera.
/// Meticulous exact-match of the React PWA Home Screen.
/// </summary>
public class MainMenuSetup : MonoBehaviour
{
    private readonly Color titleGlowColor = new Color(1f, 0.42f, 0.42f, 1f);   
    private readonly Color primaryTop = new Color(1f, 0.42f, 0.42f, 1f);       
    private readonly Color primaryBottom = new Color(1f, 0.557f, 0.325f, 1f);  
    private readonly Color secondaryTop = new Color(0.306f, 0.804f, 0.769f, 1f); 
    private readonly Color secondaryBottom = new Color(0.267f, 0.627f, 0.553f, 1f); 
    private readonly Color textSecondaryColor = new Color(0.659f, 0.855f, 0.863f, 1f); 
    private readonly Color overlayColor = new Color(0.102f, 0.102f, 0.18f, 0.85f); 

    private Sprite roundedSpriteCache;
    private Sprite playSpriteCache;
    private Sprite starSpriteCache;

    private void Start()
    {
        // Pre-generate assets
        roundedSpriteCache = CreateRoundedSprite(128, 128, 32); 
        playSpriteCache = CreatePlaySprite(128, 128);
        starSpriteCache = CreateStarSprite(128, 128);
        
        SetupCamera();
        CreateUI();
    }

    private void SetupCamera()
    {
        Camera.main.backgroundColor = new Color(0.1f, 0.1f, 0.18f, 1f);
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

        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // --- Background (.home-container) ---
        GameObject bgPanel = CreatePanel(canvasObj.transform, "Background", Color.white);
        RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        Sprite bgSprite = Resources.Load<Sprite>("Images/background");
        Image bgImg = bgPanel.GetComponent<Image>();
        if (bgSprite != null)
        {
            bgImg.sprite = bgSprite;
            // Native PWA uses background-size: cover.
            // We use AspectRatioFitter to ensure it covers perfectly without distortion.
            AspectRatioFitter arf = bgPanel.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = (float)bgSprite.texture.width / bgSprite.texture.height;
        }
        else
        {
            bgImg.color = new Color(0.1f, 0.1f, 0.15f, 1f); 
        }

        // --- Dark Overlay (.home-overlay) ---
        GameObject overlayPanel = CreatePanel(canvasObj.transform, "Overlay", overlayColor);
        RectTransform overlayRect = overlayPanel.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        // --- Content (.home-content) ---
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(canvasObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 0;
        vlg.childControlWidth = false; // Fix stretch: let children be their true width
        vlg.childControlHeight = false; 
        vlg.childForceExpandWidth = false; 
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(60, 60, 150, 60);

        // --- Header ---
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(contentObj.transform, false);
        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.sizeDelta = new Vector2(900, 250);

        VerticalLayoutGroup headerVlg = headerObj.AddComponent<VerticalLayoutGroup>();
        headerVlg.childAlignment = TextAnchor.MiddleCenter;
        headerVlg.spacing = 15;
        headerVlg.childControlWidth = true;
        headerVlg.childControlHeight = false;

        CreateTitle(headerObj.transform);
        CreateSubtitle(headerObj.transform);

        // Space betweeen header and buttons
        GameObject topSpacer = new GameObject("TopSpacer");
        topSpacer.transform.SetParent(contentObj.transform, false);
        LayoutElement topSpacerLe = topSpacer.AddComponent<LayoutElement>();
        topSpacerLe.flexibleHeight = 0.5f;

        // --- Buttons (.home-buttons) ---
        GameObject buttonsObj = new GameObject("ButtonsGroup");
        buttonsObj.transform.SetParent(contentObj.transform, false);
        RectTransform buttonsRect = buttonsObj.AddComponent<RectTransform>();
        buttonsRect.sizeDelta = new Vector2(850, 750);
        LayoutElement btnGroupLe = buttonsObj.AddComponent<LayoutElement>();
        // Constrain max width
        btnGroupLe.preferredWidth = 850;

        VerticalLayoutGroup btnsVlg = buttonsObj.AddComponent<VerticalLayoutGroup>();
        btnsVlg.childAlignment = TextAnchor.MiddleCenter;
        // PWA uses gap 30px, scaled up = ~60-80
        btnsVlg.spacing = 80; 
        btnsVlg.childControlWidth = false; // Disable to let cards keep their own fixed size!
        btnsVlg.childControlHeight = false;
        btnsVlg.childForceExpandWidth = false;
        btnsVlg.childForceExpandHeight = false;

        // Let's use simple shapes or letters to avoid "square" emoji missing fonts.
        // PWA uses standard images/emojis. If emojis broke in play mode, lets try simple characters or image loading later
        // But for now, using unicode Play (►) and Star (★) as fallback.
        CreatePWAButton(buttonsObj.transform, "Btn_Assistir", playSpriteCache, "ASSISTIR", "Animes e Filmes",
            primaryTop, primaryBottom, () => SceneManager.LoadScene("Watch"));

        CreatePWAButton(buttonsObj.transform, "Btn_Jogar", starSpriteCache, "JOGAR", "Jogos Divertidos",
            secondaryTop, secondaryBottom, () => SceneManager.LoadScene("Play"));

        // Bottom spacer
        GameObject bottomSpacer = new GameObject("BottomSpacer");
        bottomSpacer.transform.SetParent(contentObj.transform, false);
        LayoutElement bottomSpacerLe = bottomSpacer.AddComponent<LayoutElement>();
        bottomSpacerLe.flexibleHeight = 1f;

        // --- Footer (.home-footer) ---
        CreateFooter(contentObj.transform);
    }

    private void CreateTitle(Transform parent)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);
        RectTransform rect = titleObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900, 130);

        TextMeshProUGUI text = titleObj.AddComponent<TextMeshProUGUI>();
        text.text = "Will Apps";
        text.fontSize = 130; 
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        // Simulating the intense red text-shadow: 0 0 20px #FF6B6B
        addGlow(titleObj, titleGlowColor, new Vector2(0, 0), Color.white);
    }

    private void addGlow(GameObject target, Color glowColor, Vector2 effectDistance, Color textColor)
    {
       Shadow s1 = target.AddComponent<Shadow>();
       s1.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.4f);
       s1.effectDistance = new Vector2(4, -4);

       Shadow s2 = target.AddComponent<Shadow>();
       s2.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.4f);
       s2.effectDistance = new Vector2(-4, 4);

       Shadow s3 = target.AddComponent<Shadow>();
       s3.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.4f);
       s3.effectDistance = new Vector2(4, 4);

       Shadow s4 = target.AddComponent<Shadow>();
       s4.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.4f);
       s4.effectDistance = new Vector2(-4, -4);
    }

    private void CreateSubtitle(Transform parent)
    {
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(parent, false);
        RectTransform rect = subObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900, 80);

        TextMeshProUGUI text = subObj.AddComponent<TextMeshProUGUI>();
        text.text = "Diversão Garantida!"; 
        text.fontSize = 48; 
        text.color = textSecondaryColor;
        text.fontStyle = FontStyles.Bold; 
        text.alignment = TextAlignmentOptions.Top;
    }

    private void CreatePWAButton(Transform parent, string goName, Sprite iconSprite, string title, string subTxt,
        Color gradTop, Color gradBottom, System.Action onClick)
    {
        GameObject btnObj = new GameObject(goName);
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(880, 360); 
        
        // Force layout container to respect exact sizes
        LayoutElement btnLe = btnObj.AddComponent<LayoutElement>();
        btnLe.preferredWidth = 880;
        btnLe.preferredHeight = 360;
        btnLe.flexibleWidth = 0;
        btnLe.flexibleHeight = 0;

        // Base image acts as strict mask. Must be the graphic target to mask children.
        Image bgImg = btnObj.AddComponent<Image>();
        bgImg.color = Color.white;
        bgImg.sprite = roundedSpriteCache; 
        bgImg.type = Image.Type.Sliced;
        bgImg.pixelsPerUnitMultiplier = 2f;

        Mask mask = btnObj.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // Gradient
        WillAppsUIGradient gradient = btnObj.AddComponent<WillAppsUIGradient>();
        gradient.Color1 = gradTop;
        gradient.Color2 = gradBottom;

        // Simple Shadow below the actual button graphic
        GameObject shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(btnObj.transform, false);
        shadowObj.transform.SetAsFirstSibling();
        // Place it just behind, slightly offset down
        RectTransform srect = shadowObj.AddComponent<RectTransform>();
        srect.anchorMin = Vector2.zero;
        srect.anchorMax = Vector2.one;
        srect.offsetMin = new Vector2(4, -15f);
        srect.offsetMax = new Vector2(-4, -5f);
        Image simg = shadowObj.AddComponent<Image>();
        simg.sprite = roundedSpriteCache;
        simg.type = Image.Type.Sliced;
        simg.pixelsPerUnitMultiplier = 2f; 
        simg.color = new Color(0f, 0f, 0f, 0.45f);

        // Button Component
        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());

        // Vertical Layout for Content
        GameObject contentGroup = new GameObject("ContentGroup");
        contentGroup.transform.SetParent(btnObj.transform, false);
        RectTransform cRect = contentGroup.AddComponent<RectTransform>();
        cRect.anchorMin = Vector2.zero; cRect.anchorMax = Vector2.one;
        cRect.offsetMin = Vector2.zero; cRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = contentGroup.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 15;
        vlg.padding = new RectOffset(20, 20, 30, 30);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true; 

        // 1. Icon (Image instead of font character to avoid "missing icon" blocks)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(contentGroup.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(100, 100);
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = iconSprite;
        iconImg.color = Color.white;
        iconImg.preserveAspect = true;
        // 2. Title label
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(contentGroup.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 72; 
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 8; 
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;

        // 3. Sub label 
        GameObject subObj = new GameObject("SubLabel");
        subObj.transform.SetParent(contentGroup.transform, false);
        TextMeshProUGUI subText = subObj.AddComponent<TextMeshProUGUI>();
        subText.text = subTxt;
        subText.fontSize = 38; 
        subText.color = new Color(1f, 1f, 1f, 0.9f); 
        subText.alignment = TextAlignmentOptions.Top;
    }

    private void CreateFooter(Transform parent)
    {
        GameObject footerObj = new GameObject("Footer");
        footerObj.transform.SetParent(parent, false);
        RectTransform rect = footerObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900, 50);

        TextMeshProUGUI text = footerObj.AddComponent<TextMeshProUGUI>();
        text.text = "v2.1 Unity";
        text.fontSize = 32;
        text.color = new Color(textSecondaryColor.r, textSecondaryColor.g, textSecondaryColor.b, 0.5f);
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

    // Creates a pure white rounded rect sprite at runtime!
    private Sprite CreateRoundedSprite(int width, int height, int radius)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[width * height];
        Color32 transparent = new Color32(0, 0, 0, 0);
        Color32 white = new Color32(255, 255, 255, 255);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Calculate distance from closest corner
                int cornerX = x < radius ? radius : (x > width - radius ? width - radius - 1 : x);
                int cornerY = y < radius ? radius : (y > height - radius ? height - radius - 1 : y);
                
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerX, cornerY));
                
                if (dist > radius)
                {
                    pixels[y * width + x] = transparent; // Outside radius
                }
                else
                {
                    pixels[y * width + x] = white; // Inside pill shape
                }
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();

        // Slice borders so it scales like UI cards
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }
 
    private Sprite CreatePlaySprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;
 
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Triangle math: x from 0 to width, y symmetrical around height/2
                float normalizedX = (float)x / width;
                float halfHeight = height / 2f;
                float limitY = halfHeight * normalizedX;
 
                if (Mathf.Abs(y - halfHeight) <= limitY)
                    pixels[y * width + x] = white;
                else
                    pixels[y * width + x] = transparent;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
 
    private Sprite CreateStarSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;
 
        float centerX = width / 2f;
        float centerY = height / 2f;
        float outerRadius = width * 0.45f;
        float innerRadius = width * 0.2f;
        int points = 5;
 
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
 
                // Star shape logic
                float anglePerPoint = 360f / points;
                float halfAngle = anglePerPoint / 2f;
                float localAngle = (angle + 90f + 360f) % anglePerPoint;
                
                float r = (localAngle < halfAngle) ? 
                    Mathf.Lerp(outerRadius, innerRadius, localAngle / halfAngle) :
                    Mathf.Lerp(innerRadius, outerRadius, (localAngle - halfAngle) / halfAngle);
 
                pixels[y * width + x] = (dist <= r) ? white : transparent;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
}

/// <summary>
/// Simple helper component to apply a vertical Linear Gradient to a UI Image.
/// </summary>
[RequireComponent(typeof(Graphic))]
public class WillAppsUIGradient : BaseMeshEffect
{
    public Color Color1 = Color.white;
    public Color Color2 = Color.black;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        List<UIVertex> vertices = new List<UIVertex>();
        vh.GetUIVertexStream(vertices);

        float bottomY = vertices[0].position.y;
        float topY = vertices[0].position.y;

        for (int i = 1; i < vertices.Count; i++)
        {
            float y = vertices[i].position.y;
            if (y > topY) topY = y;
            else if (y < bottomY) bottomY = y;
        }

        float uiElementHeight = topY - bottomY;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            UIVertex uiVertex = new UIVertex();
            vh.PopulateUIVertex(ref uiVertex, i);
            float normalizedY = (uiVertex.position.y - bottomY) / uiElementHeight;
            uiVertex.color = Color32.Lerp(Color2, Color1, normalizedY);
            vh.SetUIVertex(uiVertex, i);
        }
    }
}
