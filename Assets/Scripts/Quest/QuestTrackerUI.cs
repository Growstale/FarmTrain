using UnityEngine;
using TMPro;
using System.Linq;

public class QuestTrackerUI : MonoBehaviour
{
    [SerializeField] private GameObject trackerPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI goalsText;

    void Start()
    {
        QuestManager.Instance.OnQuestLogUpdated += UpdateTracker;
        trackerPanel.SetActive(false);
        UpdateTracker();
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestLogUpdated -= UpdateTracker;
        }
    }

    private void UpdateTracker()
    {
        var pinnedQuest = QuestManager.Instance.ActiveQuests.FirstOrDefault(q => q.isPinned);

        if (pinnedQuest == null)
        {
            trackerPanel.SetActive(false);
            return;
        }

        trackerPanel.SetActive(true);
        titleText.text = pinnedQuest.title;

        string goalsString = "";
        foreach (var goal in pinnedQuest.goals)
        {
            if (!goal.IsReached())
            {
                goalsString += $"{GetGoalDescription(goal)}: {goal.currentAmount}/{goal.requiredAmount}\n";
            }
        }
        goalsText.text = goalsString;
    }

    private string GetGoalDescription(QuestGoal goal)
    {
        switch (goal.goalType)
        {
            case GoalType.Gather: return $"Собрать {goal.targetID}";
            case GoalType.Buy: return $"Купить {goal.targetID}";
            case GoalType.Earn: return $"Заработать";
            default: return "Цель";
        }
    }
}