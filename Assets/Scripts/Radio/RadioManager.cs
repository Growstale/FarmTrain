using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RadioManager : MonoBehaviour
{
    [System.Serializable]
    public class RadioStation
    {
        public string name; 
        public AudioClip[] tracks;
        public bool isUnlocked;
    }

    public RadioStation[] stations;
    public AudioSource audioSource;
    public TextMeshProUGUI stationText;
    public TextMeshProUGUI songText;
    public Slider progressSlider;
    public GameObject radioPanel;
    public Button openButton;
    public Button closeButton;
    public Button repeatButton;
    public Sprite repeatOffSprite;
    public Sprite repeatOnSprite;
    public Button shuffleButton;
    public Sprite shuffleOffSprite;
    public Sprite shuffleOnSprite;
    private float trackLength;
    private float currentPlayTime;
    private float targetPlayPosition;

    private int currentStationIndex = 0;
    private int currentTrackIndex = 0;
    private bool isPlaying = false;
    private bool isDragging = false;
    private bool isRepeatOn = false;
    private bool isShuffleOn = false;
    private List<int> shuffledTracks = new List<int>();

    private void Awake()
    {
        openButton.onClick.AddListener(OpenRadioPanel);
        closeButton.onClick.AddListener(CloseRadioPanel);
        radioPanel.SetActive(false);
    }

    void Start()
    {
        stations[0].isUnlocked = true;
        UpdateText();
        if (stations[0].tracks.Length > 0)
        {
            Debug.Log($"RadioManager: Воспроизведение первого трека на станции {stations[0].name}");
            PlayTrack();
        }
        else
        {
            Debug.LogWarning("RadioManager: Не найдено треков для первой станции");
        }
        UpdateRepeatButtonVisual();
        UpdateShuffleButtonVisual();
    }

    void Update()
    {
        if (isPlaying && !isDragging)
        {
            currentPlayTime = audioSource.time;
            progressSlider.value = currentPlayTime;

            if (!audioSource.isPlaying && audioSource.time >= audioSource.clip.length - 0.1f)
            {
                if (isRepeatOn)
                {
                    Debug.Log("RadioManager: Повтор трека");
                    audioSource.time = 0;
                    audioSource.Play();
                }
                else
                {
                    Debug.Log("RadioManager: Трек завершился, играет следующий");
                    PlayNextTrack();
                }
            }
        }
    }

    public void OpenRadioPanel()
    {
        radioPanel.SetActive(true);
    }

    public void CloseRadioPanel()
    {
        radioPanel.SetActive(false);
    }

    public void PlayTrack()
    {
        if (stations[currentStationIndex].tracks.Length == 0)
        {
            Debug.LogWarning($"RadioManager: На станции нет доступных треков {stations[currentStationIndex].name}");
            return;
        }

        Debug.Log($"RadioManager: Воспроизведение трека {currentTrackIndex} на станции{stations[currentStationIndex].name}");

        audioSource.clip = stations[currentStationIndex].tracks[currentTrackIndex];
        audioSource.Play();
        isPlaying = true;

        trackLength = audioSource.clip.length;
        progressSlider.maxValue = trackLength;
        progressSlider.value = 0;

        UpdateText();
    }

    public void PlayNextTrack()
    {
        Debug.Log("RadioManager: Переключение на следующий трек");

        if (isShuffleOn)
        {
            if (shuffledTracks.Count == 0)
            {
                Debug.Log("RadioManager: Восстановление списка перемешанных треков");
                ShuffledTracks();
            }

            currentTrackIndex = shuffledTracks[0];
            shuffledTracks.RemoveAt(0);
        }
        else
        {
            currentTrackIndex = (currentTrackIndex + 1) % stations[currentStationIndex].tracks.Length;
        }

        PlayTrack();
    }

    public void PlayPreviousTrack()
    {
        Debug.Log("RadioManager: Переключение на предыдущий трек");

        if (isShuffleOn)
        {
            if (shuffledTracks.Count == 0)
            {
                Debug.Log("RadioManager: Восстановление списка перемешанных треков");
                ShuffledTracks();
            }

            currentTrackIndex = shuffledTracks[shuffledTracks.Count - 1];
            shuffledTracks.RemoveAt(shuffledTracks.Count - 1);
        }
        else
        {
            currentTrackIndex = (currentTrackIndex - 1 + stations[currentStationIndex].tracks.Length) % stations[currentStationIndex].tracks.Length;
        }

        PlayTrack();
    }

    public void TogglePlayPause()
    {
        if (isPlaying)
        {
            Debug.Log("RadioManager: Пауза");
            audioSource.Pause();
            isPlaying = false;
        }
        else
        {
            Debug.Log("RadioManager: Возобновление");
            audioSource.Play();
            isPlaying = true;
        }
        UpdateText();
    }

    public void ToggleRepeat()
    {
        isRepeatOn = !isRepeatOn;
        Debug.Log($"RadioManager: Режим повтора {(isRepeatOn ? "включен" : "выключен")}");
        UpdateRepeatButtonVisual();
        UpdateText();
    }

    private void UpdateRepeatButtonVisual()
    {
        if (repeatButton != null)
        {
            repeatButton.image.sprite = isRepeatOn ? repeatOnSprite : repeatOffSprite;
        }
    }

    public void ToggleShuffle()
    {
        isShuffleOn = !isShuffleOn;
        Debug.Log($"RadioManager: Режим перемешиванмя {(isShuffleOn ? "включен" : "выключен")}");
        if (isShuffleOn)
            ShuffledTracks();
        UpdateShuffleButtonVisual();
        UpdateText();
    }

    private void UpdateShuffleButtonVisual()
    {
        if (shuffleButton != null)
        {
            shuffleButton.image.sprite = isShuffleOn ? shuffleOnSprite : shuffleOffSprite;
        }
    }

    public void SwitchStation(int stationIndex)
    {
        Debug.Log($"RadioManager: Переключение на станцию {stations[stationIndex].name}");
        currentStationIndex = stationIndex;
        currentTrackIndex = 0;
        PlayTrack();
    }

    public void UnlockStation(int stationIndex)
    {
        stations[stationIndex].isUnlocked = true;
        Debug.Log($"RadioManager: Станция {stations[stationIndex].name} разблокирована");
        UpdateText();
    }

    private void ShuffledTracks()
    {
        Debug.Log("RadioManager: Перемешивание треков");
        shuffledTracks = Enumerable.Range(0, stations[currentStationIndex].tracks.Length).ToList();
        shuffledTracks = shuffledTracks.OrderBy(x => Random.value).ToList();
    }

    private void UpdateText()
    {
        stationText.text = stations[currentStationIndex].name;
        songText.text = stations[currentStationIndex].tracks[currentTrackIndex].name;
    }

    public void OnSliderBeginDrag()
    {
        isDragging = true;
        Debug.Log("RadioManager: Начали перемотку");
    }

    public void OnSliderEndDrag()
    {
        isDragging = false;
        audioSource.time = progressSlider.value;
        Debug.Log($"RadioManager: Перемотка завершена. Новое время: {progressSlider.value}");
    }

    public void SliderValueChanged()
    {
        if (isDragging)
        {
            targetPlayPosition = progressSlider.value;
        }
    }
}