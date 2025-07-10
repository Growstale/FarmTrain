using System.Collections.Generic;
using UnityEngine;

public class PlantManager : MonoBehaviour
{
    public static PlantManager instance;

    public bool UpgradeWatering;
    public ItemData _UpgradeData;

    public Dictionary<int, List<Vector2Int>> positionBed;

    public static GameSaveData SessionData { get; private set; }


    public static bool ShouldLoadSessionData { get; set; } = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Применяем загруженные данные, если они есть
        // и если мы вернулись на сцену, с которой ушли.
        if (ShouldLoadSessionData && SessionData != null)
        {
            this.UpgradeWatering = SessionData.upgradeWatering;
        }

        // Инициализация словаря (если нет сохранения, он будет пустым)
        positionBed = new Dictionary<int, List<Vector2Int>>();
        positionBed.Add(0, new List<Vector2Int>());
        positionBed.Add(1, new List<Vector2Int>());
        // CkeckValue(); // Этот метод можно убрать или оставить для отладки
    }


    public bool CompleteWateringUpgrade()
    {
        Debug.Log("Complete upgrade");
       UpgradeWatering = true;
        return true;
    }
    public void AddNewPositionBed(int level, Vector2Int posBed)
    {

       
        if (!positionBed.ContainsKey(level))
        {
           positionBed.Add(level, new List<Vector2Int>());
           
        }
        positionBed[level].Add(posBed);
    }
    
    public List<Vector2Int> GetValueFromDictionary(int key)
    {
        if(positionBed.TryGetValue(key, out List<Vector2Int> value))
          return value;  
        else  return null;
    }
    public void SaveStateToMemory()
    {
        Debug.Log("--- НАЧАЛО СОХРАНЕНИЯ СОСТОЯНИЯ В ПАМЯТЬ ---");
        GameSaveData saveData = new GameSaveData();
        SessionData = saveData;

        // 1. Сохраняем данные из самого PlantManager
        SessionData.upgradeWatering = this.UpgradeWatering;
        Debug.Log($"Сохранено: UpgradeWatering = {SessionData.upgradeWatering}");

        // 2. Находим все GridGenerator'ы на сцене
        GridGenerator[] gridGenerators = FindObjectsOfType<GridGenerator>();
        // --- ОТЛАДОЧНЫЙ ЛОГ 1 ---
        Debug.Log($"Найдено {gridGenerators.Length} объектов GridGenerator на сцене.");

        foreach (var grid in gridGenerators)
        {
            // --- ОТЛАДОЧНЫЙ ЛОГ 2 ---
            Debug.Log($"Сохранение данных для грида: '{grid.gameObject.name}'");

            GridSaveData gridData = new GridSaveData { identifier = grid.gameObject.name };

            // Собираем данные о слотах
            // --- ОТЛАДОЧНЫЙ ЛОГ 3 ---
            Debug.Log($"У грида '{grid.gameObject.name}' найдено {grid.gridObjects.Count} слотов в словаре для сохранения.");
            foreach (var slotEntry in grid.gridObjects)
            {
                SlotScripts slotScript = slotEntry.Value.GetComponent<SlotScripts>();
                SlotSaveData slotData = new SlotSaveData
                {
                    gridPosition = slotEntry.Key,
                    isPlanted = slotScript.isPlanted,
                    ishavebed = slotScript.ishavebed, // <--- Проверяем эти
                    isRaked = slotScript.isRaked     // <--- два флага
                };

                // --- ДОБАВЬТЕ ЭТОТ ЛОГ ---
                if (slotData.ishavebed || slotData.isRaked)
                {
                    Debug.Log($"СОХРАНЕНИЕ СЛОТА: {slotEntry.Value.name} -> ishavebed={slotData.ishavebed}, isRaked={slotData.isRaked}");
                }
                // --- КОНЕЦ ЛОГА ---

                gridData.slotsData.Add(slotData);
            }
            SessionData.gridsData.Add(gridData);

            // 3. Собираем данные о растениях
            PlantController[] plants = grid.GetComponentsInChildren<PlantController>();
            // --- ОТЛАДОЧНЫЙ ЛОГ 4 ---
            Debug.Log($"У грида '{grid.gameObject.name}' найдено {plants.Length} дочерних растений для сохранения.");
            foreach (var plant in plants)
            {
                SessionData.plantsData.Add(plant.GetSaveData());
            }
        }

        // --- ОТЛАДОЧНЫЙ ЛОГ 5 ---
        Debug.Log($"--- ИТОГ СОХРАНЕНИЯ ---");
        Debug.Log($"Всего сеток в SessionData: {SessionData.gridsData.Count}");
        // Выведем имена сеток, которые мы сохранили
        foreach (var gridData in SessionData.gridsData)
        {
            Debug.Log($"Сохранен идентификатор сетки: '{gridData.identifier}'");
        }
        Debug.Log($"Всего растений в SessionData: {SessionData.plantsData.Count}");

        // 4. Устанавливаем флаг
        ShouldLoadSessionData = true;

        Debug.Log("--- КОНЕЦ СОХРАНЕНИЯ. ShouldLoadSessionData = true ---");
    }



}
