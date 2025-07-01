using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class QuestLogUI : MonoBehaviour
{
    // --- ПРИВЯЗКИ К UI В ИНСПЕКТОРЕ ---
    [Header("Main Components")]
    [SerializeField] private GameObject questLogPanelObject; // Ссылка на саму панель
    [SerializeField] private Button openLogButton; // Кнопка-книжка на главном экране
    [SerializeField] private Button closeLogButton; // Крестик на панели

    [Header("Quest List")]
    [Tooltip("Префаб строки для одного квеста")]
    [SerializeField] private GameObject questEntryPrefab;
    [Tooltip("Контейнер, куда будут добавляться активные квесты")]
    [SerializeField] private Transform questListContentContainer;

    [Header("Quest Details (Right Panel)")]
    [SerializeField] private GameObject detailsPanelObject; // Правая панель с деталями
    [SerializeField] private TextMeshProUGUI detailsTitleText;
    [SerializeField] private TextMeshProUGUI detailsDescriptionText;
    [SerializeField] private TextMeshProUGUI detailsGoalsText; // Текст для списка целей
    [SerializeField] private TextMeshProUGUI detailsRewardText; // Текст для награды (например, "10 XP")

    private List<GameObject> spawnedEntries = new List<GameObject>();
    private Quest selectedQuest;

    private void Start()
    {
        // Привязываем методы к кнопкам
        openLogButton.onClick.AddListener(ToggleLog);
        closeLogButton.onClick.AddListener(CloseLog);

        // Подписываемся на события менеджера
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestLogUpdated += UpdateUI;
        }

        // Убеждаемся, что все скрыто при старте
        questLogPanelObject.SetActive(false);
        detailsPanelObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestLogUpdated -= UpdateUI;
        }
    }

    public void ToggleLog()
    {
        bool isActive = questLogPanelObject.activeSelf;
        questLogPanelObject.SetActive(!isActive);
        if (!isActive) // Если только что открыли
        {
            UpdateUI();
        }
    }

    private void CloseLog()
    {
        questLogPanelObject.SetActive(false);
    }

    // Главный метод обновления всего UI
    private void UpdateUI()
    {
        // Если панель не активна, ничего не делаем
        if (!questLogPanelObject.activeSelf) return;

        PopulateQuestList();

        // Показываем детали для ранее выбранного квеста или первого в списке
        if (selectedQuest != null && QuestManager.Instance.ActiveQuests.Contains(selectedQuest))
        {
            ShowQuestDetails(selectedQuest);
        }
        else
        {
            var firstQuest = QuestManager.Instance.ActiveQuests.FirstOrDefault() ?? QuestManager.Instance.CompletedQuests.FirstOrDefault();
            if (firstQuest != null)
            {
                ShowQuestDetails(firstQuest);
            }
            else
            {
                detailsPanelObject.SetActive(false);
            }
        }
    }

    private void PopulateQuestList()
    {
        // Уничтожаем старые записи
        foreach (var entry in spawnedEntries)
        {
            Destroy(entry);
        }
        spawnedEntries.Clear();

        // Объединяем активные и выполненные квесты для отображения в одном списке
        var allVisibleQuests = QuestManager.Instance.ActiveQuests.Concat(QuestManager.Instance.CompletedQuests).ToList();

        foreach (var quest in allVisibleQuests)
        {
            GameObject entryGO = Instantiate(questEntryPrefab, questListContentContainer);
            entryGO.GetComponent<QuestLogEntryUI>().Setup(quest, OnQuestSelected); // Передаем метод выбора
            spawnedEntries.Add(entryGO);
        }
    }

    // Этот метод вызывается из QuestLogEntryUI, когда мы кликаем на квест
    private void OnQuestSelected(Quest quest)
    {
        selectedQuest = quest;
        quest.hasBeenViewed = true; // Отмечаем как просмотренный
        ShowQuestDetails(quest);

        // Триггерим обновление UI (например, для иконки-булавки и уведомления)
        QuestManager.Instance.TriggerQuestLogUpdate();
    }

    private void ShowQuestDetails(Quest quest)
    {
        detailsPanelObject.SetActive(true);
        detailsTitleText.text = quest.title;
        detailsDescriptionText.text = quest.description;
        detailsRewardText.text = $"{quest.rewardXP} XP"; // Ваша награда тут

        // Формируем строку с целями
        string goalsString = "";
        foreach (var goal in quest.goals)
        {
            string status = goal.IsReached() ? "<color=green>✓</color>" : "";
            goalsString += $"{status} {GetGoalDescription(goal)}: {goal.currentAmount}/{goal.requiredAmount}\n";
        }
        detailsGoalsText.text = goalsString.TrimEnd('\n'); // Убираем лишний перенос строки
    }

    private string GetGoalDescription(QuestGoal goal)
    {
        switch (goal.goalType)
        {
            case GoalType.Gather: return $"Собрать {goal.targetID}";
            case GoalType.Buy: return $"Купить {goal.targetID}";
            case GoalType.Earn: return $"Заработать денег";
            default: return "Неизвестная цель";
        }
    }
}