using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    // Словарь для хранения объектов сетки
    public Dictionary<Vector2Int, GameObject> gridObjects = new Dictionary<Vector2Int, GameObject>();

    [SerializeField] private GameObject prefab; // Префаб для создания объектов сетки
    [SerializeField] public Vector2Int gridSize = new Vector2Int(12, 2); // Размер сетки (12 столбцов, 2 ряда)
    [SerializeField] private Vector2 startPosition = new Vector2(2f, 3f); // Начальная позиция сетки (x=2, y=3)

    [SerializeField] private Vector3 _sizePLant;

    [SerializeField] ItemSpawner _itemSpawner;
    public string identifier;

    void Awake()
    {
        GenerateGrid();
        // LogGridObjects();
    }

    void GenerateGrid()
    {
        // Проверяем, что префаб задан
        if (prefab == null)
        {
            Debug.LogError("Префаб не назначен!");
            return;
        }

        // Очищаем словарь перед генерацией
        gridObjects.Clear();

        // Получаем размер префаба (предполагается, что у него есть компонент Renderer)
        Renderer prefabRenderer = prefab.GetComponent<Renderer>();
        Vector2 objectSize = prefabRenderer != null ? prefabRenderer.bounds.size : Vector2.one;

        // Цикл по строкам (y) и столбцам (x)
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                // Вычисляем позицию: первый объект в startPosition, остальные смещены на размер предыдущего объекта
                Vector3 position = new Vector3(
                    startPosition.x + x * objectSize.x,
                    startPosition.y + y * objectSize.y,
                    0
                );

                // Создаем объект из префаба
                GameObject newObject = Instantiate(prefab, position, Quaternion.identity, transform);
                newObject.name = $"Slot_{x}_{y}";

                Vector2Int gridPos = new Vector2Int(x, y);
                SlotScripts slotScript = newObject.GetComponent<SlotScripts>();
                if (slotScript != null)
                {
                    slotScript.gridPosition = gridPos;
                }
                else
                {
                    Debug.LogError($"Префаб слота {prefab.name} не содержит компонент SlotScripts!", prefab);
                }

                // Добавляем объект в словарь с ключом Vector2Int
             
                gridObjects[gridPos] = newObject;
            }
        }

        Debug.Log($"Сетка сгенерирована с {gridObjects.Count} объектами.");
    }

    // Метод для получения объекта по координатам сетки
    public GameObject GetObjectAt(Vector2Int position)
    {
        if (gridObjects.TryGetValue(position, out GameObject obj))
        {
            return obj;
        }
        Debug.LogWarning($"Объект не найден в позиции {position}");
        return null;
    }


    public (bool, Vector3, Vector2Int[]) CheckFreeSlot(string nameSlot)
    {
        Vector2Int currentIDSlot = GetGridPositionFromName(nameSlot);
        if (currentIDSlot != new Vector2Int(100, 100))
        {
            GameObject currentSlot = transform.Find(nameSlot)?.gameObject;
            SlotScripts currentSlotScripts = currentSlot.GetComponent<SlotScripts>();
            if (currentSlotScripts != null) {

                if (!currentSlotScripts.isPlanted && currentSlotScripts.isRaked) {
                    return (true, currentSlot.transform.position, new Vector2Int[1] { currentIDSlot });
                }
                else
                {
                    Debug.LogError($"Грядка занята");
                    return (false, new Vector3(222, 222), null);
                }
            }
            else
            {
                Debug.LogError($"Не удалось получить корректный currentSlotScripts из имени {nameSlot}.");
                return (false, new Vector3(222, 222), null);
            }
        }
        else
        {
            Debug.LogError($"Не удалось получить корректный ID слота из имени {nameSlot}.");
            return (false, new Vector3(222, 222), null);


        }
    }


    // Метод для проверки двух свободных слотов (вертикальная пара)
    public (bool, Vector3, Vector2Int[]) CheckFree2Slot(string nameSlot)
    {
        Vector2Int currentIDSlot = GetGridPositionFromName(nameSlot);
        if (currentIDSlot != new Vector2Int(100, 100))
        {
            if (currentIDSlot.y == 0)
            {
                GameObject currentSlot = transform.Find(nameSlot)?.gameObject;
                GameObject upperSlot = transform.Find($"Slot_{currentIDSlot.x}_{1}")?.gameObject;

                if (currentSlot == null || upperSlot == null)
                {
                    Debug.LogWarning($"Один из слотов ({nameSlot} или Slot_{currentIDSlot.x}_1) не найден.");
                    return (false, new Vector3(222, 222),null);
                }

                SlotScripts currentSlotScripts = currentSlot.GetComponent<SlotScripts>();
                SlotScripts upperSlotScript = upperSlot.GetComponent<SlotScripts>();

                if (currentSlotScripts != null && !currentSlotScripts.isPlanted && currentSlotScripts.isRaked &&
                    upperSlotScript != null && !upperSlotScript.isPlanted && upperSlotScript.isRaked)
                {
                    Debug.Log($"Текущий слот {currentSlot.name} и соседний {upperSlot.name} свободны.");
                    currentSlotScripts.isPlanted = true;
                    upperSlotScript.isPlanted = true;


                    Vector2Int[] IdCurrentSlot = new Vector2Int[2] { currentIDSlot, GetGridPositionFromName($"Slot_{currentIDSlot.x}_{1}") };

                    Vector3 pos = (upperSlot.transform.position + currentSlot.transform.position)/2;

                    return (true,pos,IdCurrentSlot);
                }
            }
            else if (currentIDSlot.y == 1)
            {
                GameObject currentSlot = transform.Find(nameSlot)?.gameObject;
                GameObject lowerSlot = transform.Find($"Slot_{currentIDSlot.x}_0")?.gameObject;

                if (currentSlot == null || lowerSlot == null)
                {
                    Debug.LogWarning($"Один из слотов ({nameSlot} или Slot_{currentIDSlot.x}_0) не найден.");
                    return (false, new Vector3(222, 222), null);
                }

                SlotScripts currentSlotScripts = currentSlot.GetComponent<SlotScripts>();
                SlotScripts lowerSlotScript = lowerSlot.GetComponent<SlotScripts>();

                if (currentSlotScripts != null && !currentSlotScripts.isPlanted && currentSlotScripts.isRaked &&
                    lowerSlotScript != null && !lowerSlotScript.isPlanted && lowerSlotScript.isRaked)
                {
                    Debug.Log($"Текущий слот {currentSlot.name} и соседний {lowerSlot.name} свободны.");
                    currentSlotScripts.isPlanted = true;
                    lowerSlotScript.isPlanted = true;

                    Vector2Int[] IdCurrentSlot = new Vector2Int[2] { currentIDSlot, GetGridPositionFromName($"Slot_{currentIDSlot.x}_{0}") };

                    Vector3 pos = (lowerSlotScript.transform.position + currentSlot.transform.position) / 2;

                    return (true, pos, IdCurrentSlot);
                }
            }

            Debug.LogWarning($"Не удалось найти свободную пару слотов для {nameSlot}.");
            return (false, new Vector3(222, 222), null);
        }
        else
        {
            Debug.LogError($"Не удалось получить корректный ID слота из имени {nameSlot}.");
            return (false, new Vector3(222, 222), null);
        }
    }

    // Метод для проверки четырех клеток, образующих квадрат, на isPlanted == false
    public (bool, Vector3, Vector2Int[]) CheckSquareCells(string nameslot)
    {
        Vector2Int currentCell = GetGridPositionFromName(nameslot);
        // Проверяем, что текущая клетка существует в словаре
        if (!gridObjects.TryGetValue(currentCell, out GameObject currentObj) || currentObj == null)
        {
            Debug.LogWarning($"Клетка {currentCell} не существует.");
            return (false, Vector3.zero, null);
        }

        // Проверяем, что текущая клетка имеет isPlanted == false
        SlotScripts currentSlot = currentObj.GetComponent<SlotScripts>();
        if (currentSlot == null || currentSlot.isPlanted)
        {
            Debug.LogWarning($"Клетка {currentCell} либо не имеет SlotScripts, либо уже занята (isPlanted = true).");
            return (false, Vector3.zero, null);
        }

        // Проверяем возможные квадраты вокруг текущей клетки
        // Вариант 1: текущая клетка — левый нижний угол (x,y), квадрат: (x,y), (x,y+1), (x+1,y), (x+1,y+1)
        Vector2Int[] square1 = new Vector2Int[]
        {
        currentCell,                    // (x, y)
        new Vector2Int(currentCell.x, currentCell.y + 1),   // (x, y+1)
        new Vector2Int(currentCell.x + 1, currentCell.y),   // (x+1, y)
        new Vector2Int(currentCell.x + 1, currentCell.y + 1) // (x+1, y+1)
        };

        // Вариант 2: текущая клетка — правый нижний угол (x,y), квадрат: (x-1,y), (x-1,y+1), (x,y), (x,y+1)
        Vector2Int[] square2 = new Vector2Int[]
        {
        new Vector2Int(currentCell.x - 1, currentCell.y),   // (x-1, y)
        new Vector2Int(currentCell.x - 1, currentCell.y + 1), // (x-1, y+1)
        currentCell,                    // (x, y)
        new Vector2Int(currentCell.x, currentCell.y + 1)    // (x, y+1)
        };

        // Вариант 3: текущая клетка — левый верхний угол (x,y), квадрат: (x,y-1), (x,y), (x+1,y-1), (x+1,y)
        Vector2Int[] square3 = new Vector2Int[]
        {
        new Vector2Int(currentCell.x, currentCell.y - 1),   // (x, y-1)
        currentCell,                    // (x, y)
        new Vector2Int(currentCell.x + 1, currentCell.y - 1), // (x+1, y-1)
        new Vector2Int(currentCell.x + 1, currentCell.y)    // (x+1, y)
        };

        // Вариант 4: текущая клетка — правый верхний угол (x,y), квадрат: (x-1,y-1), (x-1,y), (x,y-1), (x,y)
        Vector2Int[] square4 = new Vector2Int[]
        {
        new Vector2Int(currentCell.x - 1, currentCell.y - 1), // (x-1, y-1)
        new Vector2Int(currentCell.x - 1, currentCell.y),   // (x-1, y)
        new Vector2Int(currentCell.x, currentCell.y - 1),   // (x, y-1)
        currentCell                     // (x, y)
        };

        // Проверяем все варианты квадрата
        Vector2Int[][] squares = new Vector2Int[][] { square1, square2, square3, square4 };

        foreach (var square in squares)
        {
            // Проверяем, что все клетки в квадрате находятся в пределах сетки
            bool isValid = true;
            foreach (var cell in square)
            {
                if (cell.x < 0 || cell.x >= gridSize.x || cell.y < 0 || cell.y >= gridSize.y)
                {
                    isValid = false;
                    break;
                }
            }

            // Проверяем, что все клетки имеют isPlanted == false
            if (isValid)
            {
                bool allFree = true;
                Vector3 centerPosition = Vector3.zero;
                int validObjects = 0;

                // Проверяем слоты и накапливаем позиции для вычисления центра
                foreach (var cell in square)
                {
                    if (!gridObjects.TryGetValue(cell, out GameObject obj) || obj == null)
                    {
                        allFree = false;
                        break;
                    }

                    SlotScripts slot = obj.GetComponent<SlotScripts>();
                    if (slot == null || slot.isPlanted || !slot.ishavebed || !slot.isRaked)
                    {
                        allFree = false;
                        break;
                    }

                    // Суммируем позиции объектов для вычисления центра
                    centerPosition += obj.transform.position;
                    validObjects++;
                }

                // Если найден свободный квадрат, устанавливаем isPlanted = true и возвращаем центр и массив id
                if (allFree && validObjects == 4)
                {
                    // Вычисляем среднюю позицию (центр квадрата)
                    centerPosition /= 4f;

                    // Устанавливаем isPlanted = true для всех клеток
                    foreach (var cell in square)
                    {
                        if (gridObjects.TryGetValue(cell, out GameObject obj))
                        {
                            SlotScripts slot = obj.GetComponent<SlotScripts>();
                            if (slot != null)
                            {
                                slot.isPlanted = true;
                            }
                        }
                    }

                    Debug.Log($"Найден свободный квадрат: {square[0]}, {square[1]}, {square[2]}, {square[3]}. " +
                              $"Установлено isPlanted = true. Центр квадрата: {centerPosition}");
                    return (true, centerPosition, square);
                }
            }
        }

        Debug.LogWarning($"Свободный квадрат вокруг клетки {currentCell} не найден.");
        return (false, Vector3.zero, null);
    }

    private Vector2Int GetGridPositionFromName(string objectName)
    {
        // Проверяем, соответствует ли имя ожидаемому формату
        if (!objectName.StartsWith("Slot_") || !objectName.Contains("_"))
        {
            Debug.LogWarning($"Неверный формат имени объекта: {objectName}. Ожидается формат 'Slot_X_Y'.");
            return Vector2Int.zero;
        }

        // Разделяем строку по символу '_'
        string[] parts = objectName.Split('_');
        if (parts.Length != 3)
        {
            Debug.LogWarning($"Неверный формат имени объекта: {objectName}. Ожидается формат 'Slot_X_Y'.");
            return Vector2Int.zero;
        }

        // Пробуем преобразовать X и Y в числа
        if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
        {
            return new Vector2Int(x, y);
        }

        Debug.LogWarning($"Не удалось преобразовать координаты из имени: {objectName}");
        return new Vector2Int(100, 100);
    }


    public bool FreeSlot(Vector2Int[] idSlots)
    {
        bool allFreed = true;

        foreach (Vector2Int slotPos in idSlots)
        {
            // Проверяем, существует ли слот в словаре
            if (gridObjects.TryGetValue(slotPos, out GameObject slotObj) && slotObj != null)
            {
                // Получаем компонент SlotScripts
                SlotScripts slotScript = slotObj.GetComponent<SlotScripts>();
                if (slotScript != null)
                {
                    // Устанавливаем isPlanted в false
                    slotScript.isPlanted = false;
                    slotScript.isRaked = false;
                    Transform bedTransform = slotObj.transform.Find("Bed(Clone)");
                    if (bedTransform != null)
                    {
                        slotScript.ChangeStateBed(
                                                 BedData.StageGrowthPlant.DrySoil,
                                                 0);
                    }
                    else
                    {
                        Debug.LogWarning($"Грядка не найдена в слоте {slotObj.name}");
                    }
                    Debug.Log($"Слот {slotObj.name} освобожден (isPlanted = false).");
                }
                else
                {
                    Debug.LogWarning($"Слот {slotObj.name} не имеет компонента SlotScripts.");
                    allFreed = false;
                }
            }
            else
            {
                Debug.LogWarning($"Слот с позицией {slotPos} не найден в сетке.");
                allFreed = false;
            }
        }

        return allFreed;
    }
    public void FertilizerSlot(Vector2Int[] idSlots)
    {
        bool allFertilizer = true;

        foreach (Vector2Int slotPos in idSlots)
        {
            // Проверяем, существует ли слот в словаре
            if (gridObjects.TryGetValue(slotPos, out GameObject slotObj) && slotObj != null)
            {
                // Получаем компонент SlotScripts
                SlotScripts slotScript = slotObj.GetComponent<SlotScripts>();
                if (slotScript != null)
                {
                    Transform bedTransform = slotObj.transform.Find("Bed(Clone)");
                    if (bedTransform != null)
                    {
                        slotScript.ChangeStateBed(
                                                BedData.StageGrowthPlant.WithFertilizers,
                                                3);
                    }
                    else
                    {
                        Debug.LogWarning($"В слоте {slotObj.name} не найдена грядка (Bed(Clone)).");
                        allFertilizer = false;
                    }
                }
                else
                {
                    Debug.LogWarning($"Слот {slotObj.name} не имеет компонента SlotScripts.");
                    allFertilizer = false;
                }
            }
            else
            {
                Debug.LogWarning($"Слот с позицией {slotPos} не найден в сетке.");
                allFertilizer = false;
            }
        }

       
    }




    public GridSaveData GetSaveData()
    {
        var gridData = new GridSaveData
        {
            identifier = this.identifier,
            slotsData = new List<SlotSaveData>()
        };

        // Проходим по всем GameObject'ам в словаре
        foreach (var slotObject in gridObjects.Values)
        {
            SlotScripts slot = slotObject.GetComponent<SlotScripts>();
            if (slot != null)
            {
                // Получаем данные из компонента Slot и добавляем в список
                gridData.slotsData.Add(slot.GetSaveData());
            }
        }
        return gridData;
    }

    public void ApplySaveData(GridSaveData data)
    {
        if (data.identifier != this.identifier)
        {
            Debug.LogWarning($"Попытка применить данные от грядки '{data.identifier}' к грядке '{this.identifier}'. Операция пропущена.");
            return;
        }

        foreach (var slotData in data.slotsData)
        {
            // Используем словарь для мгновенного поиска слота
            if (gridObjects.TryGetValue(slotData.gridPosition, out GameObject slotObject))
            {
                SlotScripts targetSlot = slotObject.GetComponent<SlotScripts>();
                if (targetSlot != null)
                {
                    targetSlot.ApplySaveData(slotData);
                }
            }
            else
            {
                Debug.LogWarning($"Слот с позицией {slotData.gridPosition} не найден в грядке '{identifier}' при загрузке.");
            }
        }
    }



    public Vector3 GetWorldPositionForSlot(Vector2Int gridPosition)
    {
        if (gridObjects.TryGetValue(gridPosition, out GameObject slotObject))
        {
            return slotObject.transform.position;
        }
        Debug.LogWarning($"Запрошена мировая позиция для несуществующего слота {gridPosition} на грядке '{identifier}'.");
        return transform.position; // Возвращаем позицию грядки как запасной вариант
    }

    // Вспомогательный метод для расчета центральной позиции для спавна

    private int GetLevelGrids(string name)
    {
        if(name == "GridGeneratorUp")
        {
            return 0;
        }
        if(name == "GridGeneratorDown")
        {
            return 1;
        }
        return -1;
    }

    private int GetLengthDictionaryValue(int key)
    {
        return PlantManager.instance.positionBed.Where(k=> k.Key == key).Count();
    }

    private Vector2Int[] GetArrayPostition(List<Vector2Int> pos)
    {
        return pos.ToArray();
    }

    public void SpawnPlantFromSave(PlantSaveData plantData)
    {
        var plant = plantData as PlantSaveData;
        if (identifier == plantData.gridIdentifier) {
            _itemSpawner.SpawnLoadedPlant(plant,transform,plantData.currentposition,_sizePLant);
        }


        
    }
}