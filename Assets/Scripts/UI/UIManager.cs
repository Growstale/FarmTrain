using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private Button goToStationButton;
    [SerializeField] private Button returnToTrainButton;
    [SerializeField] private GameObject notificationIcon;
    // Добавьте сюда другие глобальные панели по необходимости

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // Привязываем методы к кнопкам.
        // Кнопки будут вызывать методы в менеджерах, которые существуют всегда.
        goToStationButton.onClick.AddListener(() => {
            // Ищем LocomotiveController на текущей сцене и вызываем его метод
            FindObjectOfType<LocomotiveController>()?.OnGoToStationButtonPressed();
        });

        returnToTrainButton.onClick.AddListener(() => {
            TransitionManager.Instance.GoToTrainScene();
        });

        // Скрываем все кнопки при самом первом запуске
        goToStationButton.gameObject.SetActive(false);
        returnToTrainButton.gameObject.SetActive(false);;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureUIForScene(scene.name);
    }

    private void ConfigureUIForScene(string sceneName)
    {
        if (sceneName == "SampleScene") // Сцена Поезда
        {
            goToStationButton.gameObject.SetActive(false); // Скрыта по умолчанию, пока поезд не прибудет
            returnToTrainButton.gameObject.SetActive(false);
        }
        else if (sceneName == "Station_1") // Сцена Станции
        {
            goToStationButton.gameObject.SetActive(false);
            returnToTrainButton.gameObject.SetActive(true); // Всегда видна на станции
        }
    }

    // Публичные методы, которые могут вызывать другие скрипты
    public void ShowGoToStationButton(bool show)
    {
        goToStationButton.gameObject.SetActive(show);
    }

    public void ShowNotification(bool show)
    {
        notificationIcon.SetActive(show);
    }
}