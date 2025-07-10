using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LocomotiveController : MonoBehaviour
{
<<<<<<< Updated upstream
    // ��������� ������ �����: �� ���� ��������, ���� ����� � �������.
=======
    [SerializeField] private AudioSource trainAudioSource;
    [SerializeField] private AudioClip trainMovingClip;
    [SerializeField] private float fadeDuration = 3.0f; // длительность затухания в секундах
    private Coroutine fadeOutCoroutine;
    [SerializeField] private AudioClip hornSound;
    // Состояния теперь проще: мы либо движемся, либо стоим у станции.
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
=======
        UpdateTrainSound();  // обновляем звук движения

>>>>>>> Stashed changes
    }

<<<<<<< Updated upstream
=======
            if (!trainAudioSource.isPlaying)
            {
                trainAudioSource.clip = trainMovingClip;
                trainAudioSource.loop = true;
                trainAudioSource.volume = 0.1f; // на всякий случай вернуть громкость
                trainAudioSource.Play();
            }
        }
        else
        {
            // Если поезд не движется и звук играет — запускаем плавное затухание
            if (trainAudioSource.isPlaying && fadeOutCoroutine == null)
            {
                fadeOutCoroutine = StartCoroutine(FadeOutSound());
            }
        }
    }
    private IEnumerator FadeOutSound()
    {
        float startVolume = trainAudioSource.volume;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            trainAudioSource.volume = Mathf.Lerp(startVolume, 0f, time / fadeDuration);
            yield return null;
        }

        trainAudioSource.Stop();
        trainAudioSource.volume = 0.3f; // сбрасываем громкость для следующего запуска
        fadeOutCoroutine = null;
    }
>>>>>>> Stashed changes
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
            var currentClip = RadioManager.Instance.audioSource.clip;
            var currentTime = RadioManager.Instance.audioSource.time;
            var wasPlaying = RadioManager.IsPlaying;

            RadioManager.Instance.radioPanel?.SetActive(false);

            UIManager.Instance.ShowGoToStationButton(false); // Скрываем кнопку перед переходом

            // <<< ВОТ КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ >>>
            // Мы сообщаем менеджеру, что переходим в фазу Станции.
            // Это обнулит XP и активирует квесты для станции.
            ExperienceManager.Instance.EnterStation();
            SceneManager.LoadScene("Station_1");
            StartCoroutine(LoadStationAndRestoreRadio(currentClip, currentTime, wasPlaying));
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
        UIManager.Instance.ShowGoToStationButton(true);
        UIManager.Instance.ShowNotification(true);

        foreach (var layer in parallaxLayers) layer.enabled = false;

        // ВАЖНО: Мы не вызываем AdvanceToNextPhase здесь.
        // Смена фазы произойдет только при окончательном отправлении.
    }

    private void OnReturnFromStation()
    {
        // Когда мы возвращаемся, мы все еще в состоянии "пристыкован к станции"
        currentState = TrainState.DockedAtStation;
        departureUnlocked = TransitionManager.isDepartureUnlocked;

        UIManager.Instance.ShowNotification(false);

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

        yield return StartCoroutine(ScreenFaderManager.Instance.FadeOutAndInCoroutine(() =>
        {
            // --- ДЕЙСТВИЯ В СЕРЕДИНЕ ЗАТЕМНЕНИЯ ---

            // 1. Сообщаем ExperienceManager, что мы перешли на следующий уровень
            ExperienceManager.Instance.DepartToNextTrainLevel();
            int newLevel = ExperienceManager.Instance.CurrentLevel; // Получаем новый уровень

            // 2. Включаем движение параллакса и сообщаем им о новом уровне
            foreach (var layer in parallaxLayers)
            {
                layer.enabled = true;
                layer.SetSpriteForLevel(newLevel); // <<< ВОТ КЛЮЧЕВОЕ ИЗМЕНЕНИЕ
            }

            // --- КОНЕЦ ДЕЙСТВИЙ ---
        }));
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

    private IEnumerator LoadStationAndRestoreRadio(AudioClip clip, float time, bool play)
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
    #endregion
}