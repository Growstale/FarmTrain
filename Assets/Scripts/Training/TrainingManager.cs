using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using UnityEngine.Audio;

[RequireComponent(typeof(VideoPlayer))]
public class TrainingVideoManager : MonoBehaviour, IUIManageable
{
    public AudioSource audioSource;        // Источник звука
    public AudioClip closeSound;           // Звук закрытия
    public static TrainingVideoManager Instance { get; private set; }

    [Header("References")]
    public GameObject trainingPanel;
    public VideoPlayer videoPlayer;
    public Button backButton;
    public Button forwardButton;
    public GameObject notificationIcon;
    public TextMeshProUGUI titleText;

    [Header("Clips")]
    public VideoClip[] clips;

    private int currentIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (ExclusiveUIManager.Instance != null)
        {
            ExclusiveUIManager.Instance.Register(this);
        }

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
       
        if (titleText != null)
        {
            titleText.text = videoPlayer.clip.name;
        }


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
        ExclusiveUIManager.Instance.NotifyPanelOpening(this);
        GameStateManager.Instance.RequestPause(this);
        if (trainingPanel != null)
            trainingPanel.SetActive(true);

        if (notificationIcon != null)
            notificationIcon.SetActive(false);

        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("[TrainingManager] Клипики не заданы");
            return;
        }

        PlayClip(0);
    }

    public void Close()
    {
        if (audioSource != null && closeSound != null)
        {
            audioSource.PlayOneShot(closeSound);
        }
        GameStateManager.Instance.RequestResume(this);
        videoPlayer.Stop();
        if (titleText != null)
        {
            titleText.text = "";
        }

        if (trainingPanel != null)
            trainingPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (ExclusiveUIManager.Instance != null)
        {
            ExclusiveUIManager.Instance.Deregister(this);
        }
    }

    public void CloseUI()
    {
        // Наша система будет вызывать этот метод, чтобы закрыть панель
        Close();
    }

    public bool IsOpen()
    {
        // Наша система должна знать, открыта ли панель
        return trainingPanel != null && trainingPanel.activeSelf;
    }

}