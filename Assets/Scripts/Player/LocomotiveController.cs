// LocomotiveController.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
// using UnityEngine.EventSystems; // <<< МОЖНО УДАЛИТЬ

public class LocomotiveController : MonoBehaviour
{
    // ... (поля состояний, если есть)

    [Header("Ссылки на объекты")]
    // <<< СДЕЛАЙТЕ ЭТО ПОЛЕ ПУБЛИЧНЫМ, чтобы другие скрипты могли его видеть
    public GameObject hornObject;
    //[SerializeField] private Animator hornAnimator;
    [SerializeField] private Button goToStationButton;
    [SerializeField] private GameObject screenFader;
    [SerializeField] private AutoScrollParallax[] parallaxLayers;

    private bool travelUnlocked = false;

    // <<< УДАЛИТЕ весь метод Update() и Awake() из этого скрипта. Они больше не нужны.
    // void Awake() { ... }
    // void Update() { ... }

    void Start()
    {
        // Подписка на событие остается
        ExperienceManager.Instance.OnPhaseUnlocked += OnPhaseUnlocked;
        goToStationButton.onClick.AddListener(GoToStation);

        screenFader.SetActive(false);
        goToStationButton.gameObject.SetActive(false);

        // Проверка при старте тоже остается
        if (ExperienceManager.Instance.CurrentPhase == GamePhase.Train &&
            ExperienceManager.Instance.CurrentXP >= ExperienceManager.Instance.XpForNextPhase)
        {
            OnPhaseUnlocked(ExperienceManager.Instance.CurrentLevel, GamePhase.Train);
        }
    }

    void OnDisable()
    {
        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnPhaseUnlocked -= OnPhaseUnlocked;
        }
    }

    // <<< СДЕЛАЙТЕ ЭТОТ МЕТОД ПУБЛИЧНЫМ
    public void OnHornClicked()
    {
        Debug.Log("<color=orange>Клик по гудку зарегистрирован через LocomotiveController!</color>");

        if (travelUnlocked)
        {
            Debug.Log("Условие travelUnlocked выполнено. Останавливаем поезд.");
            StopTrain();
        }
        else
        {
            Debug.Log("Клик по гудку был, но travelUnlocked == false. Ничего не делаем.");
        }
    }

    private void OnPhaseUnlocked(int level, GamePhase phase)
    {
        if (phase == GamePhase.Train)
        {
            Debug.Log("<color=cyan>LocomotiveController: Получен сигнал OnPhaseUnlocked. travelUnlocked = true</color>");
            travelUnlocked = true;
            //hornAnimator.SetBool("IsHighlighted", true);
        }
    }

    // Остальные методы (StopTrain, GoToStation, DepartToNextTrainLevel) остаются без изменений.
    private void StopTrain()
    {
        travelUnlocked = false;
        //hornAnimator.SetBool("IsHighlighted", false);
        foreach (var layer in parallaxLayers) layer.enabled = false;
        goToStationButton.gameObject.SetActive(true);
    }

    private void GoToStation()
    {
        ExperienceManager.Instance.AdvanceToNextPhase();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Station_1");
    }

    public IEnumerator DepartToNextTrainLevel()
    {
        screenFader.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        ExperienceManager.Instance.AdvanceToNextPhase();
        foreach (var layer in parallaxLayers) layer.enabled = true;
        yield return new WaitForSeconds(1.0f);
        screenFader.SetActive(false);
    }
}