using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class TrainingVideoManager : MonoBehaviour
{
    [Header("References")]
    public GameObject trainingPanel;
    public VideoPlayer videoPlayer;
    public Button backButton;
    public Button forwardButton;

    [Header("Clips")]
    public VideoClip[] clips;

    private int currentIndex = 0;

    private void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (backButton != null)
            backButton.onClick.AddListener(ShowPrev);
        if (forwardButton != null)
            forwardButton.onClick.AddListener(ShowNext);
    }

    private void ShowPrev()
    {
        if (currentIndex <= 0) return;
        PlayClip(currentIndex - 1);
    }

    private void ShowNext()
    {
        if (currentIndex >= clips.Length - 1) return;
        PlayClip(currentIndex + 1);
    }

    private void PlayClip(int index)
    {
        if (index < 0 || index >= clips.Length) return;

        currentIndex = index;

        videoPlayer.Stop();
        videoPlayer.clip = clips[currentIndex];
        videoPlayer.Play();

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        if (backButton != null)
            backButton.interactable = currentIndex > 0;

        if (forwardButton != null)
            forwardButton.interactable = currentIndex < clips.Length - 1;
    }

    public void Open()
    {
        if (trainingPanel != null)
            trainingPanel.SetActive(true);

        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("[TrainingManager] Клипики не заданы");
            return;
        }

        PlayClip(0);
    }

    public void Close()
    {
        videoPlayer.Stop();

        if (trainingPanel != null)
            trainingPanel.SetActive(false);
    }
}