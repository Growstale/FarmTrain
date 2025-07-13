// SaveLoadManager.cs
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;

    private string[] _saveFilePaths = new string[4];
    private string _metadataFilePath;

    private GameData _pendingDataToApply = null;

    // Свойство для доступа к текущему слоту. Теперь оно будет взаимодействовать с PlayerPrefs.
    public static int CurrentSlotID { get; private set; }

    void Awake()
    {
        Debug.Log($"<color=lime>ПАПКА С СОХРАНЕНИЯМИ: {Application.persistentDataPath}</color>");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            for (int i = 0; i < 4; i++)
            {
                _saveFilePaths[i] = Path.Combine(Application.persistentDataPath, $"save_slot_{i}.json");
            }
            _metadataFilePath = Path.Combine(Application.persistentDataPath, "metadata.json");

            // ПРИ ЗАПУСКЕ ИГРЫ: Загружаем ID последнего использованного слота из памяти устройства
            CurrentSlotID = PlayerPrefs.GetInt("LastUsedSlotID", -1); // -1, если еще ничего не сохранялось
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // --- Главные методы ---

    public void StartNewGame(int slotId)
    {
        SetCurrentSlot(slotId);

        // Создаем пустой контейнер данных для новой игры
        _pendingDataToApply = new GameData();

        // <<< ИЗМЕНЕНИЕ: Вызываем ApplyPersistentManagerData с ПУСТЫМИ данными (null) >>>
        // Это заставит менеджеры выполнить логику новой игры (выдать стартовые предметы, деньги и т.д.)
        // Мы передаем _pendingDataToApply, который только что был создан, но его поля (типа playerData) еще null.
        ApplyPersistentManagerData(_pendingDataToApply);

        // Удаляем старый файл, если он был
        if (File.Exists(_saveFilePaths[slotId]))
        {
            File.Delete(_saveFilePaths[slotId]);
        }
        UpdateSlotMetadata(slotId, false);

        SceneManager.LoadScene("SampleScene");
    }


    public void LoadGame(int slotId)
    {
        if (!File.Exists(_saveFilePaths[slotId]))
        {
            Debug.LogError($"Файл сохранения для слота {slotId} не найден!");
            return;
        }

        SetCurrentSlot(slotId); // Устанавливаем и ЗАПОМИНАЕМ выбранный слот

        string json = File.ReadAllText(_saveFilePaths[slotId]);
        _pendingDataToApply = JsonUtility.FromJson<GameData>(json);

        ApplyPersistentManagerData(_pendingDataToApply);
        SceneManager.LoadScene("SampleScene");
    }

    public void SaveGame()
    {
        if (CurrentSlotID < 0 || CurrentSlotID >= 4)
        {
            Debug.LogError("Невозможно сохранить игру, не выбран слот!");
            return;
        }

        Debug.Log($"<color=orange>Начинаю сбор данных для сохранения в слот {CurrentSlotID}...</color>");

        // 1. Создаем пустой контейнер для всех данных
        GameData data = new GameData();

        // 2. Начинаем ПОСЛЕДОВАТЕЛЬНО собирать данные со всех менеджеров

        // --- Вечные менеджеры (из Initializer) ---
        data.playerData = GatherPlayerData(); // Собирает данные из PlayerWallet и ExperienceManager
        data.inventoryItems = InventoryManager.Instance.GetSaveData();
        data.trainUpgradesData = GatherTrainUpgradeData(); // Собирает данные из TrainUpgradeManager и PlantManager

        // --- Менеджеры сцены (из SampleScene, Station, и т.д.) ---

        // Пытаемся найти QuestManager и собрать его данные
        if (QuestManager.Instance != null)
        {
            data.questData = QuestManager.Instance.GetSaveData();
            Debug.Log("<color=green>Данные квестов собраны.</color>");
        }

        // Пытаемся найти AchievementManager и собрать его данные
        if (AchievementManager.instance != null)
        {
            data.achievementData = AchievementManager.instance.GetSaveData();
            Debug.Log("<color=green>Данные достижений собраны.</color>");
        }

        // Пытаемся найти AnimalPenManager и собрать его данные
        if (AnimalPenManager.Instance != null)
        {
            data.animalData = AnimalPenManager.Instance.GetSaveData();
            Debug.Log("<color=green>Данные животных собраны.</color>");
        }

        // --- Сбор данных с объектов на сцене (Грядки и Растения) ---

        // Находим все GridGenerator-ы и собираем с них данные
        var gridGenerators = FindObjectsOfType<GridGenerator>();
        data.gridsData = new List<GridSaveData>();
        foreach (var grid in gridGenerators)
        {
            data.gridsData.Add(grid.GetSaveData());
        }
        Debug.Log($"<color=green>Собраны данные с {gridGenerators.Length} сеток (Grid).</color>");


        // Находим все PlantController-ы и собираем с них данные
        var plants = FindObjectsOfType<PlantController>();
        data.plantsData = new List<PlantSaveData>();
        foreach (var plant in plants)
        {
            PlantSaveData plantData = plant.GetSaveData();
            if (plantData != null)
            {
                data.plantsData.Add(plantData);
            }
        }
        Debug.Log($"<color=green>Собраны данные с {plants.Length} растений.</color>");


        // 3. Устанавливаем время сохранения
        data.lastSaved = DateTime.Now;

        // 4. Сериализуем всё в JSON и записываем в файл
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(_saveFilePaths[CurrentSlotID], json);

        // 5. Обновляем метаданные для меню
        UpdateSlotMetadata(CurrentSlotID, true, data);

        Debug.Log($"<color=orange>Игра успешно сохранена в файл: {_saveFilePaths[CurrentSlotID]}</color>");
    }


    // НОВЫЙ МЕТОД для установки и сохранения слота
    private void SetCurrentSlot(int slotId)
    {
        CurrentSlotID = slotId;
        // Сохраняем выбор в память устройства, чтобы он не потерялся после перезапуска
        PlayerPrefs.SetInt("LastUsedSlotID", slotId);
        PlayerPrefs.Save(); // Немедленно записываем на диск
        Debug.Log($"Текущий слот установлен: {CurrentSlotID}");
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "SampleScene" && _pendingDataToApply != null)
        {
            StartCoroutine(ApplySceneSpecificDataCoroutine());
        }
    }

    private System.Collections.IEnumerator ApplySceneSpecificDataCoroutine()
    {
        yield return null;

        Debug.Log("Применение данных к менеджерам сцены SampleScene...");

        AnimalPenManager.Instance.ApplySaveData(_pendingDataToApply.animalData);
        QuestManager.Instance.ApplySaveData(_pendingDataToApply.questData);
        AchievementManager.instance.ApplySaveData(_pendingDataToApply.achievementData); // <<< Теперь типы совпадают

        var gridGenerators = FindObjectsOfType<GridGenerator>().ToDictionary(g => g.identifier, g => g);
        foreach (var gridData in _pendingDataToApply.gridsData)
        {
            if (gridGenerators.TryGetValue(gridData.identifier, out var grid))
            {
                grid.ApplySaveData(gridData);
            }
        }

        foreach (var plant in FindObjectsOfType<PlantController>())
        {
            Destroy(plant.gameObject);
        }

        foreach (var plantData in _pendingDataToApply.plantsData)
        {
            if (gridGenerators.TryGetValue(plantData.gridIdentifier, out var grid))
            {
                grid.SpawnPlantFromSave(plantData);
            }
        }

        PlantManager.instance.ApplySaveData(_pendingDataToApply.trainUpgradesData);
        TrainUpgradeManager.Instance.ApplySaveData(_pendingDataToApply.trainUpgradesData);

        _pendingDataToApply = null;
        Debug.Log("Применение данных завершено.");
    }

    private void ApplyPersistentManagerData(GameData data)
    {
        PlayerWallet.Instance.ApplySaveData(data.playerData);
        ExperienceManager.Instance.ApplySaveData(data.playerData);
        InventoryManager.Instance.ApplySaveData(data.inventoryItems);
    }

    private PlayerSaveData GatherPlayerData()
    {
        return new PlayerSaveData
        {
            currentMoney = PlayerWallet.Instance.GetCurrentMoney(),
            currentLevel = ExperienceManager.Instance.CurrentLevel,
            currentPhase = ExperienceManager.Instance.CurrentPhase,
            currentXP = ExperienceManager.Instance.CurrentXP
        };
    }

    private TrainUpgradesSaveData GatherTrainUpgradeData()
    {
        return new TrainUpgradesSaveData
        {
            purchasedUpgradeItemNames = TrainUpgradeManager.Instance.GetSaveData(),
            upgradeWatering = PlantManager.instance.UpgradeWatering
        };
    }

    public AllSlotsMetadata GetAllSlotsMetadata()
    {
        if (!File.Exists(_metadataFilePath))
        {
            var emptyMeta = new AllSlotsMetadata();
            for (int i = 0; i < 4; i++) emptyMeta.slots.Add(new SaveSlotMetadata());
            File.WriteAllText(_metadataFilePath, JsonUtility.ToJson(emptyMeta));
            return emptyMeta;
        }
        string json = File.ReadAllText(_metadataFilePath);
        return JsonUtility.FromJson<AllSlotsMetadata>(json);
    }

    private void UpdateSlotMetadata(int slotId, bool isUsed, GameData data = null)
    {
        var allMeta = GetAllSlotsMetadata();
        if (isUsed && data != null)
        {
            allMeta.slots[slotId].isUsed = true;
            allMeta.slots[slotId].saveTime = data.lastSaved;
            allMeta.slots[slotId].playerLevel = data.playerData.currentLevel;
            allMeta.slots[slotId].stationName = $"Level {data.playerData.currentLevel}";
        }
        else
        {
            allMeta.slots[slotId].isUsed = false;
        }

        string json = JsonUtility.ToJson(allMeta, true);
        File.WriteAllText(_metadataFilePath, json);
    }
}