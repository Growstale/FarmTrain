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


    public void AddQuestProgress(GoalType goalType, string targetID, int amount)
    {
        foreach (var quest in new List<Quest>(ActiveQuests))
        {
            bool questProgressed = false;
            foreach (var goal in quest.goals.Where(g => g.goalType == goalType && !g.IsReached()))
            {
                // <<< ОБЪЕДИНЯЕМ ЛОГИКУ ДЛЯ ВСЕХ НАКОПИТЕЛЬНЫХ ЦЕЛЕЙ
                if (goalType == GoalType.Gather ||
                    goalType == GoalType.Buy ||
                    goalType == GoalType.FeedAnimal ||
                    goalType == GoalType.Earn) // <<< ДОБАВЛЯЕМ EARN В ЭТОТ СПИСОК
                {
                    // Для Earn нам не нужно проверять ID, прогресс идет всегда.
                    // Для остальных целей ID должен совпадать.
                    if (goalType == GoalType.Earn || goal.targetID == targetID)
                    {
                        goal.UpdateProgress(amount); // Этот метод суммирует: currentAmount += amount
                        questProgressed = true;
                    }
                }
                // <<< СТАРАЯ ЛОГИКА для else if (goalType == GoalType.Earn) теперь не нужна и удалена
            }

            if (questProgressed)
            {
                Debug.Log($"<color=lightblue>[QuestManager]</color> Прогресс для квеста '{quest.title}' обновлен по цели '{goalType}'.");
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

    private void UnsubscribeFromEvents()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemAdded -= HandleItemAdded;
        //if (ShopUIManager.Instance != null)
          //  ShopUIManager.Instance.OnItemPurchased -= HandleItemPurchased;
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.OnMoneyAdded -= HandleMoneyAdded;
    }
    private void SubscribeToEvents()
    {
        InventoryManager.Instance.OnItemAdded += HandleItemAdded;
        //ShopUIManagerShopUIManager.Instance.OnItemPurchased += HandleItemPurchased;
        PlayerWallet.Instance.OnMoneyAdded += HandleMoneyAdded;

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

    private void HandleMoneyAdded(int amountAdded)
    {
        // Теперь мы отправляем прогресс для типа Earn, но передаем только СУММУ ПРИРОСТА
        AddQuestProgress(GoalType.Earn, "", amountAdded);
    }
    public void TriggerQuestLogUpdate()
    {
        OnQuestLogUpdated?.Invoke();
    }

}