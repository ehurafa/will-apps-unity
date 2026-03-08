using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this script to the Main Camera in the FlappyNinja scene.
/// Creates the entire Flappy Ninja game programmatically:
/// Menu → Character Select → Game → Game Over.
/// </summary>
public class FlappyNinjaSetup : MonoBehaviour
{
    // Naruto theme colors
    private readonly Color bgColor = new Color(0.133f, 0.133f, 0.133f, 1f);     // #222
    private readonly Color skyTop = new Color(0.31f, 0.765f, 0.969f, 1f);       // #4FC3F7
    private readonly Color skyBottom = new Color(0.882f, 0.961f, 0.996f, 1f);   // #E1F5FE
    private readonly Color groundColor = new Color(0.365f, 0.251f, 0.216f, 1f); // #5D4037
    private readonly Color grassColor = new Color(0.22f, 0.557f, 0.235f, 1f);   // #388E3C
    private readonly Color bambooColor = new Color(0.459f, 0.757f, 0.302f, 1f); // #75C24D
    private readonly Color bambooEdge = new Color(0.18f, 0.49f, 0.196f, 1f);    // #2E7D32
    private readonly Color goldColor = new Color(1f, 0.843f, 0f, 1f);           // #FFD700
    private readonly Color uiBgColor = new Color(0.13f, 0.13f, 0.13f, 0.9f);

    // Game state
    private enum GameScreen { Menu, CharacterSelect, Playing, GameOver }
    private GameScreen currentScreen = GameScreen.Menu;
    private NinjaCharacter selectedCharacter;
    private int score = 0;
    private int highScore = 0;
    private bool isGameStarted = false;

    // Game objects
    private GameObject ninjaObj;
    private NinjaBirdController ninjaController;
    private GameObject pipeSpawnerObj;
    private PipeSpawner pipeSpawner;

    // UI panels
    private GameObject menuPanel;
    private GameObject charSelectPanel;
    private GameObject gameUI;
    private GameObject gameOverPanel;
    private Canvas mainCanvas;

    // Game UI refs
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI highScoreText;
    private TextMeshProUGUI goScoreText;
    private TextMeshProUGUI newRecordText;
    private TextMeshProUGUI startHintText;

    // Background animation
    private Transform bgTransform;
    private float bgPulseTime = 0f;

    private void Start()
    {
        selectedCharacter = NinjaCharacter.Naruto;
        highScore = PlayerPrefs.GetInt("FlappyNinjaHighScore", 0);

        SetupCamera();
        CreateBackground();
        CreateGameWorld();
        CreateUI();
        ShowScreen(GameScreen.Menu);
    }

    private void Update()
    {
        // Background pulse animation
        if (bgTransform != null)
        {
            bgPulseTime += Time.deltaTime;
            float scale = 1.0f + ((Mathf.Sin(bgPulseTime) + 1f) / 2f) * 0.03f;
            bgTransform.localScale = new Vector3(scale, scale, 1f);
        }

        // Handle "tap to start" during Playing screen
        if (currentScreen == GameScreen.Playing && !isGameStarted)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                StartPlaying();
            }
        }

        // Check ninja fell off screen
        if (isGameStarted && ninjaController != null && ninjaObj != null && ninjaObj.transform.position.y < -6f)
        {
            ninjaController.Die();
        }
    }

    // ========== SETUP ==========

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        cam.backgroundColor = skyTop;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
    }

    private void CreateBackground()
    {
        // Sky gradient (using a simple colored quad)
        GameObject bg = new GameObject("Background");
        bg.transform.position = new Vector3(0, 0, 5f); // Behind everything
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = CreateSquareSprite();
        bgSr.color = skyTop;
        bg.transform.localScale = new Vector3(20f, 12f, 1f);
        bgSr.sortingOrder = -10;
        bgTransform = bg.transform;

        // Ground visual
        GameObject ground = new GameObject("GroundVisual");
        ground.transform.position = new Vector3(0, -5.2f, 0);
        SpriteRenderer groundSr = ground.AddComponent<SpriteRenderer>();
        groundSr.sprite = CreateSquareSprite();
        groundSr.color = groundColor;
        ground.transform.localScale = new Vector3(30f, 1f, 1f);
        groundSr.sortingOrder = 5;

        // Grass strip
        GameObject grass = new GameObject("Grass");
        grass.transform.position = new Vector3(0, -4.75f, 0);
        SpriteRenderer grassSr = grass.AddComponent<SpriteRenderer>();
        grassSr.sprite = CreateSquareSprite();
        grassSr.color = grassColor;
        grass.transform.localScale = new Vector3(30f, 0.15f, 1f);
        grassSr.sortingOrder = 6;
    }

    private void CreateGameWorld()
    {
        // Boundaries
        CreateBoundary("TopBoundary", new Vector3(0, 5.5f, 0), new Vector2(30f, 1f));
        CreateBoundary("BottomBoundary", new Vector3(0, -5.5f, 0), new Vector2(30f, 1f));

        // Ninja (bird)
        ninjaObj = new GameObject("Ninja");
        ninjaObj.transform.position = new Vector3(-2f, 0f, 0f);
        ninjaObj.tag = "Player";

        SpriteRenderer sr = ninjaObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = selectedCharacter.color;
        sr.sortingOrder = 10;
        ninjaObj.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        Rigidbody2D rb = ninjaObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        CircleCollider2D col = ninjaObj.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        ninjaController = ninjaObj.AddComponent<NinjaBirdController>();
        ninjaController.SetCharacter(selectedCharacter);
        ninjaController.OnDied += OnNinjaDied;

        ninjaObj.SetActive(false);

        // Pipe Spawner (reuses PipeSpawner from Flappy Bird but with bamboo colors)
        pipeSpawnerObj = new GameObject("PipeSpawner");
        pipeSpawner = pipeSpawnerObj.AddComponent<PipeSpawner>();
        pipeSpawner.spawnInterval = 1.8f;
        pipeSpawner.pipeSpeed = 3f;
        pipeSpawner.gapSize = 3f;
        pipeSpawner.spawnX = 10f;
        pipeSpawner.minY = -1.5f;
        pipeSpawner.maxY = 2f;
    }

    private void CreateBoundary(string name, Vector3 position, Vector2 size)
    {
        GameObject boundary = new GameObject(name);
        boundary.transform.position = position;
        boundary.tag = "Obstacle";

        BoxCollider2D col = boundary.AddComponent<BoxCollider2D>();
        col.size = size;

        Rigidbody2D rb = boundary.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    // ========== UI ==========

    private void CreateUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("Canvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Event System
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        CreateMenuPanel(canvasObj.transform);
        CreateCharSelectPanel(canvasObj.transform);
        CreateGameUI(canvasObj.transform);
        CreateGameOverPanel(canvasObj.transform);
    }

    // --- Menu Panel ---
    private void CreateMenuPanel(Transform parent)
    {
        menuPanel = CreateFullOverlay(parent, "MenuPanel", uiBgColor);

        VerticalLayoutGroup vlg = menuPanel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 30;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.padding = new RectOffset(80, 80, 0, 0);

        // Ninja emoji
        CreateText(menuPanel.transform, "NinjaIcon", "\ud83e\udd77", 100, Color.white);

        // Title
        TextMeshProUGUI title = CreateText(menuPanel.transform, "Title", "FLAPPY NINJA", 64, goldColor);
        title.fontStyle = FontStyles.Bold;

        // Subtitle
        CreateText(menuPanel.transform, "Subtitle", "Tema Naruto", 32, new Color(1f, 1f, 1f, 0.6f));

        CreateSpacer(menuPanel.transform, 40);

        // Play button
        CreateActionButton(menuPanel.transform, "\u25b6  JOGAR", selectedCharacter.color, () =>
        {
            ShowScreen(GameScreen.Playing);
        });

        // Character select button
        CreateActionButton(menuPanel.transform, "\ud83e\udd77  ESCOLHER NINJA", new Color(0.33f, 0.33f, 0.33f, 1f), () =>
        {
            ShowScreen(GameScreen.CharacterSelect);
        });

        CreateSpacer(menuPanel.transform, 20);

        // Back to Play menu
        CreateActionButton(menuPanel.transform, "\u2190  VOLTAR", new Color(0.2f, 0.2f, 0.2f, 1f), () =>
        {
            SceneManager.LoadScene("Play");
        });

        // High score
        CreateText(menuPanel.transform, "HighScoreMenu", "Recorde: " + highScore, 28, new Color(1f, 1f, 1f, 0.4f));
    }

    // --- Character Select Panel ---
    private void CreateCharSelectPanel(Transform parent)
    {
        charSelectPanel = CreateFullOverlay(parent, "CharSelectPanel", new Color(0, 0, 0, 0.95f));

        VerticalLayoutGroup vlg = charSelectPanel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 25;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.padding = new RectOffset(60, 60, 0, 0);

        // Title
        TextMeshProUGUI title = CreateText(charSelectPanel.transform, "Title", "ESCOLHA SEU NINJA", 48, goldColor);
        title.fontStyle = FontStyles.Bold;

        CreateSpacer(charSelectPanel.transform, 20);

        // Character cards
        foreach (var character in NinjaCharacter.All)
        {
            CreateCharacterCard(charSelectPanel.transform, character);
        }

        CreateSpacer(charSelectPanel.transform, 30);

        // Back button
        CreateActionButton(charSelectPanel.transform, "VOLTAR", new Color(0.4f, 0.4f, 0.4f, 1f), () =>
        {
            ShowScreen(GameScreen.Menu);
        });
    }

    private void CreateCharacterCard(Transform parent, NinjaCharacter character)
    {
        GameObject card = new GameObject("Card_" + character.id);
        card.transform.SetParent(parent, false);
        RectTransform rect = card.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800, 140);

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.preferredHeight = 140;

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        Button btn = card.AddComponent<Button>();
        string capturedId = character.id;
        NinjaCharacter capturedChar = character;
        btn.onClick.AddListener(() =>
        {
            SelectCharacter(capturedChar);
        });

        HorizontalLayoutGroup hlg = card.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 20;
        hlg.padding = new RectOffset(20, 20, 10, 10);
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;

        // Color circle
        GameObject circle = new GameObject("ColorCircle");
        circle.transform.SetParent(card.transform, false);
        RectTransform circleRect = circle.AddComponent<RectTransform>();
        circleRect.sizeDelta = new Vector2(80, 80);
        LayoutElement circleLE = circle.AddComponent<LayoutElement>();
        circleLE.preferredWidth = 80;

        Image circleImg = circle.AddComponent<Image>();
        circleImg.color = character.color;
        // Make it round-ish
        circleImg.type = Image.Type.Simple;

        // Info
        GameObject info = new GameObject("Info");
        info.transform.SetParent(card.transform, false);
        RectTransform infoRect = info.AddComponent<RectTransform>();
        infoRect.sizeDelta = new Vector2(400, 100);
        LayoutElement infoLE = info.AddComponent<LayoutElement>();
        infoLE.preferredWidth = 400;
        infoLE.flexibleWidth = 1;

        VerticalLayoutGroup infoVlg = info.AddComponent<VerticalLayoutGroup>();
        infoVlg.childAlignment = TextAnchor.MiddleLeft;
        infoVlg.spacing = 5;
        infoVlg.childControlWidth = true;
        infoVlg.childControlHeight = false;

        // Name
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(info.transform, false);
        nameObj.AddComponent<RectTransform>().sizeDelta = new Vector2(350, 40);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = character.name;
        nameText.fontSize = 34;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.MidlineLeft;

        // Stats
        string statsStr = $"Pulo: {character.jumpForce:F1}  |  Gravidade: {character.gravityScale:F1}";
        GameObject statsObj = new GameObject("Stats");
        statsObj.transform.SetParent(info.transform, false);
        statsObj.AddComponent<RectTransform>().sizeDelta = new Vector2(350, 30);
        TextMeshProUGUI statsText = statsObj.AddComponent<TextMeshProUGUI>();
        statsText.text = statsStr;
        statsText.fontSize = 22;
        statsText.color = new Color(1f, 1f, 1f, 0.5f);
        statsText.alignment = TextAlignmentOptions.MidlineLeft;

        // Selected badge
        if (character.id == selectedCharacter.id)
        {
            GameObject selectedBadge = new GameObject("SelectedBadge");
            selectedBadge.transform.SetParent(card.transform, false);
            RectTransform badgeRect = selectedBadge.AddComponent<RectTransform>();
            badgeRect.sizeDelta = new Vector2(160, 50);
            LayoutElement badgeLE = selectedBadge.AddComponent<LayoutElement>();
            badgeLE.preferredWidth = 160;

            Image badgeBg = selectedBadge.AddComponent<Image>();
            badgeBg.color = goldColor;

            GameObject badgeTextObj = new GameObject("Text");
            badgeTextObj.transform.SetParent(selectedBadge.transform, false);
            RectTransform btRect = badgeTextObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.offsetMin = Vector2.zero;
            btRect.offsetMax = Vector2.zero;
            TextMeshProUGUI badgeText = badgeTextObj.AddComponent<TextMeshProUGUI>();
            badgeText.text = "\u2713";
            badgeText.fontSize = 30;
            badgeText.fontStyle = FontStyles.Bold;
            badgeText.color = Color.black;
            badgeText.alignment = TextAlignmentOptions.Center;
        }

        // Border if selected
        if (character.id == selectedCharacter.id)
        {
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = goldColor;
            outline.effectDistance = new Vector2(3, 3);
        }
    }

    // --- Game UI (HUD) ---
    private void CreateGameUI(Transform parent)
    {
        gameUI = new GameObject("GameUI");
        gameUI.transform.SetParent(parent, false);
        RectTransform guiRect = gameUI.AddComponent<RectTransform>();
        guiRect.anchorMin = Vector2.zero;
        guiRect.anchorMax = Vector2.one;
        guiRect.offsetMin = Vector2.zero;
        guiRect.offsetMax = Vector2.zero;

        // Top HUD
        GameObject topHud = new GameObject("TopHUD");
        topHud.transform.SetParent(gameUI.transform, false);
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

        scoreText = CreateText(topHud.transform, "ScoreText", "0", 100, Color.white);
        scoreText.fontStyle = FontStyles.Bold;

        highScoreText = CreateText(topHud.transform, "HighScoreText", "Recorde: " + highScore, 32, new Color(1, 1, 1, 0.6f));

        // Back button (top-left)
        GameObject backBtn = new GameObject("Btn_Back");
        backBtn.transform.SetParent(gameUI.transform, false);
        RectTransform backRect = backBtn.AddComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0, 1);
        backRect.anchorMax = new Vector2(0, 1);
        backRect.pivot = new Vector2(0, 1);
        backRect.anchoredPosition = new Vector2(20, -20);
        backRect.sizeDelta = new Vector2(100, 100);

        Image backImg = backBtn.AddComponent<Image>();
        backImg.color = new Color(0, 0, 0, 0.4f);

        Button backButton = backBtn.AddComponent<Button>();
        backButton.onClick.AddListener(() => ShowScreen(GameScreen.Menu));

        TextMeshProUGUI backText = CreateText(backBtn.transform, "BackIcon", "\u2190", 50, Color.white);
        RectTransform btRect = backText.GetComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.offsetMin = Vector2.zero;
        btRect.offsetMax = Vector2.zero;

        // Start hint
        startHintText = CreateText(gameUI.transform, "StartHint", "Toque para começar!", 50, goldColor);
        RectTransform hintRect = startHintText.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0.5f);
        hintRect.anchorMax = new Vector2(0.5f, 0.5f);
        hintRect.sizeDelta = new Vector2(900, 80);
    }

    // --- Game Over Panel ---
    private void CreateGameOverPanel(Transform parent)
    {
        gameOverPanel = CreateFullOverlay(parent, "GameOverPanel", new Color(0, 0, 0, 0.85f));

        VerticalLayoutGroup vlg = gameOverPanel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 25;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.padding = new RectOffset(80, 80, 0, 0);

        // Game Over title
        TextMeshProUGUI goTitle = CreateText(gameOverPanel.transform, "GOTitle", "Game Over!", 60, new Color(1f, 0.42f, 0.42f, 1f));
        goTitle.fontStyle = FontStyles.Bold;

        // Score
        goScoreText = CreateText(gameOverPanel.transform, "GOScore", "Pontuação: 0", 40, Color.white);

        // New Record
        newRecordText = CreateText(gameOverPanel.transform, "NewRecord", "\ud83c\udfc6 Novo Recorde!", 36, goldColor);
        newRecordText.gameObject.SetActive(false);

        CreateSpacer(gameOverPanel.transform, 20);

        // Retry
        CreateActionButton(gameOverPanel.transform, "Jogar Novamente", selectedCharacter.color, () =>
        {
            RestartGame();
            ShowScreen(GameScreen.Playing);
        });

        // Menu
        CreateActionButton(gameOverPanel.transform, "Menu", new Color(0.2f, 0.2f, 0.2f, 1f), () =>
        {
            RestartGame();
            ShowScreen(GameScreen.Menu);
        });

        // Back to Play
        CreateActionButton(gameOverPanel.transform, "Sair", new Color(0.15f, 0.15f, 0.15f, 1f), () =>
        {
            SceneManager.LoadScene("Play");
        });
    }

    // ========== GAME LOGIC ==========

    private void ShowScreen(GameScreen screen)
    {
        currentScreen = screen;

        menuPanel.SetActive(screen == GameScreen.Menu);
        charSelectPanel.SetActive(screen == GameScreen.CharacterSelect);
        gameUI.SetActive(screen == GameScreen.Playing || screen == GameScreen.GameOver);
        gameOverPanel.SetActive(screen == GameScreen.GameOver);

        if (screen == GameScreen.Playing)
        {
            PrepareGame();
        }
        else
        {
            ninjaObj.SetActive(false);
        }
    }

    private void SelectCharacter(NinjaCharacter character)
    {
        selectedCharacter = character;

        // Rebuild char select panel to show updated selection
        if (charSelectPanel != null)
        {
            Destroy(charSelectPanel);
            CreateCharSelectPanel(mainCanvas.transform);
            charSelectPanel.SetActive(true);
        }

        // Update ninja visuals
        if (ninjaController != null)
        {
            ninjaController.SetCharacter(selectedCharacter);
        }
    }

    private void PrepareGame()
    {
        isGameStarted = false;
        score = 0;
        UpdateScoreUI();

        // Reset ninja
        ninjaObj.SetActive(true);
        ninjaController.SetCharacter(selectedCharacter);
        ninjaController.ResetNinja(new Vector2(-2f, 0f));

        // Show start hint
        if (startHintText != null) startHintText.gameObject.SetActive(true);

        // Clean pipes
        foreach (var pipe in FindObjectsByType<PipeMove>(FindObjectsSortMode.None))
        {
            Destroy(pipe.gameObject);
        }
    }

    private void StartPlaying()
    {
        isGameStarted = true;
        ninjaController.EnablePhysics();

        if (startHintText != null) startHintText.gameObject.SetActive(false);
        if (pipeSpawner != null) pipeSpawner.StartSpawning();
    }

    private void OnNinjaDied()
    {
        isGameStarted = false;

        if (pipeSpawner != null) pipeSpawner.StopSpawning();

        // Check high score
        bool isNewRecord = false;
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("FlappyNinjaHighScore", highScore);
            PlayerPrefs.Save();
            isNewRecord = true;
        }

        // Show game over
        if (goScoreText != null) goScoreText.text = "Pontuação: " + score;
        if (newRecordText != null) newRecordText.gameObject.SetActive(isNewRecord && score > 0);

        UpdateScoreUI();
        ShowScreen(GameScreen.GameOver);
    }

    private void RestartGame()
    {
        // Clean all pipes
        foreach (var pipe in FindObjectsByType<PipeMove>(FindObjectsSortMode.None))
        {
            Destroy(pipe.gameObject);
        }

        isGameStarted = false;
        score = 0;
    }

    // Score trigger detection — called from NinjaBirdController via OnTriggerEnter2D
    // We override the detection here using a separate approach
    private void OnEnable()
    {
        // Register a score listener
        InvokeRepeating(nameof(CheckScoreTriggers), 0.1f, 0.1f);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(CheckScoreTriggers));
    }

    private void CheckScoreTriggers()
    {
        // Score triggers are handled by NinjaBirdController's OnTriggerEnter2D
        // We just need to count destroyed triggers. Instead, let's use a different approach:
        // Listen for trigger events via the ninja controller.
    }

    // Public method for score - the NinjaBirdController calls FindAnyObjectByType to find this
    public void AddScore()
    {
        if (!isGameStarted) return;
        score++;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = score.ToString();
        if (highScoreText != null) highScoreText.text = "Recorde: " + highScore;
    }

    // ========== HELPERS ==========

    private GameObject CreateFullOverlay(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = panel.AddComponent<Image>();
        img.color = color;

        return panel;
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

    private void CreateActionButton(Transform parent, string label, Color bgColor, System.Action onClick)
    {
        GameObject btnObj = new GameObject("Btn_" + label.Replace(" ", ""));
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(700, 100);

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredHeight = 100;

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = colors;
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
        text.fontSize = 36;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
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
