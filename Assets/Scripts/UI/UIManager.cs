using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private Button goToStationButton;
    [SerializeField] private Button returnToTrainButton;
    [SerializeField] private GameObject notificationIcon;
    // �������� ���� ������ ���������� ������ �� �������������

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // ����������� ������ � �������.
        // ������ ����� �������� ������ � ����������, ������� ���������� ������.
        goToStationButton.onClick.AddListener(() => {
            // ���� LocomotiveController �� ������� ����� � �������� ��� �����
            FindObjectOfType<LocomotiveController>()?.OnGoToStationButtonPressed();
            HideNotification();
        });

        returnToTrainButton.onClick.AddListener(() => {
            TransitionManager.Instance.GoToTrainScene();
        });

        // �������� ��� ������ ��� ����� ������ �������
        goToStationButton.gameObject.SetActive(false);
        returnToTrainButton.gameObject.SetActive(false);
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
        if (sceneName == "SampleScene") // ����� ������
        {
            goToStationButton.gameObject.SetActive(false); // ������ �� ���������, ���� ����� �� ��������
            returnToTrainButton.gameObject.SetActive(false);
        }
        else if (sceneName == "Station_1") // ����� �������
        {
            goToStationButton.gameObject.SetActive(false);
            returnToTrainButton.gameObject.SetActive(true); // ������ ����� �� �������
        }
    }

    // ��������� ������, ������� ����� �������� ������ �������
    public void ShowGoToStationButton(bool show)
    {
        goToStationButton.gameObject.SetActive(show);
    }

    public void ShowNotification(bool show)
    {
        notificationIcon.SetActive(show);
    }

    public void HideNotification()
    {
        notificationIcon.SetActive(false);
    }
}