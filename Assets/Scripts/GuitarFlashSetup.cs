using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Attach to Main Camera in GuitarFlash scene.
/// Creates the entire Guitar Flash rhythm game programmatically.
/// Screens: Song Select → Difficulty → Gameplay → Results.
/// </summary>
public class GuitarFlashSetup : MonoBehaviour
{
    // Neon lane colors
    private readonly Color[] laneColors = new Color[]
    {
        new Color(1f, 0.2f, 0.2f, 1f),    // Red
        new Color(0.2f, 0.6f, 1f, 1f),     // Blue
        new Color(0.2f, 1f, 0.4f, 1f),     // Green
        new Color(1f, 0.9f, 0.2f, 1f)      // Yellow
    };
    private readonly Color bgColor = new Color(0.06f, 0.06f, 0.12f, 1f);  // Very dark blue
    private readonly Color panelColor = new Color(0.08f, 0.08f, 0.16f, 1f);
    private readonly Color hitZoneColor = new Color(1f, 1f, 1f, 0.15f);

    // Game constants
    private const float NOTE_SPEED = 8f;
    private const float HIT_ZONE_Y = -3.5f;
    private const float SPAWN_Y = 6f;
    private const float LANE_WIDTH = 1.2f;
    private const float LANE_START_X = -1.8f; // Center 4 lanes

    // Timing
    private const float MISS_THRESHOLD = 0.2f; // seconds past hit zone = miss

    // State
    private enum GameScreen { SongSelect, DifficultySelect, Playing, Results }
    private GameScreen currentScreen = GameScreen.SongSelect;
    private SongDatabase.SongData selectedSong;
    private int selectedDifficulty = 0; // 0=Easy, 1=Medium, 2=Hard

    // Gameplay state
    private List<SongDatabase.NoteData> beatmap;
    private int nextNoteIndex = 0;
    private float songTime = 0f;
    private float noteSpawnLeadTime;
    private bool isPlaying = false;
    private RhythmScoreManager scoreManager;
    private List<NoteController> activeNotes = new List<NoteController>();

    // Audio
    private AudioSource audioSource;
    private bool hasAudio = false;

    // Discovered songs
    private SongDatabase.SongData[] discoveredSongs;

    // UI panels
    private GameObject songSelectPanel;
    private GameObject difficultyPanel;
    private GameObject gameplayUI;
    private GameObject resultsPanel;
    private Canvas mainCanvas;

    // Gameplay UI refs
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI comboText;
    private TextMeshProUGUI multiplierText;
    private TextMeshProUGUI hitFeedbackText;
    private TextMeshProUGUI songProgressText;
    private float hitFeedbackTimer;

    // Hit zone buttons (world space)
    private GameObject[] hitZoneButtons = new GameObject[4];

    // Sprites
    private Sprite squareSprite;
    private Sprite circleSprite;

    // ========== LIFECYCLE ==========

    private void Start()
    {
        scoreManager = new RhythmScoreManager();
        squareSprite = CreateSquareSprite();
        circleSprite = CreateCircleSprite();

        // Auto-discover songs from Resources
        discoveredSongs = SongDatabase.DiscoverSongs();

        SetupCamera();
        SetupAudio();
        CreateUI();
        ShowScreen(GameScreen.SongSelect);
    }

    private void Update()
    {
        if (currentScreen != GameScreen.Playing || !isPlaying) return;

        songTime += Time.deltaTime;

        // Spawn upcoming notes
        SpawnNotes();

        // Check for missed notes (passed the hit zone)
        CheckMissedNotes();

        // Handle input
        HandleInput();

        // Update HUD
        UpdateHUD();

        // Check if song is over
        if (songTime >= selectedSong.duration - 2f && activeNotes.Count == 0 && nextNoteIndex >= beatmap.Count)
        {
            EndSong();
        }

        // Hit feedback fade
        if (hitFeedbackTimer > 0)
        {
            hitFeedbackTimer -= Time.deltaTime;
            if (hitFeedbackTimer <= 0 && hitFeedbackText != null)
            {
                hitFeedbackText.text = "";
            }
        }
    }

    // ========== SETUP ==========

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        cam.backgroundColor = bgColor;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
    }

    private void SetupAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    // ========== SCREENS ==========

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
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        CreateSongSelectPanel(canvasObj.transform);
        CreateDifficultyPanel(canvasObj.transform);
        CreateGameplayUI(canvasObj.transform);
        CreateResultsPanel(canvasObj.transform);
    }

    private void ShowScreen(GameScreen screen)
    {
        currentScreen = screen;
        songSelectPanel.SetActive(screen == GameScreen.SongSelect);
        difficultyPanel.SetActive(screen == GameScreen.DifficultySelect);
        gameplayUI.SetActive(screen == GameScreen.Playing);
        resultsPanel.SetActive(screen == GameScreen.Results);

        // Show/hide hit zone buttons
        foreach (var btn in hitZoneButtons)
        {
            if (btn != null) btn.SetActive(screen == GameScreen.Playing);
        }
    }

    // --- Song Select ---
    private void CreateSongSelectPanel(Transform parent)
    {
        songSelectPanel = CreateFullOverlay(parent, "SongSelectPanel", bgColor);

        VerticalLayoutGroup vlg = songSelectPanel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 25;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.padding = new RectOffset(60, 60, 80, 60);

        // Header
        TextMeshProUGUI title = CreateText(songSelectPanel.transform, "Title", "\ud83c\udfb8 GUITAR FLASH", 56, new Color(1f, 0.3f, 0.5f, 1f));
        title.fontStyle = FontStyles.Bold;

        CreateText(songSelectPanel.transform, "Subtitle", "Escolha uma música", 30, new Color(1f, 1f, 1f, 0.5f));

        CreateSpacer(songSelectPanel.transform, 30);

        // Song cards
        foreach (var song in discoveredSongs)
        {
            CreateSongCard(songSelectPanel.transform, song);
        }

        CreateSpacer(songSelectPanel.transform, 40);

        // Back button
        CreateActionButton(songSelectPanel.transform, "\u2190  VOLTAR", new Color(0.2f, 0.2f, 0.2f, 1f), () =>
        {
            SceneManager.LoadScene("Play");
        });
    }

    private void CreateSongCard(Transform parent, SongDatabase.SongData song)
    {
        GameObject card = new GameObject("SongCard_" + song.id);
        card.transform.SetParent(parent, false);
        RectTransform rect = card.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900, 200);

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.preferredHeight = 200;

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = panelColor;

        Button btn = card.AddComponent<Button>();
        SongDatabase.SongData captured = song;
        btn.onClick.AddListener(() =>
        {
            selectedSong = captured;
            ShowScreen(GameScreen.DifficultySelect);
        });

        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;

        // Layout
        VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 8;
        vlg.padding = new RectOffset(30, 30, 20, 20);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Music note emoji
        CreateText(card.transform, "Icon", "\ud83c\udfb5", 40, song.accentColor);

        // Title
        TextMeshProUGUI titleText = CreateText(card.transform, "SongTitle", song.title, 36, Color.white);
        titleText.fontStyle = FontStyles.Bold;

        // Artist
        CreateText(card.transform, "Artist", song.artist, 26, new Color(1f, 1f, 1f, 0.6f));

        // Duration
        int mins = (int)(song.duration / 60);
        int secs = (int)(song.duration % 60);
        string metaStr = song.bpm > 0 ? $"{song.bpm} BPM  •  {mins}:{secs:D2}" : $"{mins}:{secs:D2}  •  FFT Auto-detect";
        CreateText(card.transform, "Meta", metaStr, 22, new Color(1f, 1f, 1f, 0.4f));

        // Accent border (left)
        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = song.accentColor;
        outline.effectDistance = new Vector2(3, 0);
    }

    // --- Difficulty Select ---
    private void CreateDifficultyPanel(Transform parent)
    {
        difficultyPanel = CreateFullOverlay(parent, "DifficultyPanel", bgColor);

        VerticalLayoutGroup vlg = difficultyPanel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 30;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.padding = new RectOffset(80, 80, 0, 0);

        TextMeshProUGUI title = CreateText(difficultyPanel.transform, "Title", "DIFICULDADE", 52, Color.white);
        title.fontStyle = FontStyles.Bold;

        CreateSpacer(difficultyPanel.transform, 20);

        // Easy
        CreateActionButton(difficultyPanel.transform, "\u2b50  FÁCIL", new Color(0.2f, 0.8f, 0.4f, 1f), () =>
        {
            selectedDifficulty = 0;
            StartGame();
        });

        // Medium
        CreateActionButton(difficultyPanel.transform, "\u2b50\u2b50  MÉDIO", new Color(1f, 0.6f, 0.2f, 1f), () =>
        {
            selectedDifficulty = 1;
            StartGame();
        });

        // Hard
        CreateActionButton(difficultyPanel.transform, "\u2b50\u2b50\u2b50  DIFÍCIL", new Color(1f, 0.2f, 0.3f, 1f), () =>
        {
            selectedDifficulty = 2;
            StartGame();
        });

        CreateSpacer(difficultyPanel.transform, 30);

        CreateActionButton(difficultyPanel.transform, "VOLTAR", new Color(0.2f, 0.2f, 0.2f, 1f), () =>
        {
            ShowScreen(GameScreen.SongSelect);
        });
    }

    // --- Gameplay UI ---
    private void CreateGameplayUI(Transform parent)
    {
        gameplayUI = new GameObject("GameplayUI");
        gameplayUI.transform.SetParent(parent, false);
        RectTransform guiRect = gameplayUI.AddComponent<RectTransform>();
        guiRect.anchorMin = Vector2.zero;
        guiRect.anchorMax = Vector2.one;
        guiRect.offsetMin = Vector2.zero;
        guiRect.offsetMax = Vector2.zero;

        // Score (top-right)
        scoreText = CreateAnchoredText(gameplayUI.transform, "Score", "0",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -40),
            70, Color.white, TextAlignmentOptions.TopRight);
        scoreText.fontStyle = FontStyles.Bold;

        // Combo (top-center)
        comboText = CreateAnchoredText(gameplayUI.transform, "Combo", "",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40),
            42, new Color(1f, 0.9f, 0.2f, 1f), TextAlignmentOptions.Top);
        comboText.fontStyle = FontStyles.Bold;

        // Multiplier (below combo)
        multiplierText = CreateAnchoredText(gameplayUI.transform, "Multiplier", "1x",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -90),
            30, new Color(1f, 1f, 1f, 0.5f), TextAlignmentOptions.Top);

        // Hit feedback (center)
        hitFeedbackText = CreateAnchoredText(gameplayUI.transform, "HitFeedback", "",
            new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), Vector2.zero,
            50, Color.white, TextAlignmentOptions.Center);
        hitFeedbackText.fontStyle = FontStyles.Bold;

        // Song progress (top-left)
        songProgressText = CreateAnchoredText(gameplayUI.transform, "Progress", "0:00",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -40),
            28, new Color(1f, 1f, 1f, 0.4f), TextAlignmentOptions.TopLeft);

        // Lane backgrounds (world space, created separately)
        CreateLaneVisuals();

        // Hit zone buttons (touch areas at bottom of screen)
        CreateHitZoneUI(gameplayUI.transform);

        // Back button
        GameObject backBtn = CreateSmallButton(gameplayUI.transform, "\u2190",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -100),
            () => { StopGame(); ShowScreen(GameScreen.SongSelect); });
    }

    private void CreateLaneVisuals()
    {
        for (int i = 0; i < 4; i++)
        {
            float x = LANE_START_X + i * LANE_WIDTH;

            // Lane line (thin vertical strip)
            GameObject laneLine = new GameObject("Lane_" + i);
            laneLine.transform.position = new Vector3(x, 0, 1f);
            SpriteRenderer lineSr = laneLine.AddComponent<SpriteRenderer>();
            lineSr.sprite = squareSprite;
            Color lineColor = laneColors[i];
            lineColor.a = 0.08f;
            lineSr.color = lineColor;
            laneLine.transform.localScale = new Vector3(0.9f, 12f, 1f);
            lineSr.sortingOrder = -2;

            // Hit zone indicator (bright line at hit zone Y)
            GameObject hitIndicator = new GameObject("HitIndicator_" + i);
            hitIndicator.transform.position = new Vector3(x, HIT_ZONE_Y, 0);
            SpriteRenderer hitSr = hitIndicator.AddComponent<SpriteRenderer>();
            hitSr.sprite = squareSprite;
            Color hitColor = laneColors[i];
            hitColor.a = 0.4f;
            hitSr.color = hitColor;
            hitIndicator.transform.localScale = new Vector3(1f, 0.15f, 1f);
            hitSr.sortingOrder = 2;
        }

        // Hit zone line background
        GameObject hitZoneBg = new GameObject("HitZoneBg");
        hitZoneBg.transform.position = new Vector3(0, HIT_ZONE_Y, 1f);
        SpriteRenderer hzSr = hitZoneBg.AddComponent<SpriteRenderer>();
        hzSr.sprite = squareSprite;
        hzSr.color = hitZoneColor;
        hitZoneBg.transform.localScale = new Vector3(6f, 0.3f, 1f);
        hzSr.sortingOrder = 1;
    }

    private void CreateHitZoneUI(Transform parent)
    {
        // Create 4 touch buttons at bottom of screen
        for (int i = 0; i < 4; i++)
        {
            int laneIndex = i; // Capture for closure
            float xOffset = -300 + i * 200;

            GameObject btnObj = new GameObject("HitBtn_" + i);
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(xOffset, 40);
            rect.sizeDelta = new Vector2(180, 180);

            Image img = btnObj.AddComponent<Image>();
            Color btnColor = laneColors[i];
            btnColor.a = 0.3f;
            img.color = btnColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.pressedColor = new Color(2f, 2f, 2f, 1f); // Bright flash on press
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => OnLaneHit(laneIndex));

            // Label
            string[] labels = { "←", "↓", "↑", "→" };
            TextMeshProUGUI labelText = CreateChildText(btnObj.transform, labels[i], 50, Color.white);

            hitZoneButtons[i] = btnObj;
        }
    }

    // --- Results Panel ---
    private void CreateResultsPanel(Transform parent)
    {
        resultsPanel = CreateFullOverlay(parent, "ResultsPanel", new Color(0, 0, 0, 0.95f));

        VerticalLayoutGroup vlg = resultsPanel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 20;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.padding = new RectOffset(80, 80, 0, 0);

        CreateText(resultsPanel.transform, "ResultsTitle", "RESULTADOS", 50, Color.white).fontStyle = FontStyles.Bold;
        CreateSpacer(resultsPanel.transform, 10);

        // These get populated dynamically in ShowResults()
        CreateText(resultsPanel.transform, "RankLabel", "", 120, Color.white);
        CreateText(resultsPanel.transform, "FinalScore", "", 40, Color.white);
        CreateText(resultsPanel.transform, "Accuracy", "", 32, new Color(1f, 1f, 1f, 0.7f));
        CreateText(resultsPanel.transform, "MaxCombo", "", 32, new Color(1f, 1f, 1f, 0.7f));
        CreateSpacer(resultsPanel.transform, 5);
        CreateText(resultsPanel.transform, "PerfectCount", "", 26, new Color(1f, 0.9f, 0.2f, 0.8f));
        CreateText(resultsPanel.transform, "GreatCount", "", 26, new Color(0.2f, 1f, 0.4f, 0.8f));
        CreateText(resultsPanel.transform, "GoodCount", "", 26, new Color(0.2f, 0.6f, 1f, 0.8f));
        CreateText(resultsPanel.transform, "MissCount", "", 26, new Color(1f, 0.3f, 0.3f, 0.8f));

        CreateSpacer(resultsPanel.transform, 20);

        CreateActionButton(resultsPanel.transform, "Jogar Novamente", new Color(0.2f, 0.6f, 1f, 1f), () =>
        {
            StartGame();
        });

        CreateActionButton(resultsPanel.transform, "Outra Dificuldade", new Color(0.4f, 0.4f, 0.4f, 1f), () =>
        {
            ShowScreen(GameScreen.DifficultySelect);
        });

        CreateActionButton(resultsPanel.transform, "Voltar", new Color(0.2f, 0.2f, 0.2f, 1f), () =>
        {
            ShowScreen(GameScreen.SongSelect);
        });
    }

    // ========== GAME LOGIC ==========

    private void StartGame()
    {
        nextNoteIndex = 0;
        songTime = 0f;
        activeNotes.Clear();
        scoreManager.Reset();

        // Calculate lead time: how early to spawn notes
        float travelDistance = SPAWN_Y - HIT_ZONE_Y;
        noteSpawnLeadTime = travelDistance / NOTE_SPEED;

        // Try to load audio
        AudioClip clip = null;
        if (!string.IsNullOrEmpty(selectedSong.audioResource))
        {
            clip = Resources.Load<AudioClip>(selectedSong.audioResource);
        }

        if (clip != null)
        {
            audioSource.clip = clip;
            hasAudio = true;
        }
        else
        {
            hasAudio = false;
            Debug.Log("Audio not found at: " + selectedSong.audioResource + ". Playing without music.");
        }

        // Generate beatmap — uses FFT if audio clip is available
        beatmap = SongDatabase.GenerateBeatmap(selectedSong, selectedDifficulty, clip);
        Debug.Log($"Beatmap generated: {beatmap.Count} notes for '{selectedSong.title}' (difficulty {selectedDifficulty})");

        isPlaying = true;
        if (hasAudio) audioSource.Play();

        // Clean any leftover notes
        foreach (var note in FindObjectsByType<NoteController>(FindObjectsSortMode.None))
        {
            Destroy(note.gameObject);
        }

        ShowScreen(GameScreen.Playing);
    }

    private void StopGame()
    {
        isPlaying = false;
        if (audioSource.isPlaying) audioSource.Stop();

        // Clean notes
        foreach (var note in FindObjectsByType<NoteController>(FindObjectsSortMode.None))
        {
            Destroy(note.gameObject);
        }
        activeNotes.Clear();
    }

    private void SpawnNotes()
    {
        while (nextNoteIndex < beatmap.Count)
        {
            SongDatabase.NoteData noteData = beatmap[nextNoteIndex];
            float spawnTime = noteData.time - noteSpawnLeadTime;

            if (songTime >= spawnTime)
            {
                SpawnNote(noteData);
                nextNoteIndex++;
            }
            else
            {
                break;
            }
        }
    }

    private void SpawnNote(SongDatabase.NoteData noteData)
    {
        int lane = Mathf.Clamp(noteData.lane, 0, 3);
        float x = LANE_START_X + lane * LANE_WIDTH;

        GameObject noteObj = new GameObject("Note_" + noteData.time.ToString("F2"));
        noteObj.transform.position = new Vector3(x, SPAWN_Y, 0);

        SpriteRenderer sr = noteObj.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = laneColors[lane];
        sr.sortingOrder = 5;
        noteObj.transform.localScale = new Vector3(0.6f, 0.25f, 1f);

        NoteController nc = noteObj.AddComponent<NoteController>();
        nc.speed = NOTE_SPEED;
        nc.targetTime = noteData.time;
        nc.lane = lane;

        activeNotes.Add(nc);
    }

    private void CheckMissedNotes()
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            NoteController note = activeNotes[i];
            if (note == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }

            if (note.isHit || note.isMissed)
            {
                activeNotes.RemoveAt(i);
                continue;
            }

            // Check if note passed the hit zone by too much
            float timeDiff = songTime - note.targetTime;
            if (timeDiff > MISS_THRESHOLD)
            {
                note.OnMissed();
                scoreManager.RegisterMiss();
                ShowHitFeedback(RhythmScoreManager.HitQuality.Miss);
                activeNotes.RemoveAt(i);
            }
        }
    }

    private void HandleInput()
    {
        // Keyboard input (New Input System)
        if (Keyboard.current == null) return;

        if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame) OnLaneHit(0);
        if (Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame) OnLaneHit(1);
        if (Keyboard.current.jKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame) OnLaneHit(2);
        if (Keyboard.current.kKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame) OnLaneHit(3);
    }

    private void OnLaneHit(int lane)
    {
        if (!isPlaying) return;

        // Find the closest note in this lane
        NoteController closest = null;
        float closestDiff = float.MaxValue;

        foreach (var note in activeNotes)
        {
            if (note == null || note.isHit || note.isMissed || note.lane != lane) continue;

            float diff = Mathf.Abs(songTime - note.targetTime);
            if (diff < closestDiff && diff <= RhythmScoreManager.GOOD_WINDOW)
            {
                closest = note;
                closestDiff = diff;
            }
        }

        if (closest != null)
        {
            float timeDiff = songTime - closest.targetTime;
            RhythmScoreManager.HitQuality quality = scoreManager.EvaluateHit(timeDiff);
            closest.OnHit(GetQualityColor(quality));
            ShowHitFeedback(quality);

            // Flash the hit zone button
            FlashHitButton(lane, GetQualityColor(quality));
        }
    }

    private void ShowHitFeedback(RhythmScoreManager.HitQuality quality)
    {
        if (hitFeedbackText == null) return;

        switch (quality)
        {
            case RhythmScoreManager.HitQuality.Perfect:
                hitFeedbackText.text = "PERFECT!";
                hitFeedbackText.color = new Color(1f, 0.9f, 0.2f, 1f);
                break;
            case RhythmScoreManager.HitQuality.Great:
                hitFeedbackText.text = "GREAT!";
                hitFeedbackText.color = new Color(0.2f, 1f, 0.4f, 1f);
                break;
            case RhythmScoreManager.HitQuality.Good:
                hitFeedbackText.text = "GOOD";
                hitFeedbackText.color = new Color(0.2f, 0.6f, 1f, 1f);
                break;
            case RhythmScoreManager.HitQuality.Miss:
                hitFeedbackText.text = "MISS";
                hitFeedbackText.color = new Color(1f, 0.3f, 0.3f, 0.8f);
                break;
        }

        hitFeedbackTimer = 0.5f;
    }

    private Color GetQualityColor(RhythmScoreManager.HitQuality quality)
    {
        switch (quality)
        {
            case RhythmScoreManager.HitQuality.Perfect: return new Color(1f, 0.9f, 0.2f, 1f);
            case RhythmScoreManager.HitQuality.Great: return new Color(0.2f, 1f, 0.4f, 1f);
            case RhythmScoreManager.HitQuality.Good: return new Color(0.2f, 0.6f, 1f, 1f);
            default: return new Color(1f, 0.3f, 0.3f, 0.8f);
        }
    }

    private void FlashHitButton(int lane, Color color)
    {
        if (lane < 0 || lane >= 4 || hitZoneButtons[lane] == null) return;
        Image img = hitZoneButtons[lane].GetComponent<Image>();
        if (img != null) img.color = color;

        // Reset after short delay
        StartCoroutine(ResetButtonColor(lane));
    }

    private System.Collections.IEnumerator ResetButtonColor(int lane)
    {
        yield return new WaitForSeconds(0.1f);
        if (hitZoneButtons[lane] != null)
        {
            Image img = hitZoneButtons[lane].GetComponent<Image>();
            if (img != null)
            {
                Color c = laneColors[lane];
                c.a = 0.3f;
                img.color = c;
            }
        }
    }

    private void UpdateHUD()
    {
        if (scoreText != null) scoreText.text = scoreManager.Score.ToString("N0");

        if (comboText != null)
        {
            if (scoreManager.Combo >= 3)
                comboText.text = scoreManager.Combo + " COMBO";
            else
                comboText.text = "";
        }

        if (multiplierText != null)
        {
            if (scoreManager.Multiplier > 1)
                multiplierText.text = scoreManager.Multiplier + "x";
            else
                multiplierText.text = "";
        }

        if (songProgressText != null)
        {
            int mins = (int)(songTime / 60);
            int secs = (int)(songTime % 60);
            songProgressText.text = $"{mins}:{secs:D2}";
        }
    }

    private void EndSong()
    {
        isPlaying = false;
        if (audioSource.isPlaying) audioSource.Stop();
        ShowResults();
    }

    private void ShowResults()
    {
        ShowScreen(GameScreen.Results);

        // Populate results text
        var texts = resultsPanel.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var t in texts)
        {
            switch (t.gameObject.name)
            {
                case "RankLabel":
                    t.text = scoreManager.GetRankString();
                    t.color = scoreManager.GetRankColor();
                    break;
                case "FinalScore":
                    t.text = "Score: " + scoreManager.Score.ToString("N0");
                    break;
                case "Accuracy":
                    t.text = $"Accuracy: {scoreManager.GetAccuracy():F1}%";
                    break;
                case "MaxCombo":
                    t.text = $"Max Combo: {scoreManager.MaxCombo}";
                    break;
                case "PerfectCount":
                    t.text = $"Perfect: {scoreManager.PerfectCount}";
                    break;
                case "GreatCount":
                    t.text = $"Great: {scoreManager.GreatCount}";
                    break;
                case "GoodCount":
                    t.text = $"Good: {scoreManager.GoodCount}";
                    break;
                case "MissCount":
                    t.text = $"Miss: {scoreManager.MissCount}";
                    break;
            }
        }
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
        return text;
    }

    private TextMeshProUGUI CreateAnchoredText(Transform parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
        int fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(500, fontSize + 20);

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
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

        CreateChildText(btnObj.transform, label, 34, Color.white).fontStyle = FontStyles.Bold;
    }

    private GameObject CreateSmallButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pos, System.Action onClick)
    {
        GameObject btnObj = new GameObject("SmallBtn");
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(80, 80);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.5f);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());

        CreateChildText(btnObj.transform, label, 40, Color.white);
        return btnObj;
    }

    private void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(parent, false);
        spacer.AddComponent<RectTransform>().sizeDelta = new Vector2(0, height);
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
