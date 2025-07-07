using UnityEngine;

public class AchievementButton : MonoBehaviour
{
    bool isOpen = false;
    [SerializeField] GameObject windowAchievement;
  public void WindowHandler()
    {
        if (isOpen)
        {
            windowAchievement.SetActive(false);
            isOpen = false;
        }
        else
        {
            windowAchievement.SetActive(true);
            isOpen = true;
        }
    }
}
