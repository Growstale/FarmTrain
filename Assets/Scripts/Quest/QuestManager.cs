using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [SerializeField] private List<Quest> allQuests; // Сюда в инспекторе перетащите все ваши квесты

    public List<Quest> ActiveQuests { get; private set; } = new List<Quest>();
    public List<Quest> CompletedQuests { get; private set; } = new List<Quest>();

    public event Action OnQuestLogUpdated; // Сигнал для UI, что списки квестов изменились
    public event Action<Quest> OnQuestCompleted; // Сигнал для UI, чтобы показать окно завершения
    public event Action<Quest> OnNewQuestAvailable; // Сигнал для иконки-уведомления

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    private void Start()
    {
        // Сбрасываем статусы всех квестов при старте (для теста)
        foreach (var quest in allQuests)
        {
            quest.status = QuestStatus.NotAccepted;
            quest.isPinned = false;
            quest.hasBeenViewed = false;
            foreach (var goal in quest.goals)
            {
                goal.currentAmount = 0;
            }
        }

        SubscribeToEvents();

        // Запускаем квесты для первой станции
        ActivateQuestsForStation(1, true);
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void HandleStationChanged(int newStationID)
    {
        ActivateQuestsForStation(newStationID, false);
    }

    // Активирует квесты для станции
    private void ActivateQuestsForStation(int stationId, bool isFirstStation)
    {
        var questsForStation = allQuests.Where(q => q.stationId == stationId && q.status == QuestStatus.NotAccepted).ToList();

        if (isFirstStation)
        {
            // Для первой станции начинаем только первый квест (у которого нет "предка")
            var firstQuest = questsForStation.FirstOrDefault(q => !allQuests.Any(pq => pq.nextQuest == q));
            if (firstQuest != null)
            {
                StartQuest(firstQuest);
            }
        }
        else
        {
            // Для станций 2+ активируем все квесты сразу
            foreach (var quest in questsForStation)
            {
                StartQuest(quest);
            }
        }
    }

    public void StartQuest(Quest quest)
    {
        if (quest.status != QuestStatus.NotAccepted) return;

        quest.status = QuestStatus.Accepted;
        ActiveQuests.Add(quest);
        OnNewQuestAvailable?.Invoke(quest);
        OnQuestLogUpdated?.Invoke();
        Debug.Log($"Квест '{quest.title}' начат!");
    }

    public void CompleteQuest(Quest quest)
    {
        if (quest.status != QuestStatus.Accepted) return;

        quest.status = QuestStatus.Completed;
        ActiveQuests.Remove(quest);
        CompletedQuests.Add(quest);

        // Выдаем награду
        ExperienceManager.Instance.AddXP(quest.rewardXP);

        // Пытаемся запустить следующий квест в цепочке
        if (quest.nextQuest != null)
        {
            StartQuest(quest.nextQuest);
        }

        OnQuestCompleted?.Invoke(quest);
        OnQuestLogUpdated?.Invoke();
        Debug.Log($"Квест '{quest.title}' выполнен! Награда: {quest.rewardXP} XP.");
    }

    // Этот метод будут вызывать другие системы
    public void AddQuestProgress(GoalType goalType, string targetID, int amount)
    {
        foreach (var quest in new List<Quest>(ActiveQuests)) // Создаем временную копию списка
        {
            bool questProgressed = false;
            foreach (var goal in quest.goals.Where(g => g.goalType == goalType && !g.IsReached()))
            {
                // Для сбора и покупки проверяем ID, для заработка - нет.
                if (goalType == GoalType.Gather || goalType == GoalType.Buy)
                {
                    if (goal.targetID == targetID)
                    {
                        goal.UpdateProgress(amount);
                        questProgressed = true;
                    }
                }
                else if (goalType == GoalType.Earn)
                {
                    // Для заработка денег мы не добавляем, а устанавливаем текущее значение
                    goal.currentAmount = PlayerWallet.Instance.GetCurrentMoney();
                    questProgressed = true;
                }
            }

            if (questProgressed)
            {
                Debug.Log($"Прогресс для квеста '{quest.title}' обновлен.");
                OnQuestLogUpdated?.Invoke(); // Обновляем UI
                CheckQuestCompletion(quest);
            }
        }
    }

    private void CheckQuestCompletion(Quest quest)
    {
        if (quest.goals.All(g => g.IsReached()))
        {
            CompleteQuest(quest);
        }
    }

    public void PinQuest(Quest questToPin)
    {
        // Снимаем пин со всех остальных
        foreach (var q in allQuests)
        {
            if (q != questToPin) q.isPinned = false;
        }
        // Переключаем пин для выбранного
        questToPin.isPinned = !questToPin.isPinned;
        OnQuestLogUpdated?.Invoke();
    }

    private void UnsubscribeFromEvents()
    {
        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnStationChanged -= HandleStationChanged;
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemAdded -= HandleItemAdded;
        //if (ShopUIManager.Instance != null)
          //  ShopUIManager.Instance.OnItemPurchased -= HandleItemPurchased;
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.OnMoneyChanged -= HandleMoneyChanged;
    }
    private void SubscribeToEvents()
    {
        ExperienceManager.Instance.OnStationChanged += HandleStationChanged;
        InventoryManager.Instance.OnItemAdded += HandleItemAdded;
        //ShopUIManagerShopUIManager.Instance.OnItemPurchased += HandleItemPurchased;
        // PlayerWallet.OnMoneyChanged уже существует, используем его
        PlayerWallet.Instance.OnMoneyChanged += HandleMoneyChanged;
    }
    private void HandleItemAdded(ItemData item, int quantity)
    {
        // Отправляем прогресс для целей типа Gather
        AddQuestProgress(GoalType.Gather, item.name, quantity);
    }

    private void HandleItemPurchased(ItemData item, int quantity)
    {
        // Отправляем прогресс для целей типа Buy
        AddQuestProgress(GoalType.Buy, item.name, quantity);
    }

    private void HandleMoneyChanged(int newTotalMoney)
    {
        // Отправляем прогресс для целей типа Earn
        // amount не используется, т.к. мы просто устанавливаем текущее значение
        AddQuestProgress(GoalType.Earn, "", newTotalMoney);
    }
    public void TriggerQuestLogUpdate()
    {
        OnQuestLogUpdated?.Invoke();
    }

}