// TrainPenController.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TrainPenController : MonoBehaviour
{
    public static TrainPenController Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private ItemSpawner itemSpawner;

    // <<< ИЗМЕНЕНИЕ: Это наш "живой" кэш ссылок на объекты сцены
    private List<PenRuntimeInfo> livePenInfo = new List<PenRuntimeInfo>();

    private List<AnimalController> spawnedAnimals = new List<AnimalController>();
    private bool hasSpawned = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

    }

    void Start()
    {
        // К моменту вызова Start(), все Awake() уже гарантированно выполнились.
        // AnimalPenManager.Instance точно не будет null.
        InitializePenInfo();
        UpdateAllPenVisuals();

        if (itemSpawner == null)
        {
            Debug.LogError("ItemSpawner не назначен в TrainPenController!", this);
            return;
        }

        if (!hasSpawned)
        {
            SpawnAnimalsFromData();
            hasSpawned = true;
        }
    }

    private void UpdateAllPenVisuals()
    {
        foreach (var penInfo in livePenInfo)
        {
            UpdatePenVisuals(penInfo.config.animalData);
        }
    }

    // <<< НОВЫЙ ПУБЛИЧНЫЙ МЕТОД
    public void UpdatePenVisuals(AnimalData forAnimal)
    {
        var penInfo = livePenInfo.FirstOrDefault(p => p.config.animalData == forAnimal);
        if (penInfo == null) return;

        int currentLevel = AnimalPenManager.Instance.GetCurrentPenLevel(forAnimal);
        if (currentLevel < 0 || currentLevel >= penInfo.config.upgradeLevels.Count)
        {
            Debug.LogError($"Некорректный уровень {currentLevel} для загона {forAnimal.speciesName}");
            return;
        }

        penInfo.currentLevel = currentLevel;
        PenLevelData levelData = penInfo.config.upgradeLevels[currentLevel];

        // Устанавливаем правильный спрайт
        penInfo.penSpriteRenderer.sprite = levelData.penSprite;
        Debug.Log($"Обновлен спрайт для загона {forAnimal.speciesName} на уровень {currentLevel}.");
    }


    // <<< НОВЫЙ МЕТОД: Находим объекты на сцене и кэшируем ссылки
    private void InitializePenInfo()
    {
        // Получаем все "сухие" данные из глобального менеджера
        List<PenConfigData> configs = AnimalPenManager.Instance.GetAllPenConfigs();

        foreach (var config in configs)
        {
            Transform penRendererTransform = FindDeepChild(transform, config.penSpriteRendererName);
            Transform placementAreaTransform = FindDeepChild(transform, config.placementAreaName);
            Transform animalParentTransform = FindDeepChild(transform, config.animalParentName);

            if (penRendererTransform == null) { /* ошибка */ continue; }

            SpriteRenderer penRenderer = penRendererTransform.GetComponent<SpriteRenderer>();
            if (penRenderer == null) { /* ошибка */ continue; }

            if (placementAreaTransform == null)
            {
                Debug.LogError($"TrainPenController не может найти объект с именем '{config.placementAreaName}'!", gameObject);
                continue;
            }
            if (animalParentTransform == null)
            {
                Debug.LogError($"TrainPenController не может найти объект с именем '{config.animalParentName}'!", gameObject);
                continue;
            }

            Collider2D areaCollider = placementAreaTransform.GetComponent<Collider2D>();
            if (areaCollider == null)
            {
                Debug.LogError($"На объекте '{config.placementAreaName}' отсутствует Collider2D!", placementAreaTransform);
                continue;
            }

            livePenInfo.Add(new PenRuntimeInfo
            {
                config = config,
                penSpriteRenderer = penRenderer, // <<< Сохраняем SpriteRenderer
                animalParent = animalParentTransform,
                placementArea = areaCollider
            });

        }
        Debug.Log($"<color=orange>[TRAIN DEBUG]</color> Инициализировано {livePenInfo.Count} загонов. Проверяю их: ");
        foreach (var info in livePenInfo)
        {
            int capacity = AnimalPenManager.Instance.GetMaxCapacityForAnimal(info.config.animalData);
            Debug.Log($" - Загон для '{info.config.animalData.speciesName}', вместимость: {capacity}, parent: {info.animalParent.name}");
        }
    }

    // <<< ИЗМЕНЕНИЕ: Этот метод теперь ищет в локальном кэше livePenInfo
    public PenRuntimeInfo GetLivePenInfoForAnimal(AnimalData animalData)
    {
        return livePenInfo.FirstOrDefault(p => p.config.animalData == animalData);
    }


    private void SpawnAnimalsFromData()
    {
        Debug.Log("<color=orange>[TRAIN DEBUG]</color> Начинаю SpawnAnimalsFromData...");

        if (livePenInfo.Count == 0)
        {
            Debug.LogError("<color=red>[TRAIN DEBUG]</color> ОШИБКА: livePenInfo пуст! Не могу спавнить, т.к. нет информации о загонах.");
            return;
        }

        // Теперь мы итерируемся по нашим "живым" загонам
        foreach (var penInfo in livePenInfo)
        {
            var animalData = penInfo.config.animalData;
            int countInManager = AnimalPenManager.Instance.GetAnimalCount(animalData);
            Debug.Log($"<color=orange>[TRAIN DEBUG]</color> Проверяю загон для '{animalData.speciesName}'. В менеджере числится: {countInManager} шт.");

            List<AnimalStateData> statesToSpawn = AnimalPenManager.Instance.GetStatesForAnimalType(penInfo.config.animalData);

            if (statesToSpawn.Count > 0)
            {
                Debug.Log($"Найдены данные для {penInfo.config.animalData.speciesName}. Нужно заспавнить: {statesToSpawn.Count}");
                foreach (var animalState in statesToSpawn)
                {
                    SpawnSingleAnimal(penInfo, animalState); // Передаем "живой" PenRuntimeInfo
                }
            }
        }
    }

    // <<< ИЗМЕНЕНИЕ: Метод принимает PenRuntimeInfo
    private void SpawnSingleAnimal(PenRuntimeInfo penInfo, AnimalStateData stateToLoad)
    {
        var animalData = penInfo.config.animalData;

        // ... (логика определения spawnPos остается той же) ...
        Vector3 spawnPos;
        if (stateToLoad.hasBeenPlaced)
        {
            spawnPos = stateToLoad.lastPosition;
        }
        else
        {
            spawnPos = GetRandomSpawnPosition(penInfo.placementArea.bounds);
        }

        GameObject animalGO = itemSpawner.SpawnItem(animalData.correspondingItemData, spawnPos);

        if (animalGO != null)
        {
            // Используем "живую" ссылку на родителя
            animalGO.transform.SetParent(penInfo.animalParent, true);

            AnimalController newAnimal = animalGO.GetComponent<AnimalController>();
            if (newAnimal != null)
            {
                // Используем "живую" ссылку на границы
                newAnimal.InitializeWithState(stateToLoad, penInfo.placementArea.bounds);
                spawnedAnimals.Add(newAnimal);
            }
            else
            {
                Debug.LogError($"Объект, созданный спавнером для {animalData.name}, не имеет компонента AnimalController!", animalGO);
            }
        }
    }

    // ... (DespawnAnimal и GetRandomSpawnPosition остаются почти без изменений) ...
    public bool DespawnAnimal(AnimalData animalData)
    {
        AnimalController animalToDespawn = spawnedAnimals.FirstOrDefault(a => a.animalData == animalData);
        if (animalToDespawn != null)
        {
            spawnedAnimals.Remove(animalToDespawn);
            Destroy(animalToDespawn.gameObject);
            return true;
        }
        return false;
    }

    private Vector3 GetRandomSpawnPosition(Bounds bounds)
    {
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            0
        );
    }
    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);
            if (result != null)
                return result;
        }
        return null;
    }
}