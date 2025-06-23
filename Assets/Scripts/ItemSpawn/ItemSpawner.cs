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
                            animalController.InitializeMovementBounds(boundsCollider.bounds);
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
}