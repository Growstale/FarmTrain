using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    public static bool isReturningFromStation = false;
    public static bool isDepartureUnlocked = false;
    public static bool wasTrainingPanelOpen = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void GoToTrainScene()
    {
        var currentClip = RadioManager.Instance.audioSource.clip;
        var currentTime = RadioManager.Instance.audioSource.time;
        var wasPlaying = RadioManager.IsPlaying;

        RadioManager.Instance.radioPanel?.SetActive(false);

        if (TrainingVideoManager.Instance != null)
        {
            wasTrainingPanelOpen = TrainingVideoManager.Instance.trainingPanel.activeSelf;
        }

        isReturningFromStation = true;
        Debug.Log("<color=magenta>Переход на сцену поезда. isReturningFromStation = true</color>");

        // <<< УБЕДИТЕСЬ, ЧТО ИМЯ СЦЕНЫ ЗДЕСЬ ВЕРНОЕ!
        SceneManager.LoadScene("SampleScene");
        StartCoroutine(RestoreRadioState(currentClip, currentTime, wasPlaying));
    }

    public void UnlockDeparture()
    {
        if (isDepartureUnlocked) return; // Чтобы не спамить в лог
        isDepartureUnlocked = true;
        Debug.Log("<color=magenta>[TransitionManager] Отправление со станции РАЗБЛОКИРОВАНО.</color>");
    }

    private IEnumerator RestoreRadioState(AudioClip clip, float time, bool play)
    {
        yield return new WaitForSeconds(0.1f);

        RadioManager.Instance.audioSource.clip = clip;
        RadioManager.Instance.audioSource.time = time;
        if (play)
        {
            RadioManager.Instance.audioSource.Play();
            RadioManager.IsPlaying = true;
        }
    }
}