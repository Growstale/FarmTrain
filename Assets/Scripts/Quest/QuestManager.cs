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
        ActivateQuestsForCurrentPhase();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    public void ActivateQuestsForCurrentPhase()
    {
        int level = ExperienceManager.Instance.CurrentLevel;
        GamePhase phase = ExperienceManager.Instance.CurrentPhase;

        Debug.Log($"<color=yellow>[QuestManager]</color> Активация квестов для Уровня {level}, Фаза: {phase}");

        var questsForPhase = allQuests.Where(q => q.gameLevel == level && q.phase == phase && q.status == QuestStatus.NotAccepted).ToList();

        // Специальная логика для самой первой фазы (Поезд 1)
        if (level == 1 && phase == GamePhase.Train)
        {
            Debug.Log("Активация квестов в режиме 'по цепочке'.");
            var firstQuestInChain = questsForPhase.FirstOrDefault(q => !allQuests.Any(pq => pq.nextQuest == q));
            if (firstQuestInChain != null)
            {
                StartQuest(firstQuestInChain);
            }
        }
        else
        {
            Debug.Log("Активация квестов в режиме 'все сразу'.");
            foreach (var quest in questsForPhase)
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

    public void AddQuestProgress(GoalType eventType, string targetID, int amount)
    {
        foreach (var quest in new List<Quest>(ActiveQuests))
        {
            bool questProgressed = false;
            foreach (var goal in quest.goals.Where(g => !g.IsReached()))
            {
                bool progressMadeOnThisGoal = false;

                if (goal.goalType == eventType)
                {
                    switch (goal.goalType)
                    {
                        case GoalType.Gather:
                        case GoalType.Buy:
                        case GoalType.FeedAnimal:
                        case GoalType.Use:
                            if (goal.targetID == targetID) progressMadeOnThisGoal = true;
                            break;

                        case GoalType.GatherAny:
                        case GoalType.BuyAny:
                            if (goal.targetIDs.Contains(targetID)) progressMadeOnThisGoal = true;
                            break;

                        case GoalType.Earn:
                        case GoalType.SellFor: // Теперь этот case абсолютно правильный
                            progressMadeOnThisGoal = true;
                            break;
                    }
                }

                if (progressMadeOnThisGoal)
                {
                    goal.UpdateProgress(amount);
                    questProgressed = true;
                    Debug.Log($"<color=purple>[QuestManager]</color> Прогресс для цели '{goal.goalType}' квеста '{quest.title}' (+{amount})");
                }
            }

            if (questProgressed)
            {
                OnQuestLogUpdated?.Invoke();
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

    private void SubscribeToEvents()
    {
        InventoryManager.Instance.OnItemAdded += HandleItemAdded;
        PlayerWallet.Instance.OnMoneyAdded += HandleMoneyAdded;

        // <<< НОВАЯ ЛОГИКА ПОДПИСКИ >>>
        // Подписываемся на СОЗДАНИЕ ShopUIManager
        ShopUIManager.OnInstanceReady += HandleShopUIManagerReady;

        // Также проверим, может ShopUIManager УЖЕ существует (если мы загрузились сразу на станцию)
        if (ShopUIManager.Instance != null)
        {
            HandleShopUIManagerReady(ShopUIManager.Instance);
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemAdded -= HandleItemAdded;
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.OnMoneyAdded -= HandleMoneyAdded;

        // <<< НОВАЯ ЛОГИКА ОТПИСКИ >>>
        // Отписываемся от СОЗДАНИЯ
        ShopUIManager.OnInstanceReady -= HandleShopUIManagerReady;

        // И на всякий случай отписываемся от самого события, если мы были на него подписаны
        if (ShopUIManager.Instance != null)
        {
            ShopUIManager.Instance.OnItemPurchased -= HandleItemPurchased;
        }
    }

    // <<< НОВЫЙ МЕТОД-ОБРАБОТЧИК >>>
    private void HandleShopUIManagerReady(ShopUIManager shopUI)
    {
        Debug.Log("<color=lightblue>[QuestManager]</color> ShopUIManager появился. Подписываюсь на OnItemPurchased.");
        // Отписываемся на всякий случай, чтобы избежать двойной подписки
        shopUI.OnItemPurchased -= HandleItemPurchased;
        // Подписываемся на событие покупки
        shopUI.OnItemPurchased += HandleItemPurchased;
    }

    private void HandleItemAdded(ItemData item, int quantity)
    {
        // === НАЧАЛО ИЗМЕНЕНИЙ ===

        // 1. Отправляем прогресс для квестов "Собрать конкретный предмет" (Gather)
        AddQuestProgress(GoalType.Gather, item.name, quantity);

        // 2. ОТПРАВЛЯЕМ ПРОГРЕСС ДЛЯ КВЕСТОВ "СОБРАТЬ ЛЮБОЙ ИЗ СПИСКА" (GatherAny)
        AddQuestProgress(GoalType.GatherAny, item.name, quantity);

        // 3. Обрабатываем логику для квестов типа SellFor (этот блок остается без изменений)
        int currentLevel = ExperienceManager.Instance.CurrentLevel;
        StationData stationData = StationDatabase.Instance.GetStationDataById(currentLevel);
        if (stationData == null) return;

        int sellPrice = 0;
        foreach (var stallInventory in stationData.stallInventories)
        {
            var shopItem = stallInventory.shopItems.FirstOrDefault(si => si.itemData == item);
            if (shopItem != null && shopItem.willBuy)
            {
                sellPrice = shopItem.sellPrice;
                break;
            }
        }

        if (sellPrice > 0)
        {
            int totalPotentialValue = sellPrice * quantity;
            AddQuestProgress(GoalType.SellFor, item.name, totalPotentialValue);
        }
        // === КОНЕЦ ИЗМЕНЕНИЙ ===
    }


    private void HandleItemPurchased(ItemData item, int quantity)
    {
        Debug.Log($"<color=lightblue>[QuestManager]</color> Получено событие покупки: {item.name}");

        // 1. Отправляем прогресс для целей типа "Купить конкретный предмет" (Buy)
        AddQuestProgress(GoalType.Buy, item.name, quantity);

        // 2. И ТАКЖЕ отправляем прогресс для целей типа "Купить любой из списка" (BuyAny)
        AddQuestProgress(GoalType.BuyAny, item.name, quantity);
    }

    private void HandleMoneyAdded(int amountAdded)
    {
        // Отправляем прогресс для типа Earn
        AddQuestProgress(GoalType.Earn, "", amountAdded);
    }

    public void TriggerQuestLogUpdate()
    {
        OnQuestLogUpdated?.Invoke();
    }

}