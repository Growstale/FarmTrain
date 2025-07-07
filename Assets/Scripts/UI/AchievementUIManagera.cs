using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;

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
    [SerializeField] Text[] allText;

    void Start()
    {
        filePath = Path.Combine(Application.streamingAssetsPath, "achievements.json");
        LoadData();
    }

    void LoadData()
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
                allSliders.SetValue(progress[0], 0);
            }

        }
        else
        {
            Debug.LogError("File not found: " + filePath);
        }

        
    }
}
