// SaveLoadManager.cs
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;
    private string saveFilePath;

    // Сюда будем загружать данные при старте игры
    public static GameSaveData LoadedData { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Path.Combine(Application.persistentDataPath, "gamedata.json");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveGame()
    {
        Debug.Log("Сохранение игры...");
        GameSaveData saveData = new GameSaveData();

        // 1. Сохраняем данные из PlantManager
        saveData.upgradeWatering = PlantManager.instance.UpgradeWatering;

        // 2. Находим все GridGenerator'ы на сцене
        GridGenerator[] gridGenerators = FindObjectsOfType<GridGenerator>();
        foreach (var grid in gridGenerators)
        {
            // Сохраняем данные сетки и ее слотов
            GridSaveData gridData = new GridSaveData { identifier = grid.gameObject.name };
            foreach (var slotEntry in grid.gridObjects)
            {
                SlotScripts slotScript = slotEntry.Value.GetComponent<SlotScripts>();
                SlotSaveData slotData = new SlotSaveData
                {
                    gridPosition = slotEntry.Key,
                    isPlanted = slotScript.isPlanted,
                    ishavebed = slotScript.ishavebed,
                    isRaked = slotScript.isRaked
                };
                gridData.slotsData.Add(slotData);
            }
            saveData.gridsData.Add(gridData);

            // 3. Сохраняем данные о растениях, которые являются дочерними для этого грида
            PlantController[] plants = grid.GetComponentsInChildren<PlantController>();
            foreach (var plant in plants)
            {
                // Для этого метода вам нужно будет доработать PlantController 
                saveData.plantsData.Add(plant.GetSaveData());
            }
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Игра сохранена в: " + saveFilePath);
    }

    public bool LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            Debug.Log("Загрузка сохранения...");
            string json = File.ReadAllText(saveFilePath);
            LoadedData = JsonUtility.FromJson<GameSaveData>(json);
            Debug.Log("Сохранение успешно загружено в память.");
            return true;
        }

        Debug.LogWarning("Файл сохранения не найден.");
        LoadedData = null;
        return false;
    }
}