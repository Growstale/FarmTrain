// SaveLoadManager.cs
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;
    private string _saveFilePath;


    private GameSaveData _gameData;

    // Сюда будем загружать данные при старте игры

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
            Debug.Log($"Path to save data {_saveFilePath}");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
       
        LoadGame();
    }


    public void LoadGame()
    {
        if (!File.Exists(_saveFilePath))
        {
            Debug.Log("Файл сохранения не найден. Загрузка не будет выполнена. Начинается новая игра.");
            return;
        }

        Debug.Log("Загрузка игры из файла...");

        // 1. Читаем JSON из файла и десериализуем
        string json = File.ReadAllText(_saveFilePath);
        _gameData = JsonUtility.FromJson<GameSaveData>(json);

        if (_gameData == null)
        {
            Debug.LogError("Не удалось загрузить данные из файла. Возможно, файл поврежден.");
            return;
        }

        // --- Запускаем процесс применения данных ---
        ApplyLoadedData();
    }


    public void SaveGame()
    {
        Debug.Log("[SaveLoadManager]  Сохранение игры...");
        _gameData = new GameSaveData();

        // --- Собираем данные с грядок (GridGenerators) ---
        var gridGenerators = FindObjectsOfType<GridGenerator>(); // Предполагаем, что скрипт грядок так называется
        foreach (var grid in gridGenerators)
        {
            _gameData.gridsData.Add(grid.GetSaveData());
        }

        // --- Собираем данные с растений (PlantController) ---
        var plants = FindObjectsOfType<PlantController>(); // Предполагаем, что скрипт на растении так называется
        foreach (var plant in plants)
        {
            _gameData.plantsData.Add(plant.GetSaveData());
        }

        // --- Сохраняем другие данные, если нужно ---
         _gameData.upgradeWatering = PlantManager.instance.UpgradeWatering;




        // --- Сериализуем в JSON и сохраняем в файл ---
        string json = JsonUtility.ToJson(_gameData, true); // true для красивого форматирования
        File.WriteAllText(_saveFilePath, json);

        Debug.Log($"[SaveLoadManager]  Игра сохранена в: {_saveFilePath}");
    }
    public void ApplyLoadedData()
    {
        
        // --- Сначала очищаем сцену от старых растений ---
        // Это важно, чтобы не было дубликатов при перезагрузке сцены
        var oldPlants = FindObjectsOfType<PlantController>();
        foreach (var plant in oldPlants)
        {
            Destroy(plant.gameObject);
        }

        // --- Находим все генераторы грядок в сцене ---
        var gridGenerators = FindObjectsOfType<GridGenerator>().ToDictionary(g => g.identifier, g => g);

        // --- Применяем состояние грядок ---
        foreach (var gridData in _gameData.gridsData)
        {
            if (gridGenerators.ContainsKey(gridData.identifier))
            {
                gridGenerators[gridData.identifier].ApplySaveData(gridData);
            }
        }

        // --- Воссоздаем растения ---
        foreach (var plantData in _gameData.plantsData)
        {
            if (gridGenerators.ContainsKey(plantData.gridIdentifier))
            {
                gridGenerators[plantData.gridIdentifier].SpawnPlantFromSave(plantData);
            }
        }

        Debug.Log("[SaveLoadManager]  Загрузка завершена.");
    }

}