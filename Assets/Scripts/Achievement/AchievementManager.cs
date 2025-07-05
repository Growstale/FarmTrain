using System.Collections.Generic;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
   public static AchievementManager instance;

    public List<AchievementData> AllDataAchievement;

    private Dictionary<TypeOfAchivment, int> progress = new Dictionary<TypeOfAchivment, int>();




    private void Awake()
    {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeProgress();


        }
        else
        {
            Destroy(gameObject);
        }
        
    }

    private void InitializeProgress()
    {
        foreach (var item in AllDataAchievement) {
            if (progress.ContainsKey(item.typeOfAchivment)) {

                progress[item.typeOfAchivment] = 0;
            }
        
        }
        LoadProgress();
    }

    public void AddProgress(TypeOfAchivment type, int amount)
    {
        if (!progress.ContainsKey(type))
        {
            Debug.LogWarning($"Achievement type {type} not found in progress dictionary!");
            return;
        }

        progress[type] += amount;
    }

    private void LoadProgress()
    {
        Debug.Log("Loading progres...");
    }


    private void OnEnable()
    {
        GameEvents.OnAddedNewPlant += HandleAddedNewPlant;
    }
    private void HandleAddedNewPlant(int amount)
    {
        AddProgress(TypeOfAchivment.MasterGardener, amount);
    }
}
