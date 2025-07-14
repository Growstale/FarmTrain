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
        Debug.Log("<color=magenta>������� �� ����� ������. isReturningFromStation = true</color>");

        // <<< ���������, ��� ��� ����� ����� ������!
        SceneManager.LoadScene("SampleScene");
       
        StartCoroutine(RestoreRadioState(currentClip, currentTime, wasPlaying));
    }

    public void UnlockDeparture()
    {
        if (isDepartureUnlocked) return; // ����� �� ������� � ���
        isDepartureUnlocked = true;
        Debug.Log("<color=magenta>[TransitionManager] ����������� �� ������� ��������������.</color>");
    }

    private IEnumerator RestoreRadioState(AudioClip clip, float time, bool play)
    {
        yield return new WaitForSeconds(0.01f);

        RadioManager.Instance.audioSource.clip = clip;
        RadioManager.Instance.audioSource.time = time;
        if (play)
        {
            RadioManager.Instance.audioSource.Play();
            RadioManager.IsPlaying = true;
        }
    }
}