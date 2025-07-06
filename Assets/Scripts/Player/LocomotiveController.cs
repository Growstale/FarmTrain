using UnityEngine;
using System.Linq;
using System.Collections;

public class LocomotiveController : MonoBehaviour
{
    // Состояния теперь проще: мы либо движемся, либо стоим у станции.
    public enum TrainState { Moving, DockedAtStation }

    private TrainState currentState;

    // Ссылки на объекты сцены
    public GameObject hornObject { get; private set; }
    private Animator hornAnimator;
    private AutoScrollParallax[] parallaxLayers;

    // Флаги состояния
    private bool travelToStationUnlocked = false;
    private bool departureUnlocked = false;

    #region Unity Lifecycle
    void Awake()
    {
        FindSceneObjects();
        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnPhaseUnlocked += OnPhaseUnlocked;
        }
    }

    void Start()
    {
        if (TransitionManager.isReturningFromStation)
        {
            TransitionManager.isReturningFromStation = false;
            OnReturnFromStation();
        }
        else
        {
            currentState = TrainState.Moving;
            CheckInitialTravelState();
        }
    }

    void LateUpdate()
    {
        UpdateHornHighlight();
        // Постоянно обновляем UI в зависимости от состояния
        UIManager.Instance.ShowGoToStationButton(currentState == TrainState.DockedAtStation);
    }

    void OnDestroy()
    {
        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnPhaseUnlocked -= OnPhaseUnlocked;
        }
    }
    #endregion

    #region Scene Object Management
    private void FindSceneObjects()
    {
        hornObject = GameObject.FindGameObjectWithTag("Horn");
        if (hornObject != null)
        {
            hornAnimator = hornObject.GetComponent<Animator>();
        }
        else Debug.LogError("[LocomotiveController] Не найден объект с тегом 'Horn'!");

        parallaxLayers = GameObject.FindGameObjectsWithTag("ParallaxLayer")
            .Select(go => go.GetComponent<AutoScrollParallax>()).Where(c => c != null).ToArray();
    }
    #endregion

    #region Event Handlers & Core Logic
    public void OnHornClicked()
    {
        switch (currentState)
        {
            case TrainState.Moving:
                if (travelToStationUnlocked)
                {
                    ArriveAtStation();
                }
                break;
            case TrainState.DockedAtStation:
                if (departureUnlocked)
                {
                    StartCoroutine(DepartSequence());
                }
                else
                {
                    Debug.Log("Гудок нажат, но отправление со станции еще не разблокировано.");
                }
                break;
        }
    }

    public void OnGoToStationButtonPressed()
    {
        if (currentState == TrainState.DockedAtStation)
        {
            UIManager.Instance.ShowGoToStationButton(false); // Скрываем кнопку перед переходом

            // <<< ВОТ КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ >>>
            // Мы сообщаем менеджеру, что переходим в фазу Станции.
            // Это обнулит XP и активирует квесты для станции.
            ExperienceManager.Instance.EnterStation();

            UnityEngine.SceneManagement.SceneManager.LoadScene("Station_1");
        }
    }

    private void OnPhaseUnlocked(int level, GamePhase phase)
    {
        if (phase == GamePhase.Train)
        {
            travelToStationUnlocked = true;
        }
        else if (phase == GamePhase.Station)
        {
            departureUnlocked = true;
            // Обновляем флаг в TransitionManager, чтобы он сохранился между сценами
            TransitionManager.isDepartureUnlocked = true;
        }
    }

    private void ArriveAtStation()
    {
        currentState = TrainState.DockedAtStation;
        // Мы НЕ сбрасываем travelToStationUnlocked, чтобы состояние сохранилось

        foreach (var layer in parallaxLayers) layer.enabled = false;

        // ВАЖНО: Мы не вызываем AdvanceToNextPhase здесь.
        // Смена фазы произойдет только при окончательном отправлении.
    }

    private void OnReturnFromStation()
    {
        // Когда мы возвращаемся, мы все еще в состоянии "пристыкован к станции"
        currentState = TrainState.DockedAtStation;
        departureUnlocked = TransitionManager.isDepartureUnlocked;

        // Останавливаем фон, так как он мог запуститься при загрузке сцены
        foreach (var layer in parallaxLayers) layer.enabled = false;
    }

    private IEnumerator DepartSequence()
    {
        currentState = TrainState.Moving;
        travelToStationUnlocked = false;
        departureUnlocked = false;
        TransitionManager.isDepartureUnlocked = false;

        UpdateHornHighlight();
        UIManager.Instance.ShowGoToStationButton(false);

        ScreenFaderManager.Instance.FadeOutAndIn(() => {
            // <<< ИСПОЛЬЗУЕМ ПРАВИЛЬНЫЙ МЕТОД ДЛЯ ОКОНЧАТЕЛЬНОГО ОТПРАВЛЕНИЯ
            ExperienceManager.Instance.DepartToNextTrainLevel();

            foreach (var layer in parallaxLayers) layer.enabled = true;
        });

        yield return null;
    }


    private void CheckInitialTravelState()
    {
        if (ExperienceManager.Instance.CurrentPhase == GamePhase.Train &&
            ExperienceManager.Instance.CurrentXP >= ExperienceManager.Instance.XpForNextPhase)
        {
            travelToStationUnlocked = true;
        }
    }

    private void UpdateHornHighlight()
    {
        if (hornAnimator == null) return;
        bool shouldHighlight = (currentState == TrainState.Moving && travelToStationUnlocked) ||
                               (currentState == TrainState.DockedAtStation && departureUnlocked);
        hornAnimator.SetBool("IsHighlighted", shouldHighlight);
    }
    #endregion
}