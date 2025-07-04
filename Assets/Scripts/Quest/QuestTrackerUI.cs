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
        titleText.text = pinnedQuest.shortDescription;

        string goalsString = "";
        foreach (var goal in pinnedQuest.goals)
        {
            if (!goal.IsReached())
            {
                goalsString += $"{goal.currentAmount}/{goal.requiredAmount}\n";
            }
        }
        goalsText.text = goalsString;
    }
}