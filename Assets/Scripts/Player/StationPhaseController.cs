using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StationPhaseController : MonoBehaviour
{
    [SerializeField] private Button departButton; // Кнопка "Отправиться дальше"
    [SerializeField] private TextMeshProUGUI stationTitle;

    private bool departureUnlocked = false;

    void Start()
    {
        ExperienceManager.Instance.OnPhaseUnlocked += OnPhaseUnlocked;
        departButton.onClick.AddListener(DepartToNextTrain);

        // Настраиваем вид при входе
        int currentLevel = ExperienceManager.Instance.CurrentLevel;
        stationTitle.text = $"Станция {currentLevel}";

        // Проверяем, нужно ли вообще тут копить опыт
        if (ExperienceManager.Instance.XpForNextPhase == 0)
        {
            OnPhaseUnlocked(currentLevel, GamePhase.Station);
        }
        else
        {
            departButton.gameObject.SetActive(false);
        }
    }

    void OnDisable()
    {
        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnPhaseUnlocked -= OnPhaseUnlocked;
        }
    }

    private void OnPhaseUnlocked(int level, GamePhase phase)
    {
        // Реагируем только на сигналы из фазы Станции
        if (phase == GamePhase.Station)
        {
            departureUnlocked = true;
            departButton.gameObject.SetActive(true);
            // Тут можно добавить анимацию подсветки кнопки
        }
    }

    private void DepartToNextTrain()
    {
        if (!departureUnlocked) return;

        // Загружаем сцену поезда. Локомотив сам разберется с анимацией перехода.
        // Мы передаем управление ему.
        StartCoroutine(LoadTrainAndDepart());
    }

    private System.Collections.IEnumerator LoadTrainAndDepart()
    {
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("TrainScene");

        // Ждем, пока сцена загрузится
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // После загрузки сцены, находим LocomotiveController и запускаем его корутину.
        // Это более надежно, чем статические методы.
        LocomotiveController loco = FindObjectOfType<LocomotiveController>();
        if (loco != null)
        {
            yield return loco.StartCoroutine(loco.DepartToNextTrainLevel());
        }
    }
}