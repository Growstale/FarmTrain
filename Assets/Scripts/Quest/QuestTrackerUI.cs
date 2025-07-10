using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;

public class QuestTrackerUI : MonoBehaviour
{
    [SerializeField] private GameObject trackerPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI goalsText;
    [SerializeField] private Slider questTrackerSlider;
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
        titleText.text = pinnedQuest.shortDescription;

        string goalsString = "";
        float totalProgress = 0f;
        int activeGoalsCount = 0;

        foreach (var goal in pinnedQuest.goals)
        {
            if (!goal.IsReached())
            {
                goalsString += $"{goal.currentAmount}/{goal.requiredAmount}\n";
                totalProgress += (float)goal.currentAmount / goal.requiredAmount;
                activeGoalsCount++;
            }
        }
        goalsText.text = goalsString;

        if (activeGoalsCount > 0)
        {
            float averageProgress = totalProgress / activeGoalsCount;
            questTrackerSlider.value = averageProgress;
        }
        else
        {
            questTrackerSlider.value = 1f;
        }
    }
}