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
        // Небольшая проверка и для worldItemPrefab здесь, хотя основная логика в SpawnItem
        if (itemsToSpawnAtStart == null || itemsToSpawnAtStart.Length == 0) { return; }

        foreach (ItemSpawnInfo spawnInfo in itemsToSpawnAtStart)
        {
            // Проверяем, нужен ли worldItemPrefab перед вызовом спавна обычного предмета
            if (spawnInfo.itemData != null && spawnInfo.itemData.itemType != ItemType.Animal && worldItemPrefab == null)
            {
                Debug.LogError($"World Item Prefab не назначен в ItemSpawner, но пытаемся заспавнить предмет {spawnInfo.itemData.itemName} при старте!");
                continue; // Пропускаем спавн этого предмета
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
            // --- ЭТО ЖИВОТНОЕ! ---
            AnimalData animalData = dataToSpawn.associatedAnimalData;
            if (animalData.animalPrefab == null)
            {
                Debug.LogError($"У AnimalData '{animalData.speciesName}' не назначен animalPrefab в инспекторе!");
                return null;
            }

            // 1. Создаем копию префаба ЖИВОТНОГО
            GameObject animalObject = Instantiate(animalData.animalPrefab, spawnPosition, Quaternion.identity);

            // 2. Устанавливаем масштаб
            animalObject.transform.localScale = spawnScale;

            // 3. ----- УСТАНОВКА РОДИТЕЛЯ (ВАГОНА) И ПОЛУЧЕНИЕ ЕГО TRANSFORM -----
            Transform parentWagon = null; // Переменная для хранения ссылки на вагон
            bool parentAssignedSuccessfully = false; // Флаг для отслеживания успеха

            if (trainController != null)
            {
                // Вызываем метод. Он ВНУТРИ СЕБЯ устанавливает родителя, если находит вагон.
                // Метод возвращает true, если установка прошла успешно, иначе false.
                parentAssignedSuccessfully = trainController.AssignParentWagonByPosition(animalObject.transform, spawnPosition);

                if (parentAssignedSuccessfully)
                {
                    // Если родитель успешно назначен, ПОЛУЧАЕМ ссылку на него из объекта
                    parentWagon = animalObject.transform.parent;

                    // Дополнительная проверка: убедимся, что родитель действительно установился
                    if (parentWagon == null)
                    {
                        Debug.LogError($"AssignParentWagonByPosition вернул true, но родитель у {animalObject.name} не установился! Животное уничтожено.", animalObject);
                        Destroy(animalObject);
                        return null;
                    }
                    // Debug.Log($"Животное {animalObject.name} успешно привязано к вагону {parentWagon.name}");
                }
                else // Если AssignParentWagonByPosition вернул false
                {
                    Debug.LogError($"Не удалось найти или назначить родительский вагон для животного '{animalData.speciesName}' в позиции {spawnPosition}. Животное уничтожено.");
                    Destroy(animalObject);
                    return null;
                }
            }
            else
            {
                // trainController не назначен, мы уже выдали ошибку в Start, но дублируем здесь для надежности
                Debug.LogWarning("TrainController не назначен в ItemSpawner. Невозможно определить вагон для животного. Животное уничтожено.");
                Destroy(animalObject);
                return null;
            }

            // 4. Получаем компонент AnimalController
            AnimalController animalController = animalObject.GetComponent<AnimalController>();
            if (animalController != null)
            {
                // 5. Назначаем данные животного (на префабе уже должно быть, но для надежности можно оставить)
                animalController.animalData = animalData;

                // 6. ----- ИЩЕМ ГРАНИЦЫ В ВАГОНЕ И ПЕРЕДАЕМ ИХ ЖИВОТНОМУ -----
                // Используем parentWagon, который мы получили выше.
                // Проверка parentWagon != null здесь важна, хотя она должна быть пройдена, если мы дошли сюда.
                if (parentWagon != null)
                {
                    string placementAreaName = animalData.speciesName.Replace(" ", "") + "PlacementArea";
                    Transform placementAreaTransform = parentWagon.Find(placementAreaName);
                    if (placementAreaTransform != null)
                    {
                        Collider2D boundsCollider = placementAreaTransform.GetComponent<Collider2D>();
                        if (boundsCollider != null)
                        {
                            // УРА! Нашли коллайдер, передаем его границы в AnimalController
                            animalController.InitializeMovementBounds(boundsCollider.bounds);
                            Debug.Log($"Границы {boundsCollider.bounds} переданы животному {animalData.speciesName} ({animalObject.name})");
                        }
                        else
                        {
                            Debug.LogError($"На объекте 'AnimalPlacementArea' в вагоне '{parentWagon.name}' отсутствует компонент Collider2D! Животное не сможет двигаться корректно.");
                            // Реши, что делать: уничтожать или позволять жить без границ?
                            // Destroy(animalObject); return null;
                        }
                    }
                    else
                    {
                        Debug.LogError($"Не найден дочерний объект с именем 'AnimalPlacementArea' в вагоне '{parentWagon.name}'! Животное не сможет двигаться корректно.");
                        // Реши, что делать: уничтожать или позволять жить без границ?
                        // Destroy(animalObject); return null;
                    }
                }
                else
                {
                    // Эта ошибка не должна возникать при нормальной работе, но оставим на всякий случай
                    Debug.LogError($"Не удалось получить Transform родительского вагона ({animalObject.name}) для поиска AnimalPlacementArea. Животное уничтожено.");
                    Destroy(animalObject);
                    return null;
                }

                Debug.Log($"Заспавнен ЖИВОТНОЕ: {animalData.speciesName} в позиции {spawnPosition}");
                return animalObject; // Возвращаем созданный объект животного
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
            // --- ЭТО ОБЫЧНЫЙ ПРЕДМЕТ (WorldItem) ---
            if (worldItemPrefab == null)
            {
                Debug.LogError($"World Item Prefab не назначен в ItemSpawner! Невозможно заспавнить предмет '{dataToSpawn.itemName}'.");
                return null;
            }

            // 1. Создаем копию префаба предмета
            GameObject newItemObject = Instantiate(worldItemPrefab, spawnPosition, Quaternion.identity);

            // 2. Устанавливаем масштаб
            newItemObject.transform.localScale = spawnScale;

            // 3. ----- УСТАНОВКА РОДИТЕЛЯ (ВАГОНА) -----
            if (trainController != null)
            {
                // Вызываем метод и проверяем его bool результат.
                // Если НЕ удалось назначить родителя (метод вернул false)...
                if (!trainController.AssignParentWagonByPosition(newItemObject.transform, spawnPosition))
                {
                    Debug.LogError($"Не удалось найти или назначить родительский вагон для предмета '{dataToSpawn.itemName}' в позиции {spawnPosition}. Предмет уничтожен.");
                    Destroy(newItemObject);
                    return null;
                }
                // Если метод вернул true, родитель уже назначен внутри AssignParentWagonByPosition.
            }
            else
            {
                // Предмет можно и не привязывать к вагону, если trainController не задан, но выведем предупреждение
                Debug.LogWarning($"TrainController не назначен в ItemSpawner. Предмет '{dataToSpawn.itemName}' не будет привязан к вагону.");
                // Реши, нужно ли уничтожать предмет в этом случае:
                // Destroy(newItemObject); return null;
            }

            // 4. Получаем компонент WorldItem
            WorldItem worldItemComponent = newItemObject.GetComponent<WorldItem>();
            if (worldItemComponent != null)
            {
                // 5. Настраиваем WorldItem
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

    // Перегрузка метода без масштаба
    public GameObject SpawnItem(ItemData dataToSpawn, Vector3 spawnPosition)
    {
        // Используем масштаб по умолчанию для обоих типов объектов
        return SpawnItem(dataToSpawn, spawnPosition, defaultSpawnScale);
    }
}