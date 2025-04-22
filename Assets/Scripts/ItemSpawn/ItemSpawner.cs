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
        if (worldItemPrefab == null) { Debug.LogError("..."); return; }
        if (itemsToSpawnAtStart == null || itemsToSpawnAtStart.Length == 0) { return; }

        foreach (ItemSpawnInfo spawnInfo in itemsToSpawnAtStart)
        {
            // Вызываем новый ОБЩИЙ метод, используя масштаб по умолчанию
            SpawnItem(spawnInfo.itemData, spawnInfo.position, defaultSpawnScale);
        }
    }

    public GameObject SpawnItem(ItemData dataToSpawn, Vector3 spawnPosition, Vector3 spawnScale)
    {
        if (worldItemPrefab == null)
        {
            Debug.LogError("World Item Prefab не назначен в ItemSpawner!");
            return null;
        }
        if (dataToSpawn == null)
        {
            Debug.LogWarning("Попытка заспавнить предмет с null ItemData.");
            return null; 
        }

        // 1. Создаем копию префаба
        GameObject newItemObject = Instantiate(worldItemPrefab, spawnPosition, Quaternion.identity);

        // 2. Устанавливаем переданный масштаб
        newItemObject.transform.localScale = spawnScale;

        // ----- УСТАНОВКА РОДИТЕЛЯ -----
        if (trainController != null)
        {
            // Вызываем метод контроллера для поиска и установки родителя
            trainController.AssignParentWagonByPosition(newItemObject.transform, spawnPosition);
        }
        else
        {
            Debug.LogWarning("TrainController не назначен");
            Destroy(newItemObject);
            return null;       
        }

        // 3. Получаем компонент WorldItem
        WorldItem worldItemComponent = newItemObject.GetComponent<WorldItem>();

        // 4. Настраиваем WorldItem
        if (worldItemComponent != null)
        {
            worldItemComponent.itemData = dataToSpawn; // Назначаем переданные данные
            worldItemComponent.InitializeVisuals();   // Инициализируем визуал
            Debug.Log($"Заспавнен {dataToSpawn.itemName} в позиции {spawnPosition} с масштабом {spawnScale}");
            return newItemObject; 
        }
        else
        {
            Debug.LogError($"На префабе {worldItemPrefab.name} отсутствует компонент WorldItem!");
            Destroy(newItemObject);
            return null;
        }
    }

    public GameObject SpawnItem(ItemData dataToSpawn, Vector3 spawnPosition)
    {
        return SpawnItem(dataToSpawn, spawnPosition, defaultSpawnScale);
    }
}