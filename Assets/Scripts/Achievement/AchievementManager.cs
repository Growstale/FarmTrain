using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq; // Добавлено для удобной конвертации HashSet в List

// Атрибут для автоматического добавления AudioSource, если его нет на объекте
[RequireComponent(typeof(AudioSource))]
public class AchievementManager : MonoBehaviour
{
    #region Singleton & Fields

    public static AchievementManager instance;

    // Сюда в инспекторе перетащите все ваши ScriptableObject-ассеты достижений
    public List<AchievementData> AllDataAchievement;

    // Звук, который проигрывается при получении достижения
    public AudioClip achievementSound;

    // Ссылка на компонент для проигрывания звука
    private AudioSource audioSource;

    // --- Runtime Data (Состояние игрока) ---
    // Хранит текущий прогресс (например, 5/10 собранных морковок)
    private Dictionary<TypeOfAchivment, int> progress = new Dictionary<TypeOfAchivment, int>();
    // Хранит типы достижений, которые уже были ПОЛУЧЕНЫ. HashSet для очень быстрых проверок.
    private HashSet<TypeOfAchivment> completedAchievements = new HashSet<TypeOfAchivment>();


    public GameObject windowsNotification;

    public static List<string> allTpyesPlant = new List<string> { "Carrot", "Berries", "Potato", "Wheat", "Corn", "Pumpkin", "Tomato" };
    public static List<string> allTpyesAnimal = new List<string> { "Cow", "Chicken", "Sheep" };

    // Путь к файлу сохранения
    private string saveFilePath;

    #endregion




    #region Unity Lifecycle Methods

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Инициализация компонентов и данных
            audioSource = GetComponent<AudioSource>();
            saveFilePath = Path.Combine(Application.persistentDataPath, "achievements.json");

            LoadOrInitializeProgress();
        }
        else
        {
            Debug.LogWarning($"Destroying duplicate AchievementManager on GameObject {gameObject.name}");
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Подписываемся на все игровые события
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
        // Всегда отписываемся от событий, чтобы избежать утечек памяти
        GameEvents.OnHarvestTheCrop -= HandleHarvestTheCrop;
        GameEvents.OnCollectAnimalProduct -= HandleCollectAnimalProduct;
        GameEvents.OnCollectCoin -= HandleCollectCoin;
        GameEvents.OnAddedNewAnimal -= HandleAddedNewAnimal;
        GameEvents.OnCollectAllPlants -= HandleCollectAllPlants;
        GameEvents.OnAddedNewUpdgrade -= HandleAddedNewUpgrade;
        GameEvents.OnCompleteTheQuest -= HandleCompleteTheQuest;
    }

    #endregion

    #region Public Methods

 

    public void AddProgress(TypeOfAchivment type, int amount)
    {
        // 1. Если достижение уже получено, выходим
        if (completedAchievements.Contains(type))
        {
            return;
        }

        // 2. Находим данные для этого достижения
        AchievementData data = AllDataAchievement.Find(a => a.typeOfAchivment == type);
        if (data == null)
        {
            Debug.LogWarning($"Конфигурация для достижения типа {type} не найдена в списке AllDataAchievement!");
            return;
        }

        // 3. Обновляем прогресс
        if (!progress.ContainsKey(type))
        {
            progress[type] = 0;
        }
        progress[type] += amount;


        // 4. Проверяем, выполнено ли условие
        if (progress[type] >= data.goal)
        {
            // Фиксируем прогресс на максимальном значении для корректного отображения в UI
            progress[type] = data.goal;

            Debug.Log($"Достижение '{data.Name}' ПОЛУЧЕНО!");
            MarkAsComplete(type, playSound: true);
            Debug.Log($"Прогресс для {data.Name}: {progress[type]} / {data.goal}");
            if (windowsNotification != null)
            {
                AchievementWindow win = windowsNotification.GetComponent<AchievementWindow>();
                if (win != null)
                {
                    win.GetVisibleWindow(data.name, data.reward);
                }
            }
            else
            {
                Debug.LogError("windowsNotification is Null");
            }

            if(data.Name == "Budding Tycoon")
            {
                
                PlayerWallet.Instance.AchievemnetisReward = true;
            }
            // Выдаем награду (предполагая, что поле reward есть в AchievementData)
            PlayerWallet.Instance.AddMoney(data.reward);

            // Отмечаем достижение как выполненное

        }

        // 5. Сохраняем все изменения
        SaveProgress();
    }

    #endregion

    #region Private Logic

    /// <summary>
    /// Отмечает достижение как выполненное и проигрывает звук.
    /// </summary>
    private void MarkAsComplete(TypeOfAchivment type, bool playSound)
    {
        // .Add() для HashSet возвращает true, если элемент был успешно добавлен (т.е. его там не было)
        if (completedAchievements.Add(type))
        {
            if (playSound && audioSource != null && achievementSound != null)
            {
                audioSource.PlayOneShot(achievementSound);
            }
        }
    }

    #endregion

    #region Save & Load System

    // Классы для сериализации данных в JSON
    [System.Serializable]
    private class SaveDataProgress
    {
        public List<SerializableProgress> progressList = new List<SerializableProgress>();
        public List<TypeOfAchivment> completedList = new List<TypeOfAchivment>();
    }

    [System.Serializable]
    private struct SerializableProgress
    {
        public TypeOfAchivment type;
        public int value;
    }

    /// <summary>
    /// Пытается загрузить прогресс из файла. Если файл не найден, инициализирует новый прогресс.
    /// </summary>
    private void LoadOrInitializeProgress()
    {
        if (File.Exists(saveFilePath))
        {
            InitializeNewProgress();
        }
        else
        {
            InitializeNewProgress();
            // Сразу сохраняем, чтобы создать файл сохранения
            SaveProgress();
            Debug.Log("Файл сохранений не найден. Создан новый файл достижений.");
        }
    }

    /// <summary>
    /// Загружает данные из JSON файла и восстанавливает состояние менеджера.
    /// </summary>
    private void LoadProgressFromFile()
    {
        string json = File.ReadAllText(saveFilePath);
        SaveDataProgress data = JsonUtility.FromJson<SaveDataProgress>(json);

        // Восстанавливаем словарь прогресса
        progress.Clear();
        foreach (var savedProgress in data.progressList)
        {
            progress[savedProgress.type] = savedProgress.value;
        }

        // Восстанавливаем HashSet полученных достижений
        completedAchievements = new HashSet<TypeOfAchivment>(data.completedList);

        Debug.Log("Прогресс достижений успешно загружен.");
    }

    /// <summary>
    /// Инициализирует прогресс для новой игры.
    /// </summary>
    private void InitializeNewProgress()
    {
        progress.Clear();
        completedAchievements.Clear();

        // Заполняем словарь прогресса нулями для всех известных достижений
        foreach (var achievementData in AllDataAchievement)
        {
            if (!progress.ContainsKey(achievementData.typeOfAchivment))
            {
                progress.Add(achievementData.typeOfAchivment, 0);
            }
        }
    }

    /// <summary>
    /// Сохраняет текущий прогресс и список выполненных достижений в JSON файл.
    /// </summary>
    private void SaveProgress()
    {
        SaveDataProgress data = new SaveDataProgress();

        // Конвертируем словарь и HashSet в списки для сериализации
        foreach (var pair in progress)
        {
            data.progressList.Add(new SerializableProgress { type = pair.Key, value = pair.Value });
        }
        data.completedList = completedAchievements.ToList();

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);

        // Этот лог может быть слишком частым, можно закомментировать после отладки
        // Debug.Log("Прогресс достижений сохранен.");
    }

    #endregion

    #region Public Getters for UI & Other Systems

    /// <summary>
    /// Возвращает текущий числовой прогресс для указанного типа достижения.
    /// </summary>
    /// <param name="type">Тип достижения, для которого нужен прогресс.</param>
    /// <returns>Текущее значение прогресса, или 0, если прогресс еще не начался.</returns>
    public int GetProgress(TypeOfAchivment type)
    {
        // TryGetValue - это безопасный и быстрый способ получить значение из словаря.
        // Он не вызовет ошибку, если ключа не существует.
        if (progress.TryGetValue(type, out int value))
        {
            return value;
        }

        // Если по какой-то причине для этого типа нет записи в словаре,
        // безопасно возвращаем 0.
        Debug.LogWarning($"Запрошен прогресс для типа {type}, но он не найден в словаре. Возвращено 0.");
        return 0;
    }

    /// <summary>
    /// Проверяет, было ли достижение указанного типа уже получено (выполнено).
    /// </summary>
    /// <param name="type">Тип достижения для проверки.</param>
    /// <returns>True, если достижение выполнено, иначе false.</returns>
    public bool IsCompleted(TypeOfAchivment type)
    {
        // Проверка на наличие элемента в HashSet - это очень быстрая операция (O(1)).
        // Гораздо эффективнее, чем перебирать список.
        return completedAchievements.Contains(type);
    }

    #endregion

    #region Event Handlers
    // Используем лямбда-выражения для краткости, т.к. они просто вызывают один метод
    private void HandleHarvestTheCrop(int amount) => AddProgress(TypeOfAchivment.BountifulHarvest, amount);
    private void HandleCollectAnimalProduct(int amount) => AddProgress(TypeOfAchivment.Rancher, amount);
    private void HandleCollectCoin(int amount) => AddProgress(TypeOfAchivment.BuddingTycoon, amount);
    private void HandleAddedNewAnimal(int amount) => AddProgress(TypeOfAchivment.TheWholeGangsHere, amount);
    private void HandleCollectAllPlants(int amount) => AddProgress(TypeOfAchivment.MasterGardener, amount);
    private void HandleAddedNewUpgrade(int amount) => AddProgress(TypeOfAchivment.StateoftheArtFarm, amount);
    private void HandleCompleteTheQuest(int amount) => AddProgress(TypeOfAchivment.FarmingLegend, amount);
    #endregion
}