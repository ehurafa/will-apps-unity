using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this script to the Main Camera in the FlappyBird scene.
/// It creates the entire Flappy Bird game programmatically: bird, pipes, UI, boundaries.
/// </summary>
public class FlappyBirdSetup : MonoBehaviour
{
    private readonly Color skyColor = new Color(0.529f, 0.808f, 0.922f, 1f);  // #87CEEB
    private readonly Color birdColor = new Color(1f, 0.902f, 0.427f, 1f);     // #FFE66D
    private readonly Color birdOutline = new Color(1f, 0.671f, 0.298f, 1f);   // #FFAB4C
    private readonly Color uiBgColor = new Color(0.1f, 0.1f, 0.18f, 0.85f);

    private FlappyGameManager gameManager;

    private void Start()
    {
        SetupCamera();
        SetupTags();
        CreateBird();
        CreateBoundaries();
        CreateUI();
        CreatePipeSpawner();
        ConnectSystems();
    }

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        cam.backgroundColor = skyColor;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
    }

    private void SetupTags()
    {
        // Tags are created via code at build-time, but we need them at runtime.
        // Unity requires tags to be defined in the Tag Manager.
        // We'll use layers/names instead if tags aren't pre-defined.
        // For a code-only approach, we'll handle collision via layer or name checking.
    }

    private void CreateBird()
    {
        GameObject bird = new GameObject("Bird");
        bird.transform.position = new Vector3(-2f, 0f, 0f);
        bird.tag = "Player";

        // Sprite (circle)
        SpriteRenderer sr = bird.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = birdColor;
        sr.sortingOrder = 10;

        bird.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

        // Physics
        Rigidbody2D rb = bird.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // Start frozen until game starts
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        CircleCollider2D col = bird.AddComponent<CircleCollider2D>();
        col.radius = 0.45f;

        // Controller
        BirdController controller = bird.AddComponent<BirdController>();
    }

    private void CreateBoundaries()
    {
        // Top boundary
        CreateBoundary("TopBoundary", new Vector3(0, 5.5f, 0), new Vector2(30f, 1f));

        // Bottom boundary (ground)
        CreateBoundary("BottomBoundary", new Vector3(0, -5.5f, 0), new Vector2(30f, 1f));

        // Ground visual
        GameObject groundVisual = new GameObject("GroundVisual");
        groundVisual.transform.position = new Vector3(0, -5.2f, 0);
        SpriteRenderer sr = groundVisual.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = new Color(0.33f, 0.69f, 0.56f, 1f); // Greenish ground
        groundVisual.transform.localScale = new Vector3(30f, 1f, 1f);
        sr.sortingOrder = 5;
    }

    private void CreateBoundary(string name, Vector3 position, Vector2 size)
    {
        GameObject boundary = new GameObject(name);
        boundary.transform.position = position;
        boundary.tag = "Obstacle";

        BoxCollider2D col = boundary.AddComponent<BoxCollider2D>();
        col.size = size;

        // Invisible — no SpriteRenderer needed
        Rigidbody2D rb = boundary.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    private void CreatePipeSpawner()
    {
        GameObject spawnerObj = new GameObject("PipeSpawner");
        PipeSpawner spawner = spawnerObj.AddComponent<PipeSpawner>();
        spawner.spawnInterval = 1.8f;
        spawner.pipeSpeed = 3f;
        spawner.gapSize = 3f;
        spawner.spawnX = 10f;
        spawner.minY = -1.5f;
        spawner.maxY = 2f;
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

        // --- Top HUD (Score) ---
        GameObject topHud = new GameObject("TopHUD");
        topHud.transform.SetParent(canvasObj.transform, false);
        RectTransform topRect = topHud.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1);
        topRect.sizeDelta = new Vector2(0, 200);

        VerticalLayoutGroup topVlg = topHud.AddComponent<VerticalLayoutGroup>();
        topVlg.childAlignment = TextAnchor.UpperCenter;
        topVlg.spacing = 10;
        topVlg.padding = new RectOffset(0, 0, 40, 0);
        topVlg.childControlWidth = true;
        topVlg.childControlHeight = false;

        // Score text (big)
        TextMeshProUGUI scoreText = CreateText(topHud.transform, "ScoreText", "0", 100, Color.white);
        scoreText.fontStyle = FontStyles.Bold;

        // High score text (small)
        TextMeshProUGUI highScoreText = CreateText(topHud.transform, "HighScoreText", "Recorde: 0", 32, new Color(1, 1, 1, 0.6f));

        // --- Back Button (top-left) ---
        GameObject backBtn = new GameObject("Btn_Back");
        backBtn.transform.SetParent(canvasObj.transform, false);
        RectTransform backRect = backBtn.AddComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0, 1);
        backRect.anchorMax = new Vector2(0, 1);
        backRect.pivot = new Vector2(0, 1);
        backRect.anchoredPosition = new Vector2(20, -20);
        backRect.sizeDelta = new Vector2(100, 100);

        Image backImg = backBtn.AddComponent<Image>();
        backImg.color = new Color(0, 0, 0, 0.4f);

        Button backButton = backBtn.AddComponent<Button>();
        backButton.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"));

        TextMeshProUGUI backText = CreateText(backBtn.transform, "BackIcon", "←", 50, Color.white);
        RectTransform btRect = backText.GetComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.offsetMin = Vector2.zero;
        btRect.offsetMax = Vector2.zero;

        // --- Start Panel ---
        GameObject startPanel = CreateOverlayPanel(canvasObj.transform, "StartPanel");
        CreateText(startPanel.transform, "StartText", "Toque para começar!", 50, new Color(1f, 0.9f, 0.43f, 1f));

        // --- Game Over Panel ---
        GameObject gameOverPanel = CreateOverlayPanel(canvasObj.transform, "GameOverPanel");
        gameOverPanel.SetActive(false);

        VerticalLayoutGroup goVlg = gameOverPanel.GetComponent<VerticalLayoutGroup>();
        if (goVlg == null)
        {
            goVlg = gameOverPanel.AddComponent<VerticalLayoutGroup>();
        }
        goVlg.childAlignment = TextAnchor.MiddleCenter;
        goVlg.spacing = 20;
        goVlg.childControlWidth = true;
        goVlg.childControlHeight = false;
        goVlg.padding = new RectOffset(80, 80, 0, 0);

        CreateText(gameOverPanel.transform, "GameOverTitle", "Game Over!", 60, new Color(1f, 0.42f, 0.42f, 1f));
        TextMeshProUGUI goScoreText = CreateText(gameOverPanel.transform, "GOScore", "Pontuação: 0", 40, Color.white);
        TextMeshProUGUI newRecordText = CreateText(gameOverPanel.transform, "NewRecord", "\ud83c\udfc6 Novo Recorde!", 36, new Color(0.584f, 0.882f, 0.827f, 1f));
        newRecordText.gameObject.SetActive(false);

        // Restart button
        CreateActionButton(gameOverPanel.transform, "Jogar Novamente",
            new Color(1f, 0.42f, 0.42f, 1f), () =>
            {
                FlappyGameManager gm = FindAnyObjectByType<FlappyGameManager>();
                if (gm != null) gm.RestartGame();
            });

        // Menu button
        CreateActionButton(gameOverPanel.transform, "Menu Principal",
            new Color(0.086f, 0.129f, 0.243f, 1f), () =>
            {
                FlappyGameManager gm = FindAnyObjectByType<FlappyGameManager>();
                if (gm != null) gm.GoToMenu();
            });

        // --- Game Manager ---
        GameObject gmObj = new GameObject("GameManager");
        gameManager = gmObj.AddComponent<FlappyGameManager>();
        gameManager.scoreText = scoreText;
        gameManager.highScoreText = highScoreText;
        gameManager.gameOverPanel = gameOverPanel;
        gameManager.gameOverScoreText = goScoreText;
        gameManager.newRecordText = newRecordText;
        gameManager.startPanel = startPanel;
    }

    private void ConnectSystems()
    {
        BirdController bird = FindAnyObjectByType<BirdController>();
        PipeSpawner spawner = FindAnyObjectByType<PipeSpawner>();

        if (gameManager != null && bird != null && spawner != null)
        {
            gameManager.SetReferences(bird, spawner);
        }
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
        text.enableAutoSizing = false;
        return text;
    }

    private GameObject CreateOverlayPanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.8f);

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 20;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.padding = new RectOffset(80, 80, 0, 0);

        return panel;
    }

    private void CreateActionButton(Transform parent, string label, Color bgColor, System.Action onClick)
    {
        GameObject btnObj = new GameObject("Btn_" + label.Replace(" ", ""));
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600, 90);

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform lRect = labelObj.AddComponent<RectTransform>();
        lRect.anchorMin = Vector2.zero;
        lRect.anchorMax = Vector2.one;
        lRect.offsetMin = Vector2.zero;
        lRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = labelObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 32;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
    }

    private Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        float radius = size / 2f - 1;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                pixels[y * size + x] = dist <= radius ? Color.white : Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }
}
