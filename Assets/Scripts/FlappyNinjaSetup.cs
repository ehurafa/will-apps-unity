using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Attach this script to the Main Camera in the FlappyNinja scene.
/// Replicates the PWA Flappy Ninja game with:
/// - Menu with gradient sky, floating Naruto, "INICIAR MISSÃO"
/// - Gameplay with konoha.png background, sprite-sheet Naruto, bamboo pipes, audio
/// - Game Over with "FIM DE JOGO", score card, retry/menu buttons
/// </summary>
public class FlappyNinjaSetup : MonoBehaviour
{
    // PWA Colors
    private readonly Color bgDark = new Color(0.133f, 0.133f, 0.133f, 1f);       // #222
    private readonly Color skyTop = new Color(0.31f, 0.765f, 0.969f, 1f);        // #4FC3F7 → #87CEEB
    private readonly Color skyBottom = new Color(0.882f, 0.961f, 0.996f, 1f);    // #E1F5FE → #E0F7FA
    private readonly Color ninjaOrange = new Color(1f, 0.42f, 0.208f, 1f);       // #FF6B35
    private readonly Color ninjaOrangeShadow = new Color(0.702f, 0.278f, 0.125f, 1f); // #b34720
    private readonly Color groundColor = new Color(0.365f, 0.251f, 0.216f, 1f);  // #5D4037
    private readonly Color grassColor = new Color(0.22f, 0.557f, 0.235f, 1f);    // #388E3C
    private readonly Color goldColor = new Color(1f, 0.843f, 0f, 1f);            // #FFD700
    private readonly Color crimsonColor = new Color(0.863f, 0.078f, 0.235f, 1f); // #DC143C
    private readonly Color greenBtn = new Color(0.133f, 0.545f, 0.133f, 1f);     // #228B22
    private readonly Color grayBtn = new Color(0.4f, 0.4f, 0.4f, 1f);            // #666
    private readonly Color cardBg = new Color(0.2f, 0.2f, 0.2f, 1f);             // #333

    // Game state
    private enum GameScreen { Menu, Playing, GameOver }
    private GameScreen currentScreen = GameScreen.Menu;
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
    private GameObject gameUI;
    private GameObject gameOverPanel;
    private Canvas mainCanvas;

    // Game UI refs
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI goScoreText;
    private TextMeshProUGUI goHighScoreText;
    private TextMeshProUGUI startHintText;

    // Background
    private Transform bgTransform;
    private float bgPulseTime = 0f;

    // Menu floating animation
    private RectTransform menuNarutoRect;
    private float menuFloatTime = 0f;

    // Sprite sheet animation
    private SpriteRenderer ninjaRenderer;
    private Sprite[] narutoFrames;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private const float FRAME_DURATION = 0.1f; // 100ms per frame

    // Audio
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioClip jumpClip;
    private AudioClip collisionClip;
    private AudioClip scoreClip;
    private AudioClip themeClip;

    // Konoha background sprite
    private Sprite konohaBgSprite;

    private void Start()
    {
        highScore = PlayerPrefs.GetInt("FlappyNinjaHighScore", 0);

        LoadAssets();
        SetupCamera();
        SetupAudio();
        CreateBackground();
        CreateGameWorld();
        CreateUI();
        ShowScreen(GameScreen.Menu);
    }

    private void Update()
    {
        // Background pulse animation (during gameplay)
        if (bgTransform != null && currentScreen == GameScreen.Playing)
        {
            bgPulseTime += Time.deltaTime;
            float scale = 1.0f + ((Mathf.Sin(bgPulseTime) + 1f) / 2f) * 0.05f;
            bgTransform.localScale = new Vector3(scale * 20f, scale * 12f, 1f);
        }

        // Menu floating animation
        if (menuNarutoRect != null && currentScreen == GameScreen.Menu)
        {
            menuFloatTime += Time.deltaTime * 3f;
            float offsetY = Mathf.Sin(menuFloatTime) * 20f;
            menuNarutoRect.anchoredPosition = new Vector2(0, 100 + offsetY);
        }

        // Sprite sheet animation during gameplay
        if (isGameStarted && narutoFrames != null && narutoFrames.Length > 0 && ninjaRenderer != null)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= FRAME_DURATION)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % narutoFrames.Length;
                ninjaRenderer.sprite = narutoFrames[currentFrame];
            }
        }

        // Handle "tap to start" during Playing screen (New Input System)
        if (currentScreen == GameScreen.Playing && !isGameStarted)
        {
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
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

    // ========== LOADING ==========

    private void LoadAssets()
    {
        // Load konoha background
        Texture2D konohaTex = Resources.Load<Texture2D>("Images/FlappyNinja/konoha");
        if (konohaTex != null)
        {
            konohaBgSprite = Sprite.Create(konohaTex,
                new Rect(0, 0, konohaTex.width, konohaTex.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

        // Load naruto sprite sheet (2x2 grid)
        Texture2D narutoTex = Resources.Load<Texture2D>("Images/FlappyNinja/sprite-naruto");
        if (narutoTex != null)
        {
            int cols = 2, rows = 2;
            int fw = narutoTex.width / cols;
            int fh = narutoTex.height / rows;
            narutoFrames = new Sprite[4];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int index = r * cols + c;
                    // Unity texture coordinates: bottom-left origin, so flip Y
                    int y = (rows - 1 - r) * fh;
                    narutoFrames[index] = Sprite.Create(narutoTex,
                        new Rect(c * fw, y, fw, fh),
                        new Vector2(0.5f, 0.5f), fw);
                }
            }
        }

        // Load audio clips
        themeClip = Resources.Load<AudioClip>("Audio/FlappyNinja/theme");
        jumpClip = Resources.Load<AudioClip>("Audio/FlappyNinja/jump");
        collisionClip = Resources.Load<AudioClip>("Audio/FlappyNinja/collision");
        scoreClip = Resources.Load<AudioClip>("Audio/FlappyNinja/sharingan");
    }

    // ========== SETUP ==========

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        cam.backgroundColor = bgDark;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
    }

    private void SetupAudio()
    {
        // Music source (looping)
        GameObject musicObj = new GameObject("MusicSource");
        musicSource = musicObj.AddComponent<AudioSource>();
        musicSource.clip = themeClip;
        musicSource.loop = true;
        musicSource.volume = 0.5f;
        musicSource.playOnAwake = false;

        // SFX source
        GameObject sfxObj = new GameObject("SFXSource");
        sfxSource = sfxObj.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private void CreateBackground()
    {
        // Konoha background (used during gameplay)
        GameObject bg = new GameObject("Background");
        bg.transform.position = new Vector3(0, 0, 5f);
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sortingOrder = -10;

        if (konohaBgSprite != null)
        {
            bgSr.sprite = konohaBgSprite;
        }
        else
        {
            bgSr.sprite = CreateSquareSprite();
            bgSr.color = skyTop;
        }
        bg.transform.localScale = new Vector3(20f, 12f, 1f);
        bgTransform = bg.transform;

        // Ground visual
        GameObject ground = new GameObject("GroundVisual");
        ground.transform.position = new Vector3(0, -4.7f, 0);
        SpriteRenderer groundSr = ground.AddComponent<SpriteRenderer>();
        groundSr.sprite = CreateSquareSprite();
        groundSr.color = groundColor;
        ground.transform.localScale = new Vector3(30f, 0.6f, 1f);
        groundSr.sortingOrder = 5;

        // Grass strip
        GameObject grass = new GameObject("Grass");
        grass.transform.position = new Vector3(0, -4.42f, 0);
        SpriteRenderer grassSr = grass.AddComponent<SpriteRenderer>();
        grassSr.sprite = CreateSquareSprite();
        grassSr.color = grassColor;
        grass.transform.localScale = new Vector3(30f, 0.1f, 1f);
        grassSr.sortingOrder = 6;
    }

    private void CreateGameWorld()
    {
        // Boundaries
        CreateBoundary("TopBoundary", new Vector3(0, 5.5f, 0), new Vector2(30f, 1f));
        CreateBoundary("BottomBoundary", new Vector3(0, -5.0f, 0), new Vector2(30f, 1f));

        // Ninja (hero)
        ninjaObj = new GameObject("Ninja");
        ninjaObj.transform.position = new Vector3(-2f, 0f, 0f);
        ninjaObj.tag = "Player";

        ninjaRenderer = ninjaObj.AddComponent<SpriteRenderer>();
        ninjaRenderer.sortingOrder = 10;

        if (narutoFrames != null && narutoFrames.Length > 0)
        {
            ninjaRenderer.sprite = narutoFrames[0];
            ninjaObj.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
        }
        else
        {
            ninjaRenderer.sprite = CreateCircleSprite();
            ninjaRenderer.color = ninjaOrange;
            ninjaObj.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        }

        Rigidbody2D rb = ninjaObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        CircleCollider2D col = ninjaObj.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;

        ninjaController = ninjaObj.AddComponent<NinjaBirdController>();
        ninjaController.SetCharacter(NinjaCharacter.Naruto);
        ninjaController.OnDied += OnNinjaDied;
        ninjaController.OnJumped += () => PlaySFX(jumpClip);

        ninjaObj.SetActive(false);

        // Pipe Spawner with bamboo colors
        pipeSpawnerObj = new GameObject("PipeSpawner");
        pipeSpawner = pipeSpawnerObj.AddComponent<PipeSpawner>();
        pipeSpawner.spawnInterval = 1.8f;
        pipeSpawner.pipeSpeed = 3f;
        pipeSpawner.gapSize = 3f;
        pipeSpawner.spawnX = 10f;
        pipeSpawner.minY = -1.5f;
        pipeSpawner.maxY = 2f;
        pipeSpawner.pipeColor = new Color(0.459f, 0.757f, 0.302f, 1f); // #75C24D
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
        CreateGameUI(canvasObj.transform);
        CreateGameOverPanel(canvasObj.transform);
    }

    // --- Menu Panel (PWA MainMenu style) ---
    private void CreateMenuPanel(Transform parent)
    {
        menuPanel = CreateFullOverlay(parent, "MenuPanel", Color.clear);

        // Gradient sky background image
        GameObject skyBg = new GameObject("SkyGradient");
        skyBg.transform.SetParent(menuPanel.transform, false);
        RectTransform skyRect = skyBg.AddComponent<RectTransform>();
        skyRect.anchorMin = Vector2.zero;
        skyRect.anchorMax = Vector2.one;
        skyRect.offsetMin = Vector2.zero;
        skyRect.offsetMax = Vector2.zero;
        Image skyImg = skyBg.AddComponent<Image>();
        skyImg.color = skyTop;
        // Add gradient effect
        WillAppsUIGradient gradient = skyBg.AddComponent<WillAppsUIGradient>();
        gradient.Color1 = skyTop;
        gradient.Color2 = skyBottom;

        // Content layout
        GameObject content = new GameObject("Content");
        content.transform.SetParent(menuPanel.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 20;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.padding = new RectOffset(80, 80, 0, 0);

        // Floating Naruto sprite
        GameObject narutoImg = new GameObject("NarutoSprite");
        narutoImg.transform.SetParent(content.transform, false);
        menuNarutoRect = narutoImg.AddComponent<RectTransform>();
        menuNarutoRect.sizeDelta = new Vector2(200, 160);
        LayoutElement narutoLE = narutoImg.AddComponent<LayoutElement>();
        narutoLE.preferredHeight = 160;

        if (narutoFrames != null && narutoFrames.Length > 0)
        {
            Image narutoImage = narutoImg.AddComponent<Image>();
            narutoImage.sprite = narutoFrames[0];
            narutoImage.preserveAspect = true;
        }
        else
        {
            // Fallback emoji
            TextMeshProUGUI emojiText = narutoImg.AddComponent<TextMeshProUGUI>();
            emojiText.text = "\ud83e\udd77";
            emojiText.fontSize = 100;
            emojiText.alignment = TextAlignmentOptions.Center;
        }

        // Title "FLAPPY NINJA"
        TextMeshProUGUI title = CreateText(content.transform, "Title", "FLAPPY NINJA", 72, ninjaOrange);
        title.fontStyle = FontStyles.Bold;
        // (text shadow simulated via outline)
        title.outlineWidth = 0.3f;
        title.outlineColor = Color.black;

        CreateSpacer(content.transform, 40);

        // "INICIAR MISSÃO" button (PWA style: orange bg, black border, shadow)
        CreateStyledButton(content.transform, "INICIAR MISSÃO", ninjaOrange, ninjaOrangeShadow, () =>
        {
            ShowScreen(GameScreen.Playing);
        });

        CreateSpacer(content.transform, 10);

        // Back button
        CreateStyledButton(content.transform, "← VOLTAR", grayBtn, new Color(0.25f, 0.25f, 0.25f, 1f), () =>
        {
            SceneManager.LoadScene("Play");
        });

        CreateSpacer(content.transform, 20);

        // High score
        CreateText(content.transform, "HighScoreMenu", "Recorde: " + highScore, 28, new Color(0.3f, 0.3f, 0.3f, 1f));
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

        // Score text (centered top, large white, like PWA)
        scoreText = CreateText(gameUI.transform, "ScoreText", "0", 100, Color.white);
        scoreText.fontStyle = FontStyles.Bold;
        RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0.5f, 1);
        scoreRect.anchorMax = new Vector2(0.5f, 1);
        scoreRect.pivot = new Vector2(0.5f, 1);
        scoreRect.anchoredPosition = new Vector2(0, -60);
        scoreRect.sizeDelta = new Vector2(400, 120);

        // Back button (top-left circle like PWA)
        GameObject backBtn = new GameObject("Btn_Back");
        backBtn.transform.SetParent(gameUI.transform, false);
        RectTransform backRect = backBtn.AddComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0, 1);
        backRect.anchorMax = new Vector2(0, 1);
        backRect.pivot = new Vector2(0, 1);
        backRect.anchoredPosition = new Vector2(30, -30);
        backRect.sizeDelta = new Vector2(90, 90);

        Image backImg = backBtn.AddComponent<Image>();
        backImg.color = new Color(0, 0, 0, 0.5f);

        Button backButton = backBtn.AddComponent<Button>();
        backButton.onClick.AddListener(() =>
        {
            StopMusic();
            SceneManager.LoadScene("Play");
        });

        TextMeshProUGUI backText = CreateChildText(backBtn.transform, "✕", 40, Color.white);

        // Start hint
        startHintText = CreateText(gameUI.transform, "StartHint", "Toque para começar!", 50, goldColor);
        startHintText.fontStyle = FontStyles.Bold;
        RectTransform hintRect = startHintText.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0.5f);
        hintRect.anchorMax = new Vector2(0.5f, 0.5f);
        hintRect.sizeDelta = new Vector2(900, 80);
    }

    // --- Game Over Panel (PWA GameOver style) ---
    private void CreateGameOverPanel(Transform parent)
    {
        gameOverPanel = CreateFullOverlay(parent, "GameOverPanel", new Color(0, 0, 0, 0.8f));

        VerticalLayoutGroup vlg = gameOverPanel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 20;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.padding = new RectOffset(80, 80, 0, 0);

        // "FIM DE JOGO" title (crimson, 48px)
        TextMeshProUGUI goTitle = CreateText(gameOverPanel.transform, "GOTitle", "FIM DE JOGO", 72, crimsonColor);
        goTitle.fontStyle = FontStyles.Bold;

        CreateSpacer(gameOverPanel.transform, 10);

        // Score card (dark card #333 with rounded corners)
        GameObject card = new GameObject("ScoreCard");
        card.transform.SetParent(gameOverPanel.transform, false);
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(600, 280);
        LayoutElement cardLE = card.AddComponent<LayoutElement>();
        cardLE.preferredHeight = 280;

        Image cardBgImg = card.AddComponent<Image>();
        cardBgImg.color = cardBg;

        VerticalLayoutGroup cardVlg = card.AddComponent<VerticalLayoutGroup>();
        cardVlg.childAlignment = TextAnchor.MiddleCenter;
        cardVlg.spacing = 10;
        cardVlg.padding = new RectOffset(30, 30, 30, 30);
        cardVlg.childControlWidth = true;
        cardVlg.childControlHeight = false;

        // "Pontuação" label
        CreateText(card.transform, "ScoreLabel", "Pontuação", 36, Color.white);

        // Score value (gold, large)
        goScoreText = CreateText(card.transform, "ScoreValue", "0", 80, goldColor);
        goScoreText.fontStyle = FontStyles.Bold;

        // Divider
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(card.transform, false);
        RectTransform divRect = divider.AddComponent<RectTransform>();
        divRect.sizeDelta = new Vector2(500, 2);
        LayoutElement divLE = divider.AddComponent<LayoutElement>();
        divLE.preferredHeight = 2;
        Image divImg = divider.AddComponent<Image>();
        divImg.color = new Color(0.333f, 0.333f, 0.333f, 1f); // #555

        // High score
        goHighScoreText = CreateText(card.transform, "HighScoreValue", "Melhor: 0", 30, new Color(0.667f, 0.667f, 0.667f, 1f));

        CreateSpacer(gameOverPanel.transform, 20);

        // Buttons row
        GameObject btnRow = new GameObject("ButtonRow");
        btnRow.transform.SetParent(gameOverPanel.transform, false);
        RectTransform btnRowRect = btnRow.AddComponent<RectTransform>();
        btnRowRect.sizeDelta = new Vector2(700, 100);
        LayoutElement btnRowLE = btnRow.AddComponent<LayoutElement>();
        btnRowLE.preferredHeight = 100;

        HorizontalLayoutGroup hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 30;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;

        // "TENTAR DE NOVO" button (green)
        CreateActionButton(btnRow.transform, "TENTAR DE NOVO", greenBtn, 330, () =>
        {
            RestartGame();
            ShowScreen(GameScreen.Playing);
        });

        // "MENU" button (gray)
        CreateActionButton(btnRow.transform, "MENU", grayBtn, 200, () =>
        {
            StopMusic();
            RestartGame();
            ShowScreen(GameScreen.Menu);
        });
    }

    // ========== GAME LOGIC ==========

    private void ShowScreen(GameScreen screen)
    {
        currentScreen = screen;

        menuPanel.SetActive(screen == GameScreen.Menu);
        gameUI.SetActive(screen == GameScreen.Playing || screen == GameScreen.GameOver);
        gameOverPanel.SetActive(screen == GameScreen.GameOver);

        if (screen == GameScreen.Menu)
        {
            StopMusic();
            ninjaObj.SetActive(false);
            // Update high score text on menu
            TextMeshProUGUI hsText = menuPanel.GetComponentInChildren<TextMeshProUGUI>();
        }
        else if (screen == GameScreen.Playing)
        {
            PrepareGame();
        }
        else if (screen == GameScreen.GameOver)
        {
            // Keep ninja visible but frozen
        }
    }

    private void PrepareGame()
    {
        isGameStarted = false;
        score = 0;
        UpdateScoreUI();

        // Reset ninja
        ninjaObj.SetActive(true);
        ninjaController.SetCharacter(NinjaCharacter.Naruto);
        ninjaController.ResetNinja(new Vector2(-2f, 0f));

        // Reset sprite
        if (narutoFrames != null && narutoFrames.Length > 0 && ninjaRenderer != null)
        {
            currentFrame = 0;
            frameTimer = 0f;
            ninjaRenderer.sprite = narutoFrames[0];
        }

        // Show start hint
        if (startHintText != null) startHintText.gameObject.SetActive(true);

        // Clean pipes
        foreach (var pipe in FindObjectsByType<PipeMove>(FindObjectsSortMode.None))
        {
            Destroy(pipe.gameObject);
        }

        // Start music
        if (musicSource != null && themeClip != null)
        {
            musicSource.clip = themeClip;
            musicSource.Play();
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

        // Stop music and play collision
        StopMusic();
        PlaySFX(collisionClip);

        // Check high score
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("FlappyNinjaHighScore", highScore);
            PlayerPrefs.Save();
        }

        // Update game over UI
        if (goScoreText != null) goScoreText.text = score.ToString();
        if (goHighScoreText != null) goHighScoreText.text = "Melhor: " + highScore;

        ShowScreen(GameScreen.GameOver);
    }

    private void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
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

    // Public method for score - called by NinjaBirdController via FindAnyObjectByType
    public void AddScore()
    {
        if (!isGameStarted) return;
        score++;
        UpdateScoreUI();
        PlaySFX(scoreClip);
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = score.ToString();
    }

    // ========== UI HELPERS ==========

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
        rect.sizeDelta = new Vector2(900, fontSize + 30);
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize + 30;

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
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

    private void CreateStyledButton(Transform parent, string label, Color bgColor, Color shadowColor, System.Action onClick)
    {
        // Button with shadow effect (PWA style: box-shadow bottom)
        GameObject btnContainer = new GameObject("BtnContainer_" + label.Replace(" ", ""));
        btnContainer.transform.SetParent(parent, false);
        RectTransform containerRect = btnContainer.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(600, 110);
        LayoutElement containerLE = btnContainer.AddComponent<LayoutElement>();
        containerLE.preferredHeight = 110;

        // Shadow (offset below)
        GameObject shadow = new GameObject("Shadow");
        shadow.transform.SetParent(btnContainer.transform, false);
        RectTransform shadowRect = shadow.AddComponent<RectTransform>();
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.offsetMin = new Vector2(0, -8);
        shadowRect.offsetMax = new Vector2(0, -8);
        Image shadowImg = shadow.AddComponent<Image>();
        shadowImg.color = shadowColor;

        // Main button
        GameObject btnObj = new GameObject("Btn_" + label.Replace(" ", ""));
        btnObj.transform.SetParent(btnContainer.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = Vector2.zero;
        btnRect.anchorMax = Vector2.one;
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = bgColor;

        // Black border effect via outline
        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3, 3);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick());

        // Label
        TextMeshProUGUI text = CreateChildText(btnObj.transform, label, 36, Color.white);
        text.fontStyle = FontStyles.Bold;
    }

    private void CreateActionButton(Transform parent, string label, Color bgColor, float width, System.Action onClick)
    {
        GameObject btnObj = new GameObject("Btn_" + label.Replace(" ", ""));
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, 90);

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = 90;

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick());

        TextMeshProUGUI text = CreateChildText(btnObj.transform, label, 30, Color.white);
        text.fontStyle = FontStyles.Bold;
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

    // ========== SPRITE CREATION ==========

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

// WillAppsUIGradient is defined in MainMenuSetup.cs — reused here for sky gradient
