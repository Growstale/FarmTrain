using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class AchievementManager : MonoBehaviour
{
    public static AchievementManager instance;

    public List<AchievementData> AllDataAchievement;

    private Dictionary<TypeOfAchivment, int> progress = new Dictionary<TypeOfAchivment, int>();





    public static List<string> allTpyesPlant = new List<string> { "Carrot", "Berries", "Potato", "Wheat", "Corn", "Pumpkin", "Tomato" };
    public static List<string> allTpyesAnimal = new List<string> { "Cow", "Chicken", "Sheep" };

    public AudioClip achievementSound;
    private AudioSource audioSource;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeProgress();

            audioSource = GetComponent<AudioSource>();
            LoadProgress();
        }
        else
        {
            Debug.LogWarning($"Destroyed gaameobject {gameObject.name}");
            Destroy(gameObject);
        }

    }

    private void InitializeProgress()
    {
        foreach (var item in AllDataAchievement)
        {

            progress[item.typeOfAchivment] = 0;


        }

    }

    public void AddProgress(TypeOfAchivment type, int amount)
    {
        if (!progress.ContainsKey(type))
        {
            Debug.LogWarning($"Achievement type {type} not found in progress dictionary!");
            return;
        }




        switch (type)
        {
            case TypeOfAchivment.MasterGardener:
                if (CheckForComplete(type))
                {
                    if (progress[type] >= 6)
                    {
                        progress[type] = 7;
                        Debug.Log($"Achievemnet {type} recieve");
                        SwitchToComplete(type);
                        PlayerWallet.Instance.AddMoney(200);
                    }
                    else
                    {
                        progress[type] += amount;
                        Debug.Log($"Progress {type} is amount: {progress[type]} ! ");
                    }
                }
                else
                {
                    Debug.Log("Type is complite");
                }
                break;
            case TypeOfAchivment.TheWholeGangsHere:
                if (CheckForComplete(type))
                {
                    if (progress[type] >= 2)
                    {

                        progress[type] = 3;
                        Debug.Log($"Achievemnet {type} recieve!!");
                        SwitchToComplete(type);
                        PlayerWallet.Instance.AddMoney(300);
                    }
                    else
                    {
                        progress[type] += amount;
                        Debug.Log($"Progress {type} is amount: {progress[type]} ! ");
                    }
                }
                break;
            case TypeOfAchivment.BuddingTycoon:
                if (CheckForComplete(type))
                {
                    if (progress[type] >= 500)
                    {
                        progress[type] = 500;
                        Debug.Log($"Achievemnet {type} recieve!!");
                        SwitchToComplete(type);
                        PlayerWallet.Instance.AddMoney(150);
                    }
                    else
                    {
                        progress[type] += amount;
                        Debug.Log($"Progress {type} is amount: {progress[type]} ! ");
                    }
                }

                break;
            case TypeOfAchivment.Rancher:
                if (CheckForComplete(type))
                {
                    if (progress[type] >= 19)
                    {
                        progress[type] = 20;
                        Debug.Log($"Achievemnet {type} recieve!!");
                        SwitchToComplete(type);
                        PlayerWallet.Instance.AddMoney(100);
                    }
                    else
                    {
                        progress[type] += amount;
                        Debug.Log($"Progress {type} is amount: {progress[type]} ! ");
                    }

                }
                break;
            case TypeOfAchivment.StateoftheArtFarm:
                if (CheckForComplete(type))
                {
                    if (progress[type] >= 9)
                    {
                        progress[type] = 10;
                        Debug.Log($"Achievemnet {type} recieve!!");
                        SwitchToComplete(type);
                        PlayerWallet.Instance.AddMoney(800);
                    }
                    else
                    {
                        progress[type] += amount;
                        Debug.Log($"Progress {type} is amount: {progress[type]} ! ");
                    } 
                }
                break;
        }
        SaveProgress();
    }

    private void LoadProgress()
    {
        string path = Application.persistentDataPath + "/achievements.json";
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // Загружаем прогресс
            progress.Clear();
            foreach (var savedProgress in data.playerProgress)
            {
                progress[savedProgress.type] = savedProgress.value;
            }


            Debug.Log("Progress Loaded!");
        }
    }


    private bool CheckForComplete(TypeOfAchivment type)
    {
        foreach (var achievement in AllDataAchievement)
        {
            if (achievement.typeOfAchivment == type)
            {
                if (achievement.isReceived) return false;
                else return true;
            }
        }
        Debug.LogWarning($"{type} is not found in AllDataAchievement");
        return false;
    }
    private void SwitchToComplete(TypeOfAchivment type)
    {
        foreach (var achievement in AllDataAchievement)
        {
            if (achievement.typeOfAchivment == type)
            {
                achievement.isReceived = true;
               audioSource.PlayOneShot(achievementSound);
            }
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
    private void HandleCollectAllPlants(int amount)
    {
       
        AddProgress(TypeOfAchivment.MasterGardener, amount);

    }
    private void HandleHarvestTheCrop(int amount)
    {
        AddProgress(TypeOfAchivment.BountifulHarvest, amount);
    }
    private void HandleCollectAnimalProduct(int amount)
    {
        AddProgress(TypeOfAchivment.Rancher, amount);
    }
    private void HandleAddedNewAnimal(int amount)
    {
        AddProgress(TypeOfAchivment.TheWholeGangsHere, amount);
    }
    private void HandleCollectCoin(int amount)
    {
        AddProgress(TypeOfAchivment.BuddingTycoon, amount);
    }
    private void HandleAddedNewUpgrade(int amount)
    {
        AddProgress(TypeOfAchivment.StateoftheArtFarm, amount);
    }

    private void HandleCompleteTheQuest(int amount)
    {
        AddProgress(TypeOfAchivment.FarmingLegend, amount);
    }

    // Êëàññ äëÿ ñîõðàíåíèÿ
    [System.Serializable]
    private class SaveData
    {
        public List<SerializableProgress> playerProgress = new List<SerializableProgress>();
        public List<string> unlockedAchievements = new List<string>();
    }

    [System.Serializable]
    private struct SerializableProgress
    {
        public TypeOfAchivment type;
        public int value;
    }
    private void SaveProgress()
    {

        SaveData data = new SaveData();
        foreach (var pair in progress)
        {
            data.playerProgress.Add(new SerializableProgress { type = pair.Key, value = pair.Value });
        }
        string json = JsonUtility.ToJson(data, true);
        string folderPath = Application.persistentDataPath;

        // 2. Указываем имя нашего файла.
        string fileName = "achievements.json";

        // 3. Соединяем путь к папке и имя файла в один полный путь.
        // Это самый надежный способ!
        string fullPath = Path.Combine(folderPath, fileName);

        // 4. (Очень полезно для отладки!) Выводим финальный путь в консоль.
        Debug.Log("Сохраняю данные по пути: " + fullPath);

        // 5. Сохраняем файл по полному, корректному пути.
        File.WriteAllText(fullPath, json);
        Debug.Log("Progress Saved!");
    }

    public void TestSave()
    {
        Debug.Log("--- НАЖАТА КНОПКА ТЕСТОВОГО СОХРАНЕНИЯ ---");

        if (PlantManager.instance != null)
        {
            PlantManager.instance.SaveStateToMemory();
        }
        else
        {
            Debug.LogError("Не удалось выполнить тестовое сохранение: PlantManager.instance не найден!");
        }
    }
}
