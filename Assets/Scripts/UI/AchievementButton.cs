using UnityEngine;

public class AchievementButton : MonoBehaviour, IUIManageable
{
    bool isOpen = false;
    [SerializeField] GameObject windowAchievement;

    void Start()
    {
        if (ExclusiveUIManager.Instance != null)
        {
            ExclusiveUIManager.Instance.Register(this);
        }
        if (windowAchievement != null)
        {
            windowAchievement.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (ExclusiveUIManager.Instance != null)
        {
            ExclusiveUIManager.Instance.Deregister(this);
        }
    }

    public void WindowHandler()
    {
        if (isOpen)
        {
            CloseUI();
        }
        else
        {
            ExclusiveUIManager.Instance.NotifyPanelOpening(this);
            windowAchievement.SetActive(true);
            isOpen = true;
        }
    }

    public void CloseUI()
    {
        if (isOpen)
        {
            windowAchievement.SetActive(false);
            isOpen = false;
        }
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}
