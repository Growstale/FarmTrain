using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class QuestLogUI : MonoBehaviour
{
    // --- ПРИВЯЗКИ К UI В ИНСПЕКТОРЕ ---
    [Header("Main Components")]
    [SerializeField] private GameObject questLogPanelObject;
    [SerializeField] private Button openLogButton;
    [SerializeField] private Button closeLogButton;

    [Header("Quest List")]
    [SerializeField] private GameObject questEntryPrefab;
    [SerializeField] private Transform questListContentContainer;

    [Header("Quest Details (Right Panel)")]
    [SerializeField] private GameObject detailsPanelObject; // Эта панель будет скрываться
    [SerializeField] private TextMeshProUGUI detailsTitleText;
    [SerializeField] private TextMeshProUGUI detailsDescriptionText;
    [SerializeField] private TextMeshProUGUI detailsGoalsText;
    [SerializeField] private TextMeshProUGUI detailsRewardText;

    private List<GameObject> spawnedEntries = new List<GameObject>();
    private Quest selectedQuest; // По умолчанию он и так null

    private void Start()
    {
        openLogButton.onClick.AddListener(ToggleLog);
        closeLogButton.onClick.AddListener(CloseLog);

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestLogUpdated += UpdateUI;
        }

        questLogPanelObject.SetActive(false);
        detailsPanelObject.SetActive(false); // Убеждаемся, что детали скрыты при старте
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

        if (!isActive) // Если только что открыли журнал
        {
            GameStateManager.Instance.RequestPause(this);
            selectedQuest = null;
            detailsPanelObject.SetActive(false);
            UpdateUI(); // Обновляем только список
        }
        else // Если только что закрыли
        {
            // <<< ИЗМЕНЕНИЕ: Снимаем игру с паузы
            GameStateManager.Instance.RequestResume(this);
        }
    }

    private void CloseLog()
    {
        if (questLogPanelObject.activeSelf)
        {
            questLogPanelObject.SetActive(false);
            GameStateManager.Instance.RequestResume(this);
        }
    }

    // Этот метод теперь отвечает ТОЛЬКО за обновление списка квестов
    private void UpdateUI()
    {
        if (!questLogPanelObject.activeSelf) return;
        PopulateQuestList();
    }

    private void PopulateQuestList()
    {
        foreach (var entry in spawnedEntries)
        {
            Destroy(entry);
        }
        spawnedEntries.Clear();

        var allVisibleQuests = QuestManager.Instance.ActiveQuests.Concat(QuestManager.Instance.CompletedQuests).ToList();

        foreach (var quest in allVisibleQuests)
        {
            GameObject entryGO = Instantiate(questEntryPrefab, questListContentContainer);
            entryGO.GetComponent<QuestLogEntryUI>().Setup(quest, OnQuestSelected);
            spawnedEntries.Add(entryGO);
        }
    }

    // Этот метод вызывается из QuestLogEntryUI, когда мы кликаем на квест
    private void OnQuestSelected(Quest quest)
    {
        selectedQuest = quest;
        quest.hasBeenViewed = true;

        // <<< ИЗМЕНЕНИЕ 2: Теперь ТОЛЬКО этот метод решает, показывать детали или нет
        ShowQuestDetails(quest);

        QuestManager.Instance.TriggerQuestLogUpdate();
    }

    // Этот метод почти не изменился, просто отвечает за заполнение полей
    private void ShowQuestDetails(Quest quest)
    {
        // Показываем панель, только если нам передали действительный квест
        if (quest == null)
        {
            detailsPanelObject.SetActive(false);
            return;
        }

        detailsPanelObject.SetActive(true);
        detailsTitleText.text = quest.title;
        detailsDescriptionText.text = quest.description;
        detailsRewardText.text = $"Reward: {quest.rewardXP}";

        // Формируем строку с целями
        string goalsString = "";
        foreach (var goal in quest.goals)
        {
            goalsString += $"{goal.currentAmount}/{goal.requiredAmount}\n";
        }
        detailsGoalsText.text = goalsString.TrimEnd('\n'); // Убираем лишний перенос строки
    }
}