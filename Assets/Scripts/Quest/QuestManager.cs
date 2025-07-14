using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [SerializeField] private List<Quest> allQuests; // ���� � ���������� ���������� ��� ���� ������

    public List<Quest> ActiveQuests { get; private set; } = new List<Quest>();
    public List<Quest> CompletedQuests { get; private set; } = new List<Quest>();

    public event Action OnQuestLogUpdated; // ������ ��� UI, ��� ������ ������� ����������
    public event Action<Quest> OnQuestCompleted; // ������ ��� UI, ����� �������� ���� ����������
    public event Action<Quest> OnNewQuestAvailable; // ������ ��� ������-�����������

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    private void Start()
    {
        // ���������� ������� ���� ������� ��� ������ (��� �����)
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

        // ��������� ������ ��� ������ �������
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

        Debug.Log($"<color=yellow>[QuestManager]</color> ��������� ������� ��� ������ {level}, ����: {phase}");

        var questsForPhase = allQuests.Where(q => q.gameLevel == level && q.phase == phase && q.status == QuestStatus.NotAccepted).ToList();

        // ����������� ������ ��� ����� ������ ���� (����� 1)
        if (level == 1 && phase == GamePhase.Train)
        {
            Debug.Log("��������� ������� � ������ '�� �������'.");
            var firstQuestInChain = questsForPhase.FirstOrDefault(q => !allQuests.Any(pq => pq.nextQuest == q));
            if (firstQuestInChain != null)
            {
                StartQuest(firstQuestInChain);
            }
        }
        else
        {
            Debug.Log("��������� ������� � ������ '��� �����'.");
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
        Debug.Log($"����� '{quest.title}' �����!");
    }

    public void CompleteQuest(Quest quest)
    {
        if (quest.status != QuestStatus.Accepted) return;

        quest.status = QuestStatus.Completed;
        ActiveQuests.Remove(quest);
        CompletedQuests.Add(quest);

        // ������ �������
        ExperienceManager.Instance.AddXP(quest.rewardXP);

        // �������� ��������� ��������� ����� � �������
        if (quest.nextQuest != null)
        {
            StartQuest(quest.nextQuest);
        }

        OnQuestCompleted?.Invoke(quest);
        OnQuestLogUpdated?.Invoke();
        Debug.Log($"����� '{quest.title}' ��������! �������: {quest.rewardXP} XP.");
        if (allQuests.Count > 0 && CompletedQuests.Count == allQuests.Count)
        {
            Debug.Log("<color=cyan>�����������! ��� ������ � ���� ���������!</color>");
            GameEvents.TriggerCompleteTheQuest(1);
        }
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
                        case GoalType.SellFor:
                        case GoalType.SellForAnimals:
                        case GoalType.SellForPlants:
                            progressMadeOnThisGoal = true;
                            break;
                    }
                }

                if (progressMadeOnThisGoal)
                {
                    goal.UpdateProgress(amount);
                    questProgressed = true;
                    Debug.Log($"<color=purple>[QuestManager]</color> �������� ��� ���� '{goal.goalType}' ������ '{quest.title}' (+{amount})");
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
        // ������� ��� �� ���� ���������
        foreach (var q in allQuests)
        {
            if (q != questToPin) q.isPinned = false;
        }
        // ����������� ��� ��� ����������
        questToPin.isPinned = !questToPin.isPinned;
        OnQuestLogUpdated?.Invoke();
    }

    private void SubscribeToEvents()
    {
        InventoryManager.Instance.OnItemAdded += HandleItemAdded;
        PlayerWallet.Instance.OnMoneyAdded += HandleMoneyAdded;

        ShopUIManager.OnInstanceReady += HandleShopUIManagerReady;

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

        ShopUIManager.OnInstanceReady -= HandleShopUIManagerReady;

        if (ShopUIManager.Instance != null)
        {
            ShopUIManager.Instance.OnItemPurchased -= HandleItemPurchased;
        }
    }

    private void HandleShopUIManagerReady(ShopUIManager shopUI)
    {
        Debug.Log("<color=lightblue>[QuestManager]</color> ShopUIManager ��������. ������������ �� OnItemPurchased.");
        shopUI.OnItemPurchased -= HandleItemPurchased;
        shopUI.OnItemPurchased += HandleItemPurchased;
    }

    private void HandleItemAdded(ItemData item, int quantity)
    {
        // 1. ���������� �������� ��� ������� "�������"
        AddQuestProgress(GoalType.Gather, item.name, quantity);
        AddQuestProgress(GoalType.GatherAny, item.name, quantity);

        // 2. �������� ������ ��� ������� �������. ���� �� �� ������, ���� ��� �� ����� �������� ������, ��� ���������.
        int currentLevel = ExperienceManager.Instance.CurrentLevel;
        StationData currentStationData = StationDatabase.Instance.GetStationDataById(currentLevel);

        if (currentStationData == null)
        {
            return;
        }

        // --- ������ ��� ������������� SellFor (����������) ---
        // ���� ���� � ����� ������
        int genericSellPrice = 0;
        foreach (var stallInventory in currentStationData.stallInventories)
        {
            var shopItem = stallInventory.shopItems.FirstOrDefault(si => si.itemData == item && si.willBuy);
            if (shopItem != null)
            {
                genericSellPrice = shopItem.sellPrice;
                break; // ����� ������ ���������� ���� - �������
            }
        }
        if (genericSellPrice > 0)
        {
            int totalPotentialValue = genericSellPrice * quantity;
            AddQuestProgress(GoalType.SellFor, item.name, totalPotentialValue);
        }

        // --- ������ ��� SellForAnimals ---
        // ���� ���� ������ � ������ ��������
        string animalInventoryName = $"AnimalHerder_Station{currentLevel}_Inventory";
        ShopInventoryData animalInventory = currentStationData.stallInventories.FirstOrDefault(inv => inv.name == animalInventoryName);
        if (animalInventory != null)
        {
            var shopItem = animalInventory.shopItems.FirstOrDefault(si => si.itemData == item && si.willBuy);
            if (shopItem != null)
            {
                int totalValue = shopItem.sellPrice * quantity;
                AddQuestProgress(GoalType.SellForAnimals, item.name, totalValue);
            }
        }

        // --- ������ ��� SellForPlants ---
        // ���� ���� ������ � ������ ��������
        string plantInventoryName = $"Gardener_Station{currentLevel}_Inventory";
        ShopInventoryData plantInventory = currentStationData.stallInventories.FirstOrDefault(inv => inv.name == plantInventoryName);
        if (plantInventory != null)
        {
            var shopItem = plantInventory.shopItems.FirstOrDefault(si => si.itemData == item && si.willBuy);
            if (shopItem != null)
            {
                int totalValue = shopItem.sellPrice * quantity;
                AddQuestProgress(GoalType.SellForPlants, item.name, totalValue);
            }
        }
    }

    private void HandleItemPurchased(ItemData item, int quantity)
    {
        Debug.Log($"<color=lightblue>[QuestManager]</color> �������� ������� �������: {item.name}");

        AddQuestProgress(GoalType.Buy, item.name, quantity);
        AddQuestProgress(GoalType.BuyAny, item.name, quantity);
    }

    private void HandleMoneyAdded(int amountAdded)
    {
        AddQuestProgress(GoalType.Earn, "", amountAdded);
    }

    public void TriggerQuestLogUpdate()
    {
        OnQuestLogUpdated?.Invoke();
    }
}