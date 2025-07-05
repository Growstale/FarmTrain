using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    public static bool isReturningFromStation = false;
    public static bool isDepartureUnlocked = false;

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
        isReturningFromStation = true;
        Debug.Log("<color=magenta>Переход на сцену поезда. isReturningFromStation = true</color>");

        // <<< УБЕДИТЕСЬ, ЧТО ИМЯ СЦЕНЫ ЗДЕСЬ ВЕРНОЕ!
        SceneManager.LoadScene("SampleScene");
    }

    public void UnlockDeparture()
    {
        if (isDepartureUnlocked) return; // Чтобы не спамить в лог
        isDepartureUnlocked = true;
        Debug.Log("<color=magenta>[TransitionManager] Отправление со станции РАЗБЛОКИРОВАНО.</color>");
    }
}