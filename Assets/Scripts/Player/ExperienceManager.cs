using UnityEngine;
using System;

public enum GamePhase
{
    Train,
    Station
}

public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager Instance { get; private set; }

    [System.Serializable]
    public struct XpThreshold
    {
        public int trainPhaseXP; // XP для перехода с поезда на станцию
        public int stationPhaseXP; // XP для перехода со станции на следующий поезд
    }

    [SerializeField] private XpThreshold[] xpLevels; // Массив порогов XP для каждого "уровня"

    public int CurrentLevel { get; private set; } // Уровень игры (1, 2, 3...)
    public GamePhase CurrentPhase { get; private set; } // Текущая фаза (Поезд или Станция)

    public int CurrentXP { get; private set; }
    public int XpForNextPhase { get; private set; }

    public event Action<int, int> OnXPChanged; // currentXP, xpForNext
    public event Action<int, GamePhase> OnPhaseUnlocked; // Сигнал, что можно переходить на след. фазу

    private void Awake()
    {
        // ... (синглтон)
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; DontDestroyOnLoad(gameObject); Initialize(); }
    }

    private void Initialize()
    {
        // В будущем здесь будет загрузка сохранения
        CurrentLevel = 1;
        CurrentPhase = GamePhase.Train;
        CurrentXP = 0;
        UpdateXpThreshold();
    }

    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        CurrentXP += amount;
        Debug.Log($"Получено {amount} XP. Всего: {CurrentXP}/{XpForNextPhase}");

        OnXPChanged?.Invoke(CurrentXP, XpForNextPhase);

        if (CurrentXP >= XpForNextPhase)
        {
            // Мы не переходим на новый уровень автоматически.
            // Мы просто сообщаем, что переход РАЗБЛОКИРОВАН.
            OnPhaseUnlocked?.Invoke(CurrentLevel, CurrentPhase);
            Debug.Log($"<color=cyan>Переход на следующую фазу разблокирован!</color>");
        }
    }

    // Этот метод будет вызываться из LocomotiveController или StationController
    public void AdvanceToNextPhase()
    {
        CurrentXP = 0; // Сбрасываем XP для нового этапа

        if (CurrentPhase == GamePhase.Train)
        {
            // Переходим со Поезда на Станцию
            CurrentPhase = GamePhase.Station;
        }
        else // CurrentPhase == GamePhase.Station
        {
            // Переходим со Станции на следующий Поезд
            CurrentPhase = GamePhase.Train;
            CurrentLevel++;
        }

        UpdateXpThreshold();
        OnXPChanged?.Invoke(CurrentXP, XpForNextPhase); // Обновляем UI XP бара
        QuestManager.Instance.ActivateQuestsForCurrentPhase(); // <<< КЛЮЧЕВОЙ МОМЕНТ
    }

    private void UpdateXpThreshold()
    {
        int levelIndex = CurrentLevel - 1;
        if (levelIndex < 0 || levelIndex >= xpLevels.Length)
        {
            XpForNextPhase = int.MaxValue; // Конец игры
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
