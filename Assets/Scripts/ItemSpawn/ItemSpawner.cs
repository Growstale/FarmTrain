using UnityEditor.Rendering;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct ItemSpawnInfo
    {
        public ItemData itemData;
        public Vector3 position;
    }

    [Header("Основной Префаб")]
    [Tooltip("Префаб, который будет использоваться как шаблон для всех спаунящихся предметов.")]
    public GameObject worldItemPrefab;

    [Header("Начальный Спавн")]
    [Tooltip("Список предметов для спавна при старте игры.")]
    public ItemSpawnInfo[] itemsToSpawnAtStart;

    [Header("Настройки по Умолчанию")]
    [Tooltip("Масштаб по умолчанию, если не указан другой при вызове SpawnItem.")]
    public Vector3 defaultSpawnScale = Vector3.one;

    [Header("Ссылки на контроллеры")]
    [Tooltip("Ссылка на контроллер камеры/поезда для определения родительского вагона.")]
    public TrainCameraController trainController;

    void Start()
    {
        if (trainController == null)
        {
            Debug.LogError("TrainCameraController не назначен в ItemSpawner! Невозможно определить родительские вагоны.");
        }

        SpawnInitialItems();
    }

    void SpawnInitialItems()
    {
        if (itemsToSpawnAtStart == null || itemsToSpawnAtStart.Length == 0) { return; }

        foreach (ItemSpawnInfo spawnInfo in itemsToSpawnAtStart)
        {
            if (spawnInfo.itemData != null && spawnInfo.itemData.itemType != ItemType.Animal && worldItemPrefab == null)
            {
                Debug.LogError($"World Item Prefab не назначен в ItemSpawner, но пытаемся заспавнить предмет {spawnInfo.itemData.itemName} при старте!");
                continue;
            }
            SpawnItem(spawnInfo.itemData, spawnInfo.position, defaultSpawnScale);
        }
    }

    public GameObject SpawnItem(ItemData dataToSpawn, Vector3 spawnPosition, Vector3 spawnScale)
    {
        if (dataToSpawn == null)
        {

            Debug.LogWarning("Попытка заспавнить предмет с null ItemData.");
            return null;
        }

        

        if (dataToSpawn.itemType == ItemType.Animal && dataToSpawn.associatedAnimalData != null)
        {
            AnimalData animalData = dataToSpawn.associatedAnimalData;
            if (animalData.animalPrefab == null)
            {
                Debug.LogError($"У AnimalData '{animalData.speciesName}' не назначен animalPrefab в инспекторе!");
                return null;
            }

            GameObject animalObject = Instantiate(animalData.animalPrefab, spawnPosition, Quaternion.identity);

            animalObject.transform.localScale = spawnScale;

            Transform parentWagon = null;
            bool parentAssignedSuccessfully = false;

            if (trainController != null)
            {
                parentAssignedSuccessfully = trainController.AssignParentWagonByPosition(animalObject.transform, spawnPosition);

                if (parentAssignedSuccessfully)
                {
                    parentWagon = animalObject.transform.parent;

                    if (parentWagon == null)
                    {
                        Debug.LogError($"AssignParentWagonByPosition вернул true, но родитель у {animalObject.name} не установился! Животное уничтожено.", animalObject);
                        Destroy(animalObject);
                        return null;
                    }
                }
                else
                {
                    Debug.LogError($"Не удалось найти или назначить родительский вагон для животного '{animalData.speciesName}' в позиции {spawnPosition}. Животное уничтожено.");
                    Destroy(animalObject);
                    return null;
                }
            }
            else
            {
                Debug.LogWarning("TrainController не назначен в ItemSpawner. Невозможно определить вагон для животного. Животное уничтожено.");
                Destroy(animalObject);
                return null;
            }

            AnimalController animalController = animalObject.GetComponent<AnimalController>();
            if (animalController != null)
            {
                animalController.animalData = animalData;

                if (parentWagon != null)
                {
                    string placementAreaName = animalData.speciesName.Replace(" ", "") + "PlacementArea";
                    Transform placementAreaTransform = parentWagon.Find(placementAreaName);
                    if (placementAreaTransform != null)
                    {
                        Collider2D boundsCollider = placementAreaTransform.GetComponent<Collider2D>();
                        if (boundsCollider != null)
                        {
                            animalController.InitializeMovementBounds(boundsCollider.bounds, false);
                            Debug.Log($"Границы {boundsCollider.bounds} переданы животному {animalData.speciesName} ({animalObject.name})");
                        }
                        else
                        {
                            Debug.LogError($"На объекте 'AnimalPlacementArea' в вагоне '{parentWagon.name}' отсутствует компонент Collider2D! Животное не сможет двигаться корректно.");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Не найден дочерний объект с именем 'AnimalPlacementArea' в вагоне '{parentWagon.name}'! Животное не сможет двигаться корректно.");
                    }
                }
                else
                {
                    Debug.LogError($"Не удалось получить Transform родительского вагона ({animalObject.name}) для поиска AnimalPlacementArea. Животное уничтожено.");
                    Destroy(animalObject);
                    return null;
                }

                Debug.Log($"Заспавнен ЖИВОТНОЕ: {animalData.speciesName} в позиции {spawnPosition}");
                return animalObject;
            }
            else
            {
                Debug.LogError($"На префабе животного '{animalData.animalPrefab.name}' ({animalObject.name}) отсутствует компонент AnimalController! Животное уничтожено.");
                Destroy(animalObject);
                return null;
            }
        }
        else
        {
            if (worldItemPrefab == null)
            {
                Debug.LogError($"World Item Prefab не назначен в ItemSpawner! Невозможно заспавнить предмет '{dataToSpawn.itemName}'.");
                return null;
            }

            GameObject newItemObject = Instantiate(worldItemPrefab, spawnPosition, Quaternion.identity);

            newItemObject.transform.localScale = spawnScale;

            if (trainController != null)
            {
                if (!trainController.AssignParentWagonByPosition(newItemObject.transform, spawnPosition))
                {
                    Debug.LogError($"Не удалось найти или назначить родительский вагон для предмета '{dataToSpawn.itemName}' в позиции {spawnPosition}. Предмет уничтожен.");
                    Destroy(newItemObject);
                    return null;
                }
            }
            else
            {
                Debug.LogWarning($"TrainController не назначен в ItemSpawner. Предмет '{dataToSpawn.itemName}' не будет привязан к вагону.");
            }

            WorldItem worldItemComponent = newItemObject.GetComponent<WorldItem>();
            if (worldItemComponent != null)
            {
                worldItemComponent.itemData = dataToSpawn;
                worldItemComponent.InitializeVisuals();
                Debug.Log($"Заспавнен ПРЕДМЕТ: {dataToSpawn.itemName} в позиции {spawnPosition}");
                return newItemObject;
            }
            else
            {
                Debug.LogError($"На префабе '{worldItemPrefab.name}' отсутствует компонент WorldItem! Предмет '{dataToSpawn.itemName}' уничтожен.");
                Destroy(newItemObject);
                return null;
            }
        }
    }

    public GameObject SpawnItem(ItemData dataToSpawn, Vector3 spawnPosition)
    {
        return SpawnItem(dataToSpawn, spawnPosition, defaultSpawnScale);
    }


    // тестовая функция для спавна префаба грядки на уровне
    public GameObject TestSpawnBed(ItemData dataToSpawn, Vector3 spawnPosition, Vector3 spawnScale, Transform parentTransform)
    {
        if (dataToSpawn.itemType == ItemType.Pot)
        {


            BedData bedData = dataToSpawn.associatedBedData;

            if (bedData.bedlPrefab == null)
            {
                Debug.LogError($"У BedData '{bedData.speciesName}' не назначен bedlPrefab в инспекторе!");
                return null;
            }

            GameObject bedObject = Instantiate(bedData.bedlPrefab, spawnPosition, Quaternion.identity);

            bedObject.transform.localScale = spawnScale;
            if (parentTransform != null) {
                bedObject.transform.parent = parentTransform;
                Debug.Log($"Заспавнен растение: {bedObject.name} в позиции {spawnPosition}");
                       return bedObject;
            }
            else
            {
                Debug.LogError($"Отсутствует ссылка на позицию родителя, объект не заспавнен");
                            Destroy(bedObject);
                           return null;

            }
          

        }
        Debug.Log($"Попытка спавна не грядки, ошибка");
        return null;

    }


   
    public GameObject SpawnPlant(ItemData dataToSpawn, Vector3 spawnPosition, Vector3 spawnScale, Transform parentTransform, Vector2Int[] IdSelectedSlot)
    {
        if (dataToSpawn.itemType == ItemType.Seed)
        {


            PlantData plantData = dataToSpawn.associatedPlantData;

            if (plantData.PlantPrefab == null)
            {
                Debug.LogError($"У plantData '{plantData.name}' не назначен PlantPrefab в инспекторе!");
                return null;
            }

            GameObject plantObject = Instantiate(plantData.PlantPrefab, spawnPosition, Quaternion.identity);

            plantObject.transform.localScale = spawnScale;
            plantObject.transform.parent = parentTransform;
            PlantController plantController = plantObject.GetComponent<PlantController>();

            if (plantController != null)
            {
               
                plantController.FillVectorInts(IdSelectedSlot);
            }
            else
            {
                Debug.LogError($"У объекта {plantObject.name} нет plantController, растение не заспавнено!");
                return null;

            }

            Debug.Log($"Заспавнен растение: {plantObject.name} в позиции {spawnPosition}");
            return plantObject;




        }
        Debug.Log($"Попытка спавна не растения, ошибка");
        return null;
    }
    public void SpawnAndInitializePlant(ItemData seedData, Vector3 position, Vector3 scale, Transform parent, PlantSaveData saveData)
    {
        // 1. Используем ваш существующий метод для создания объекта растения
        // Он уже умеет создавать объект и передавать ему ID слотов
        GameObject plantObject = SpawnPlant(seedData, position, scale, parent, saveData.occupiedSlots);

        // 2. Проверяем, что растение успешно создалось
        if (plantObject != null)
        {
            // 3. Получаем его контроллер
            PlantController plantController = plantObject.GetComponent<PlantController>();
            if (plantController != null)
            {
                // 4. Вызываем метод инициализации из сохранения.
                // Этот метод применит нужную стадию роста, таймеры и т.д.
                plantController.InitializeFromSave(saveData);

                Debug.Log($"Растение {saveData.plantDataName} успешно восстановлено из сохранения.");
            }
            else
            {
                // Эта ошибка не должна произойти, если ваш SpawnPlant работает правильно, но проверка не помешает
                Debug.LogError($"На заспавненном префабе растения '{plantObject.name}' отсутствует компонент PlantController!");
            }
        }
        else
        {
            Debug.LogWarning($"Метод SpawnPlant не смог создать объект для {seedData.itemName}. Восстановление прервано.");
        }
    }

}