using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VideoPlayerController : MonoBehaviour
{
    [Header("Components")]
    public VideoPlayer videoPlayer;
    public Button playPauseButton;
    public Button backButton;

    [Header("Settings")]
    // URL from the React Native app
    public string videoUrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"; 

    private void Start()
    {
        // Setup buttons
        if (playPauseButton) playPauseButton.onClick.AddListener(TogglePlay);
        if (backButton) backButton.onClick.AddListener(GoBack);

        // Setup Video Player
        if (videoPlayer)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.CameraNearPlane; // Render on camera
            videoPlayer.url = videoUrl;
            videoPlayer.Play();
        }
    }

    private void TogglePlay()
    {
        if (videoPlayer == null) return;

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
        else
        {
            videoPlayer.Play();
        }
    }

    private void GoBack()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
