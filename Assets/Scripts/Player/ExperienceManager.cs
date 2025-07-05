using UnityEngine;
using System;

public enum GamePhase
{
    Train,
    Station
}

public class ExperienceManager : MonoBehaviour
{
    // ... (Все поля и Awake/Initialize/AddXP/UpdateXpThreshold остаются без изменений) ...
    public static ExperienceManager Instance { get; private set; }

    [System.Serializable]
    public struct XpThreshold { public int trainPhaseXP; public int stationPhaseXP; }
    [SerializeField] private XpThreshold[] xpLevels;

    public int CurrentLevel { get; private set; }
    public GamePhase CurrentPhase { get; private set; }
    public int CurrentXP { get; private set; }
    public int XpForNextPhase { get; private set; }

    public event Action<int, int> OnXPChanged;
    public event Action<int, GamePhase> OnPhaseUnlocked;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize()
    {
        CurrentLevel = 1;
        CurrentPhase = GamePhase.Train;
        CurrentXP = 0;
        UpdateXpThreshold();
    }

    public void AddXP(int amount)
    {
        if (amount <= 0 || XpForNextPhase == 0) return;
        CurrentXP += amount;
        Debug.Log($"Получено {amount} XP. Всего: {CurrentXP}/{XpForNextPhase}");
        OnXPChanged?.Invoke(CurrentXP, XpForNextPhase);
        if (CurrentXP >= XpForNextPhase)
        {
            OnPhaseUnlocked?.Invoke(CurrentLevel, CurrentPhase);
            Debug.Log($"<color=cyan>Переход на следующую фазу разблокирован!</color>");
        }
    }

    // <<< НОВЫЙ МЕТОД: Вызывается при переходе С ПОЕЗДА НА СТАНЦИЮ
    public void EnterStation()
    {
        if (CurrentPhase != GamePhase.Train) return;

        CurrentPhase = GamePhase.Station;
        CurrentXP = 0; // Обнуляем опыт для заданий на станции
        UpdateXpThreshold();
        OnXPChanged?.Invoke(CurrentXP, XpForNextPhase);
        QuestManager.Instance.ActivateQuestsForCurrentPhase();
        Debug.Log($"<color=yellow>Вход на Станцию {CurrentLevel}. Фаза: {CurrentPhase}.</color>");
    }

    // <<< НОВЫЙ МЕТОД: Вызывается при отправлении СО СТАНЦИИ НА СЛЕДУЮЩИЙ ПОЕЗД
    public void DepartToNextTrainLevel()
    {
        if (CurrentPhase != GamePhase.Station) return;

        CurrentPhase = GamePhase.Train;
        CurrentLevel++; // Увеличиваем уровень игры
        CurrentXP = 0; // Обнуляем опыт для заданий на новом поезде
        UpdateXpThreshold();
        OnXPChanged?.Invoke(CurrentXP, XpForNextPhase);
        QuestManager.Instance.ActivateQuestsForCurrentPhase();
        Debug.Log($"<color=yellow>Отправление на Поезд {CurrentLevel}. Фаза: {CurrentPhase}.</color>");
    }

    // <<< УДАЛИТЕ СТАРЫЙ МЕТОД AdvanceToNextPhase() >>>
    // public void AdvanceToNextPhase() { ... }

    private void UpdateXpThreshold()
    {
        int levelIndex = CurrentLevel - 1;
        if (levelIndex < 0 || levelIndex >= xpLevels.Length)
        {
            XpForNextPhase = int.MaxValue;
            return;
        }

        if (CurrentPhase == GamePhase.Train)
        {
            XpForNextPhase = xpLevels[levelIndex].trainPhaseXP;
        }
        else
        {
            XpForNextPhase = xpLevels[levelIndex].stationPhaseXP;
        }
    }
}