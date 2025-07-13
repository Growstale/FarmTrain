using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(AudioSource))]
public class AchievementManager : MonoBehaviour
{
    #region Singleton & Fields

    public static AchievementManager instance;
    public List<AchievementData> AllDataAchievement;
    public AudioClip achievementSound;
    private AudioSource audioSource;
    private Dictionary<TypeOfAchivment, int> progress = new Dictionary<TypeOfAchivment, int>();
    private HashSet<TypeOfAchivment> completedAchievements = new HashSet<TypeOfAchivment>();
    public GameObject windowsNotification;

    // Эти списки лучше оставить здесь для логики достижений
    public static List<string> allTpyesPlant = new List<string> { "Carrot", "Berries", "Potato", "Wheat", "Corn", "Pumpkin", "Tomato" };
    public static List<string> allTpyesAnimal = new List<string> { "Cow", "Chicken", "Sheep" };

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            InitializeNewProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnHarvestTheCrop += HandleHarvestTheCrop;
        GameEvents.OnCollectAnimalProduct += HandleCollectAnimalProduct;
        GameEvents.OnCollectCoin += HandleCollectCoin;
        GameEvents.OnAddedNewAnimal += HandleAddedNewAnimal;
        GameEvents.OnCollectAllPlants += HandleCollectAllPlants;
        GameEvents.OnAddedNewUpdgrade += HandleAddedNewUpgrade;
        GameEvents.OnCompleteTheQuest += HandleCompleteTheQuest;
    }

    private void OnDisable()
    {
        GameEvents.OnHarvestTheCrop -= HandleHarvestTheCrop;
        GameEvents.OnCollectAnimalProduct -= HandleCollectAnimalProduct;
        GameEvents.OnCollectCoin -= HandleCollectCoin;
        GameEvents.OnAddedNewAnimal -= HandleAddedNewAnimal;
        GameEvents.OnCollectAllPlants -= HandleCollectAllPlants;
        GameEvents.OnAddedNewUpdgrade -= HandleAddedNewUpgrade;
        GameEvents.OnCompleteTheQuest -= HandleCompleteTheQuest;
    }

    #endregion

    public void AddProgress(TypeOfAchivment type, int amount)
    {
        if (completedAchievements.Contains(type)) return;

        AchievementData data = AllDataAchievement.Find(a => a.typeOfAchivment == type);
        if (data == null)
        {
            Debug.LogWarning($"Конфигурация для достижения типа {type} не найдена!");
            return;
        }

        if (!progress.ContainsKey(type)) progress[type] = 0;
        progress[type] += amount;

        if (progress[type] >= data.goal)
        {
            progress[type] = data.goal;
            Debug.Log($"Достижение '{data.Name}' ПОЛУЧЕНО!");
            MarkAsComplete(type, playSound: true);

            if (windowsNotification != null)
            {
                AchievementWindow win = windowsNotification.GetComponent<AchievementWindow>();
                if (win != null) win.GetVisibleWindow(data.name, data.reward);
            }

            if (data.Name == "Budding Tycoon") PlayerWallet.Instance.AchievemnetisReward = true;

            PlayerWallet.Instance.AddMoney(data.reward);
        }
        // <<< ИСПРАВЛЕНИЕ: Удаляем вызов несуществующего метода SaveProgress() >>>
        // SaveProgress(); // ЭТА СТРОКА БЫЛА УДАЛЕНА
    }

    private void MarkAsComplete(TypeOfAchivment type, bool playSound)
    {
        if (completedAchievements.Add(type))
        {
            if (playSound && audioSource != null && achievementSound != null)
            {
                audioSource.PlayOneShot(achievementSound);
            }
        }
    }

    #region Save & Load System

    // <<< ИЗМЕНЕНИЕ: Удалены все вложенные классы для сохранения >>>

    private void InitializeNewProgress()
    {
        progress.Clear();
        completedAchievements.Clear();
        foreach (var achievementData in AllDataAchievement)
        {
            if (!progress.ContainsKey(achievementData.typeOfAchivment))
            {
                progress.Add(achievementData.typeOfAchivment, 0);
            }
        }
    }

    // <<< ИЗМЕНЕНИЕ: Метод теперь возвращает глобальный тип AchievementSaveData >>>
    public AchievementSaveData GetSaveData()
    {
        var data = new AchievementSaveData
        {
            completedList = completedAchievements.ToList()
        };
        foreach (var pair in progress)
        {
            data.progressList.Add(new AchievementProgressEntry { type = pair.Key, value = pair.Value });
        }
        return data;
    }

    // <<< ИЗМЕНЕНИЕ: Метод теперь принимает глобальный тип AchievementSaveData >>>
    public void ApplySaveData(AchievementSaveData data)
    {
        if (data == null || (data.progressList.Count == 0 && data.completedList.Count == 0))
        {
            InitializeNewProgress();
            return;
        }

        progress.Clear();
        foreach (var savedProgress in data.progressList)
        {
            progress[savedProgress.type] = savedProgress.value;
        }

        completedAchievements = new HashSet<TypeOfAchivment>(data.completedList);
        Debug.Log("Прогресс достижений успешно загружен.");
    }

    #endregion

    #region Public Getters
    public int GetProgress(TypeOfAchivment type)
    {
        progress.TryGetValue(type, out int value);
        return value;
    }

    public bool IsCompleted(TypeOfAchivment type)
    {
        return completedAchievements.Contains(type);
    }
    #endregion

    #region Event Handlers
    private void HandleHarvestTheCrop(int amount) => AddProgress(TypeOfAchivment.BountifulHarvest, amount);
    private void HandleCollectAnimalProduct(int amount) => AddProgress(TypeOfAchivment.Rancher, amount);
    private void HandleCollectCoin(int amount) => AddProgress(TypeOfAchivment.BuddingTycoon, amount);
    private void HandleAddedNewAnimal(int amount) => AddProgress(TypeOfAchivment.TheWholeGangsHere, amount);
    private void HandleCollectAllPlants(int amount) => AddProgress(TypeOfAchivment.MasterGardener, amount);
    private void HandleAddedNewUpgrade(int amount) => AddProgress(TypeOfAchivment.StateoftheArtFarm, amount);
    private void HandleCompleteTheQuest(int amount) => AddProgress(TypeOfAchivment.FarmingLegend, amount);
    #endregion
}