using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Attach this script to the Main Camera in the TicTacToe scene.
/// It creates the entire Tic-Tac-Toe game programmatically: board, AI, UI.
/// </summary>
public class TicTacToeSetup : MonoBehaviour
{
    // Colors
    private readonly Color bgColor = new Color(0.1f, 0.1f, 0.18f, 1f);        // #1A1A2E
    private readonly Color cardColor = new Color(0.086f, 0.129f, 0.243f, 1f);  // #16213E
    private readonly Color playerColor = new Color(0.306f, 0.804f, 0.769f, 1f); // #4ECDC4
    private readonly Color aiColor = new Color(1f, 0.42f, 0.42f, 1f);         // #FF6B6B
    private readonly Color accentColor = new Color(1f, 0.902f, 0.427f, 1f);   // #FFE66D
    private readonly Color textSecColor = new Color(0.659f, 0.855f, 0.863f, 1f);

    // Game state
    private string[] board = new string[9]; // "X", "O", or null
    private bool isPlayerTurn = true;
    private string winner = null; // "X", "O", "draw", or null
    private int playerScore = 0;
    private int aiScore = 0;

    // Winning combinations
    private readonly int[][] winCombos = new int[][]
    {
        new[] {0,1,2}, new[] {3,4,5}, new[] {6,7,8}, // rows
        new[] {0,3,6}, new[] {1,4,7}, new[] {2,5,8}, // columns
        new[] {0,4,8}, new[] {2,4,6}                   // diagonals
    };

    // UI references
    private Button[] cellButtons = new Button[9];
    private TextMeshProUGUI[] cellTexts = new TextMeshProUGUI[9];
    private Image[] cellImages = new Image[9];
    private TextMeshProUGUI playerScoreText;
    private TextMeshProUGUI aiScoreText;
    private TextMeshProUGUI statusText;
    private GameObject resultPanel;
    private TextMeshProUGUI resultText;

    private void Start()
    {
        Camera.main.backgroundColor = bgColor;
        Camera.main.orthographic = true;
        CreateUI();
        ResetGame();
    }

    private void CreateUI()
    {
        // --- Canvas ---
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

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

        // --- Background ---
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = bgColor;

        // --- Main Layout ---
        GameObject content = new GameObject("Content");
        content.transform.SetParent(canvasObj.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 30;
        vlg.padding = new RectOffset(40, 40, 60, 40);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;

        // --- Header ---
        CreateHeader(content.transform);

        // --- Scoreboard ---
        CreateScoreboard(content.transform);

        // --- Status Text ---
        statusText = CreateText(content.transform, "StatusText", "Sua vez!", 36, accentColor);

        // --- Spacer ---
        CreateSpacer(content.transform, 20);

        // --- Game Board ---
        CreateBoard(content.transform);

        // --- Spacer ---
        CreateSpacer(content.transform, 30);

        // --- Reset Scores Button ---
        CreateActionButton(content.transform, "Resetar Placar", cardColor, () => ResetScores());

        // --- Result Panel (overlay) ---
        CreateResultPanel(canvasObj.transform);
    }

    private void CreateHeader(Transform parent)
    {
        GameObject header = new GameObject("Header");
        header.transform.SetParent(parent, false);
        RectTransform rect = header.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1000, 100);

        HorizontalLayoutGroup hlg = header.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 20;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;

        // Back button
        GameObject backBtn = new GameObject("Btn_Back");
        backBtn.transform.SetParent(header.transform, false);
        RectTransform backRect = backBtn.AddComponent<RectTransform>();
        backRect.sizeDelta = new Vector2(80, 80);

        LayoutElement backLE = backBtn.AddComponent<LayoutElement>();
        backLE.preferredWidth = 80;

        Image backImg = backBtn.AddComponent<Image>();
        backImg.color = cardColor;

        Button btn = backBtn.AddComponent<Button>();
        btn.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

        TextMeshProUGUI backText = CreateChildText(backBtn.transform, "←", 40, Color.white);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(header.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(600, 80);
        LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
        titleLE.preferredWidth = 600;

        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "Jogo da Velha";
        title.fontSize = 48;
        title.fontStyle = FontStyles.Bold;
        title.color = Color.white;
        title.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void CreateScoreboard(Transform parent)
    {
        GameObject scoreboard = new GameObject("Scoreboard");
        scoreboard.transform.SetParent(parent, false);
        RectTransform rect = scoreboard.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900, 120);

        HorizontalLayoutGroup hlg = scoreboard.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 60;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;

        // Player score
        GameObject playerPanel = CreateScorePanel(scoreboard.transform, "Você (X)", playerColor);
        playerScoreText = playerPanel.GetComponentInChildren<TextMeshProUGUI>();

        // VS
        TextMeshProUGUI vsText = CreateText(scoreboard.transform, "VS", "VS", 32, textSecColor);
        RectTransform vsRect = vsText.GetComponent<RectTransform>();
        vsRect.sizeDelta = new Vector2(80, 80);
        LayoutElement vsLE = vsText.gameObject.AddComponent<LayoutElement>();
        vsLE.preferredWidth = 80;

        // AI score
        GameObject aiPanel = CreateScorePanel(scoreboard.transform, "IA (O)", aiColor);
        aiScoreText = aiPanel.GetComponentInChildren<TextMeshProUGUI>();
    }

    private GameObject CreateScorePanel(Transform parent, string label, Color color)
    {
        GameObject panel = new GameObject("ScorePanel_" + label);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 100);

        LayoutElement le = panel.AddComponent<LayoutElement>();
        le.preferredWidth = 300;

        Image img = panel.AddComponent<Image>();
        img.color = cardColor;

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 4;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.padding = new RectOffset(10, 10, 8, 8);

        // Label
        CreateText(panel.transform, "Label", label, 24, color);

        // Score value
        TextMeshProUGUI scoreText = CreateText(panel.transform, "Score", "0", 40, Color.white);
        scoreText.fontStyle = FontStyles.Bold;

        return panel;
    }

    private void CreateBoard(Transform parent)
    {
        GameObject boardContainer = new GameObject("Board");
        boardContainer.transform.SetParent(parent, false);
        RectTransform boardRect = boardContainer.AddComponent<RectTransform>();
        boardRect.sizeDelta = new Vector2(750, 750);

        LayoutElement le = boardContainer.AddComponent<LayoutElement>();
        le.preferredWidth = 750;
        le.preferredHeight = 750;

        GridLayoutGroup grid = boardContainer.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(230, 230);
        grid.spacing = new Vector2(15, 15);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < 9; i++)
        {
            CreateCell(boardContainer.transform, i);
        }
    }

    private void CreateCell(Transform parent, int index)
    {
        GameObject cellObj = new GameObject("Cell_" + index);
        cellObj.transform.SetParent(parent, false);

        Image img = cellObj.AddComponent<Image>();
        img.color = cardColor;
        cellImages[index] = img;

        Button btn = cellObj.AddComponent<Button>();
        int capturedIndex = index;
        btn.onClick.AddListener(() => OnCellClicked(capturedIndex));
        cellButtons[index] = btn;

        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.disabledColor = Color.white;
        btn.colors = colors;

        // Cell text
        TextMeshProUGUI text = CreateChildText(cellObj.transform, "", 80, Color.white);
        text.fontStyle = FontStyles.Bold;
        cellTexts[index] = text;
    }

    private void CreateResultPanel(Transform parent)
    {
        resultPanel = new GameObject("ResultPanel");
        resultPanel.transform.SetParent(parent, false);
        RectTransform rect = resultPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = resultPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);

        VerticalLayoutGroup vlg = resultPanel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 30;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.padding = new RectOffset(100, 100, 0, 0);

        resultText = CreateText(resultPanel.transform, "ResultText", "", 56, accentColor);
        resultText.fontStyle = FontStyles.Bold;

        CreateActionButton(resultPanel.transform, "Jogar Novamente", playerColor, () => ResetGame());
        CreateActionButton(resultPanel.transform, "Menu Principal", cardColor, () => SceneManager.LoadScene("MainMenu"));

        resultPanel.SetActive(false);
    }

    // --- Game Logic ---

    private void OnCellClicked(int index)
    {
        if (board[index] != null || winner != null || !isPlayerTurn) return;

        board[index] = "X";
        cellTexts[index].text = "X";
        cellTexts[index].color = playerColor;
        cellButtons[index].interactable = false;

        winner = CheckWinner();
        if (winner != null)
        {
            EndGame();
            return;
        }

        if (IsBoardFull())
        {
            winner = "draw";
            EndGame();
            return;
        }

        isPlayerTurn = false;
        statusText.text = "IA pensando...";
        Invoke(nameof(MakeAIMove), 0.5f);
    }

    private void MakeAIMove()
    {
        int move = FindBestMove();
        if (move == -1) return;

        board[move] = "O";
        cellTexts[move].text = "O";
        cellTexts[move].color = aiColor;
        cellButtons[move].interactable = false;

        winner = CheckWinner();
        if (winner != null)
        {
            EndGame();
            return;
        }

        if (IsBoardFull())
        {
            winner = "draw";
            EndGame();
            return;
        }

        isPlayerTurn = true;
        statusText.text = "Sua vez!";
    }

    private int FindBestMove()
    {
        // 1. Try to win
        for (int i = 0; i < 9; i++)
        {
            if (board[i] == null)
            {
                board[i] = "O";
                if (CheckWinner() == "O") { board[i] = null; return i; }
                board[i] = null;
            }
        }

        // 2. Block player
        for (int i = 0; i < 9; i++)
        {
            if (board[i] == null)
            {
                board[i] = "X";
                if (CheckWinner() == "X") { board[i] = null; return i; }
                board[i] = null;
            }
        }

        // 3. Take center
        if (board[4] == null) return 4;

        // 4. Take corner
        int[] corners = { 0, 2, 6, 8 };
        System.Collections.Generic.List<int> availCorners = new();
        foreach (int c in corners) if (board[c] == null) availCorners.Add(c);
        if (availCorners.Count > 0) return availCorners[Random.Range(0, availCorners.Count)];

        // 5. Take any
        System.Collections.Generic.List<int> available = new();
        for (int i = 0; i < 9; i++) if (board[i] == null) available.Add(i);
        if (available.Count > 0) return available[Random.Range(0, available.Count)];

        return -1;
    }

    private string CheckWinner()
    {
        foreach (var combo in winCombos)
        {
            string a = board[combo[0]], b = board[combo[1]], c = board[combo[2]];
            if (a != null && a == b && a == c) return a;
        }
        return null;
    }

    private bool IsBoardFull()
    {
        foreach (var cell in board) if (cell == null) return false;
        return true;
    }

    private void EndGame()
    {
        if (winner == "X") playerScore++;
        if (winner == "O") aiScore++;
        UpdateScores();

        string msg = winner switch
        {
            "X" => "\ud83c\udf89 Você Venceu!",
            "O" => "\ud83e\udd16 IA Venceu!",
            "draw" => "Empate!",
            _ => ""
        };

        resultText.text = msg;
        resultText.color = winner == "X" ? playerColor : winner == "O" ? aiColor : accentColor;
        resultPanel.SetActive(true);
    }

    private void ResetGame()
    {
        board = new string[9];
        winner = null;
        isPlayerTurn = true;

        for (int i = 0; i < 9; i++)
        {
            cellTexts[i].text = "";
            cellButtons[i].interactable = true;
            cellImages[i].color = cardColor;
        }

        if (statusText != null) statusText.text = "Sua vez!";
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    private void ResetScores()
    {
        playerScore = 0;
        aiScore = 0;
        UpdateScores();
        ResetGame();
    }

    private void UpdateScores()
    {
        if (playerScoreText != null)
        {
            // Find the "Score" child text (second TMP in the panel)
            var texts = playerScoreText.transform.parent.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2) texts[1].text = playerScore.ToString();
        }
        if (aiScoreText != null)
        {
            var texts = aiScoreText.transform.parent.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2) texts[1].text = aiScore.ToString();
        }
    }

    // --- Helpers ---

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

        TextMeshProUGUI text = CreateChildText(btnObj.transform, label, 32, Color.white);
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
}
