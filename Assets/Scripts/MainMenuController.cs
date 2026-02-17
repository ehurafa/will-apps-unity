using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    public Button videoPlayerButton;
    public Button flappyBirdButton;
    public Button ticTacToeButton;

    private void Start()
    {
        // Ensure buttons are assigned, then add listeners
        if (videoPlayerButton) videoPlayerButton.onClick.AddListener(() => LoadScene("VideoPlayer"));
        if (flappyBirdButton) flappyBirdButton.onClick.AddListener(() => LoadScene("FlappyBird"));
        if (ticTacToeButton) ticTacToeButton.onClick.AddListener(() => LoadScene("TicTacToe"));
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
