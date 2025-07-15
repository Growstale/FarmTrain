using UnityEngine;
using UnityEngine.UI;

// Реализуем интерфейс IUIManageable для интеграции с вашей системой управления UI
public class PauseMenu : MonoBehaviour, IUIManageable
{
    [Header("Панели")]
    [SerializeField] private GameObject pauseMenuPanel; // Главная панель меню паузы
    [SerializeField] private GameObject settingsPanel;  // Панель настроек

    [Header("Кнопки главного меню")]
    [SerializeField] private Button continueButton;     // Кнопка "Продолжить"
    [SerializeField] private Button settingsButton;     // Кнопка "Настройки"
    [SerializeField] private Button exitButton;         // Кнопка "Выход"
    [SerializeField] private Button closeButton;        // Кнопка "X" для закрытия

    [Header("Кнопки меню настроек")]
    [SerializeField] private Button backButton;         // Кнопка "Назад" из настроек

    private void Start()
    {
        // Регистрируем это меню в системе эксклюзивного UI
        if (ExclusiveUIManager.Instance != null)
        {
            ExclusiveUIManager.Instance.Register(this);
        }

        // Убеждаемся, что обе панели скрыты при запуске
        pauseMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);

        // Назначаем действия для кнопок
        continueButton.onClick.AddListener(CloseMenu);
        closeButton.onClick.AddListener(CloseMenu);
        settingsButton.onClick.AddListener(ShowSettings);
        exitButton.onClick.AddListener(ExitGame);
        backButton.onClick.AddListener(HideSettings);
    }

    private void OnDestroy()
    {
        // Важно отписаться от менеджера при уничтожении объекта
        if (ExclusiveUIManager.Instance != null)
        {
            ExclusiveUIManager.Instance.Deregister(this);
        }
    }

    private void Update()
    {
        // Вызываем/закрываем меню по нажатию на ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    /// <summary>
    /// Переключает состояние меню (открыто/закрыто).
    /// </summary>
    private void ToggleMenu()
    {
        // Если открыто либо главное меню, либо настройки, то закрываем всё.
        // Иначе - открываем.
        if (pauseMenuPanel.activeSelf || settingsPanel.activeSelf)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    /// <summary>
    /// Открывает основное меню паузы.
    /// </summary>
    private void OpenMenu()
    {
        // Сообщаем менеджеру, что мы открываемся (чтобы он закрыл другие окна)
        ExclusiveUIManager.Instance.NotifyPanelOpening(this);

        // Ставим игру на паузу
        GameStateManager.Instance.RequestPause(this);

        // Показываем главную панель и скрываем настройки
        pauseMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    /// <summary>
    /// Полностью закрывает систему меню паузы.
    /// </summary>
    private void CloseMenu()
    {
        // Скрываем обе панели
        pauseMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);

        // Возобновляем игру
        GameStateManager.Instance.RequestResume(this);
    }

    /// <summary>
    /// Показывает панель настроек.
    /// </summary>
    private void ShowSettings()
    {
        pauseMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    /// <summary>
    /// Скрывает панель настроек и возвращает на главное меню паузы.
    /// </summary>
    private void HideSettings()
    {
        settingsPanel.SetActive(false);
        pauseMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Выходит из игры (с учетом режима редактора).
    /// </summary>
    private void ExitGame()
    {
        Debug.Log("Выход из игры...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #region IUIManageable Implementation

    /// <summary>
    /// Метод, вызываемый ExclusiveUIManager для принудительного закрытия.
    /// </summary>
    public void CloseUI()
    {
        CloseMenu();
    }

    /// <summary>
    /// Метод для ExclusiveUIManager, чтобы проверить, открыто ли окно.
    /// </summary>
    public bool IsOpen()
    {
        return pauseMenuPanel.activeSelf || settingsPanel.activeSelf;
    }

    #endregion
}