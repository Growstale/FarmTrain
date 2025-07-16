using System.Collections;
using TMPro;
using UnityEngine;

public class AchievementWindow : MonoBehaviour
{
    public TextMeshProUGUI title;
    public TextMeshProUGUI rewardtext;
    public void GetVisibleWindow(string nameachievement,int reward)
    {
        gameObject.SetActive(true);

        title.text = "Achievement: " + nameachievement + " done!";
        rewardtext.text = "Reward: " + reward.ToString() + " BYN";
        StartCoroutine(CloseWindow());

    }

    private IEnumerator CloseWindow()
    {
        yield return new WaitForSeconds(3.5f);
        gameObject.SetActive(false);

    }
}
