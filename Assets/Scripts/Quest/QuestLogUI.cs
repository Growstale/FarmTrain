using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Audio;

public class QuestLogUI : MonoBehaviour, IUIManageable
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
    [SerializeField] private Slider questProgressSlider;

    [Header("Text Materials")]
    [SerializeField] private Material normalTextMaterial;
    [SerializeField] private Material pressedTextMaterial;

    private List<GameObject> spawnedEntries = new List<GameObject>();
    private Quest selectedQuest; // По умолчанию он и так null

    [SerializeField] private AudioClip selectQuestSound;
    private AudioSource audioSource;
    public static QuestLogUI Instance { get; private set; }

    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (ExclusiveUIManager.Instance != null)
        {
            ExclusiveUIManager.Instance.Register(this);
        }

        audioSource = GetComponent<AudioSource>();
        openLogButton.onClick.AddListener(ToggleLog);
        closeLogButton.onClick.AddListener(CloseLog);

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestLogUpdated += UpdateUI;
        }

        questLogPanelObject.SetActive(false);
        detailsPanelObject.SetActive(false);
    }

    public void ToggleLog()
    {
        bool isActive = questLogPanelObject.activeSelf;

        if (!isActive)
        {
            ExclusiveUIManager.Instance.NotifyPanelOpening(this);
            questLogPanelObject.SetActive(true);
            GameStateManager.Instance.RequestPause(this);

            UpdateUI();

            if (selectedQuest != null)
            {
                // Проверяем, существует ли еще этот квест
                var allQuests = QuestManager.Instance.ActiveQuests.Concat(QuestManager.Instance.CompletedQuests);
                if (allQuests.Contains(selectedQuest))
                {
                    ShowQuestDetails(selectedQuest);
                    var selectedEntry = spawnedEntries.FirstOrDefault(e =>
                        e.GetComponent<QuestLogEntryUI>()?.assignedQuest == selectedQuest);
                    if (selectedEntry != null)
                    {
                        selectedEntry.GetComponent<QuestLogEntryUI>().SetSelected(true);
                    }
                }
                else
                {
                    selectedQuest = null;
                    detailsPanelObject.SetActive(false);
                }
            }
        }
        else
        {
            CloseLog();
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

        if (selectedQuest != null && selectedQuest.status == QuestStatus.Completed)
        {
            selectedQuest = null;
            detailsPanelObject.SetActive(false);
        }

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
            var entryUI = entryGO.GetComponent<QuestLogEntryUI>();
            entryUI.Setup(quest, OnQuestSelected);

            if (quest == selectedQuest)
            {
                entryUI.SetSelected(true);
            }

            spawnedEntries.Add(entryGO);
        }
    }

    // Этот метод вызывается из QuestLogEntryUI, когда мы кликаем на квест
    private void OnQuestSelected(Quest quest)
    {
        // Если кликаем на уже выбранный квест - переключаем состояние
        if (selectedQuest == quest)
        {
            // Находим запись и сбрасываем выделение
            var currentEntry = spawnedEntries.FirstOrDefault(e => e.GetComponent<QuestLogEntryUI>()?.assignedQuest == quest);
            if (currentEntry != null)
            {
                currentEntry.GetComponent<QuestLogEntryUI>().SetSelected(false);
            }

            selectedQuest = null;
            detailsPanelObject.SetActive(false);
            QuestManager.Instance.TriggerQuestLogUpdate();
            return;
        }

        // Снимаем выделение со всех записей
        foreach (var entry in spawnedEntries)
        {
            var entryUI = entry.GetComponent<QuestLogEntryUI>();
            if (entryUI != null)
            {
                entryUI.SetSelected(false);
            }
        }

        // Устанавливаем новый выбранный квест
        selectedQuest = quest;
        quest.hasBeenViewed = true;

        // Находим и выделяем новую запись
        var selectedEntry = spawnedEntries.FirstOrDefault(e => e.GetComponent<QuestLogEntryUI>()?.assignedQuest == quest);
        if (selectedEntry != null)
        {
            selectedEntry.GetComponent<QuestLogEntryUI>().SetSelected(true);
        }

        ShowQuestDetails(quest);
        QuestManager.Instance.TriggerQuestLogUpdate();

        if (audioSource != null)
        {
            audioSource.PlayOneShot(selectQuestSound);
        }
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
        float totalProgress = 0f;
        int activeGoalsCount = 0;
        bool allGoalsCompleted = true;

        foreach (var goal in quest.goals)
        {
            goalsString += $"{goal.currentAmount} / {goal.requiredAmount}\n";

            totalProgress += (float)goal.currentAmount / goal.requiredAmount;
            Debug.Log($"Загруженный текст цели: '{goal.requiredAmount}'");

            activeGoalsCount++;
            allGoalsCompleted = false;
        }
        detailsGoalsText.text = goalsString.TrimEnd('\n'); // Убираем лишний перенос строки
        Debug.Log($"detailsGoalsText.text: '{detailsGoalsText.text}'");
        if (quest.goals.Count > 0)
        {
            if (allGoalsCompleted)
            {
                questProgressSlider.value = 1f;
            }
            else
            {
                questProgressSlider.value = totalProgress / activeGoalsCount;
            }
        }
    }

    public void CloseUI()
    {
        CloseLog();
    }

    public bool IsOpen()
    {
        return questLogPanelObject.activeSelf;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (ExclusiveUIManager.Instance != null)
        {
            ExclusiveUIManager.Instance.Deregister(this);
        }
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestLogUpdated -= UpdateUI;
        }
    }

    public void OpenLogAndSelectQuest(Quest questToSelect)
    {
        // Если панель уже открыта, просто выбираем нужный квест. Эта часть работает правильно.
        if (questLogPanelObject.activeSelf)
        {
            OnQuestSelected(questToSelect);
            return;
        }

        // 1. Сначала открываем панель
        ExclusiveUIManager.Instance.NotifyPanelOpening(this);
        questLogPanelObject.SetActive(true);
        GameStateManager.Instance.RequestPause(this);

        // 2. <<< ГЛАВНОЕ ИЗМЕНЕНИЕ: Принудительно обновляем UI и строим список квестов.
        // Метод UpdateUI вызовет PopulateQuestList, который создаст все нужные строчки.
        UpdateUI();

        // 3. ТЕПЕРЬ, когда список гарантированно создан, мы можем безопасно выбрать нужный квест.
        OnQuestSelected(questToSelect);
    }

}