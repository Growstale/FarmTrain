using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using TMPro;


[System.Serializable]
public class PlayerProgress
{
    public Achievement[] playerProgress;
    public string[] unlockedAchievements;
}

[System.Serializable]
public class Achievement
{
    public int type;
    public int value;
}
public class AchievementUIManagera : MonoBehaviour
{
    private string filePath;
    private Dictionary<TypeOfAchivment, int> progress = new Dictionary<TypeOfAchivment, int>();
    [SerializeField] Slider[] allSliders;
    [SerializeField] TextMeshProUGUI[] allText;
    public List<AchievementData> AllDataAchievement;

    void Start()
    {
        filePath = Application.persistentDataPath + "/achievements.json";
        LoadData(1);
    }
    private void OnEnable()
    {
        GameEvents.OnHarvestTheCrop += LoadData;
        GameEvents.OnCollectAnimalProduct += LoadData;
        GameEvents.OnCollectCoin += LoadData;
        GameEvents.OnAddedNewAnimal += LoadData;
        GameEvents.OnCollectAllPlants += LoadData;

        GameEvents.OnAddedNewUpdgrade += LoadData;
        GameEvents.OnCompleteTheQuest += LoadData;
    }
    public void LoadData(int amount)
    {
        if (File.Exists(filePath))
        {
            string jsonData = File.ReadAllText(filePath);
            PlayerProgress playerProgress = JsonUtility.FromJson<PlayerProgress>(jsonData);
            Debug.Log("Player Progress Loaded!");

            // Заполнение словаря progress
            foreach (var achievement in playerProgress.playerProgress)
            {
                TypeOfAchivment type = (TypeOfAchivment)achievement.type;
                progress[type] = achievement.value;

            }

            for (int i = 0; i < progress.Count; i++)
            {
                allSliders[i].value = playerProgress.playerProgress[i].value;
            }
            for(int i = 0;i < allText.Length; i++)
            {
                allText[i].text = $" {playerProgress.playerProgress[i].value}/{AllDataAchievement[i].goal}";
            }

        }
        else
        {
            Debug.LogError("File not found: " + filePath);
        }

        
    }
}
