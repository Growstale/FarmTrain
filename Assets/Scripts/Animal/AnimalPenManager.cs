using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AnimalPenManager : MonoBehaviour
{
    public static AnimalPenManager Instance { get; private set; }

    [Header("Конфигурация загонов")]
    [SerializeField] private List<PenConfigData> penConfigurations;

    [System.Serializable]
    public struct StartingAnimal
    {
        public AnimalData animalData;
        public int count;
    }

    [Header("Стартовый набор")]
    [SerializeField] private List<StartingAnimal> startingAnimals;

    private List<AnimalStateData> allAnimals = new List<AnimalStateData>();
    private Dictionary<AnimalData, int> penUpgradeLevels = new Dictionary<AnimalData, int>();

    private bool hasInitialized = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // НЕ вызываем инициализацию здесь, чтобы дать другим менеджерам шанс проснуться.
        // Мы вызовем ее один раз при первой необходимости.
    }

    // Этот метод теперь будет вызываться из других скриптов,
    // чтобы гарантировать, что он выполнен хотя бы раз.
    private void EnsureInitialized()
    {
        if (hasInitialized) return;

        Debug.Log("<color=yellow>[AnimalPenManager]</color> Первая инициализация...");

        penUpgradeLevels.Clear();
        foreach (var config in penConfigurations)
        {
            penUpgradeLevels[config.animalData] = 0;
        }

        allAnimals.Clear();
        foreach (var startInfo in startingAnimals)
        {
            if (startInfo.animalData != null && startInfo.count > 0)
            {
                for (int i = 0; i < startInfo.count; i++)
                {
                    // Мы не вызываем AddAnimal, а создаем данные напрямую.
                    // AddAnimal предназначен для добавления ЖИВЫХ животных в процессе игры.
                    AnimalStateData newState = new AnimalStateData(startInfo.animalData);
                    allAnimals.Add(newState);
                    Debug.Log($"Добавлены стартовые данные для: {startInfo.animalData.speciesName}");
                }
            }
        }

        hasInitialized = true;
    }

    public void ApplyUpgrade(ItemData upgradeItem)
    {
        if (upgradeItem == null) return;

        Debug.Log($"Пытаюсь применить улучшение: {upgradeItem.name}");

        foreach (var config in penConfigurations)
        {
            for (int i = 0; i < config.upgradeLevels.Count; i++)
            {
                var levelData = config.upgradeLevels[i];
                if (levelData.requiredUpgradeItem == upgradeItem)
                {
                    Debug.Log($"Найдена цель для улучшения: загон {config.animalData.speciesName}, уровень {i}");

                    int currentLevel = GetCurrentPenLevel(config.animalData);
                    Debug.Log($"Текущий уровень загона: {currentLevel}. Требуемый предыдущий уровень: {i - 1}");

                    if (currentLevel == i - 1)
                    {
                        penUpgradeLevels[config.animalData] = i;
                        Debug.Log($"<color=cyan>УРОВЕНЬ ЗАГОНА {config.animalData.speciesName} ПОВЫШЕН ДО {i}</color>");

                        if (TrainPenController.Instance != null)
                        {
                            TrainPenController.Instance.UpdatePenVisuals(config.animalData);
                        }
                        return;
                    }
                    else
                    {
                        Debug.LogWarning("Условие по уровню не выполнено! Улучшение не применено.");
                    }
                }
            }
        }
        Debug.LogError($"Не найдено улучшение, соответствующее предмету {upgradeItem.name} в конфигурациях загонов!");
    }

    public int GetCurrentPenLevel(AnimalData animalData)
    {
        EnsureInitialized();
        if (penUpgradeLevels.TryGetValue(animalData, out int level))
        {
            return level;
        }
        return -1;
    }

    public PenConfigData GetPenConfigForAnimal(AnimalData animalData)
    {
        EnsureInitialized();
        return penConfigurations.FirstOrDefault(p => p.animalData == animalData);
    }

    public List<PenConfigData> GetAllPenConfigs()
    {
        EnsureInitialized();
        return penConfigurations;
    }

    public void AddAnimal(AnimalData animalData)
    {
        EnsureInitialized();
        AnimalStateData newState = new AnimalStateData(animalData);
        allAnimals.Add(newState);
        Debug.Log($"<color=green>[AnimalPenManager]</color> Куплено/добавлено новое животное: {animalData.speciesName}.");
    }

    public bool SellAnimal(AnimalData animalData)
    {
        EnsureInitialized();
        AnimalStateData animalToRemove = allAnimals.FirstOrDefault(a => a.animalData == animalData);
        if (animalToRemove != null)
        {
            allAnimals.Remove(animalToRemove);
            return true;
        }
        return false;
    }

    public int GetAnimalCount(AnimalData animalData)
    {
        EnsureInitialized();
        return allAnimals.Count(a => a.animalData == animalData);
    }

    public List<AnimalStateData> GetStatesForAnimalType(AnimalData animalData)
    {
        EnsureInitialized();
        return allAnimals.Where(a => a.animalData == animalData).ToList();
    }

    public int GetMaxCapacityForAnimal(AnimalData animalData)
    {
        EnsureInitialized();
        var config = GetPenConfigForAnimal(animalData);
        if (config == null) return 0;

        int currentLevel = GetCurrentPenLevel(animalData);
        if (currentLevel >= 0 && currentLevel < config.upgradeLevels.Count)
        {
            return config.upgradeLevels[currentLevel].capacity;
        }

        return 0;
    }
    public bool HasAutoFeeder(AnimalData animalData)
    {
        EnsureInitialized();

        // Находим конфигурацию для этого животного
        var config = GetPenConfigForAnimal(animalData);
        if (config == null) return false;

        // Получаем его текущий уровень улучшения
        int currentLevel = GetCurrentPenLevel(animalData);
        if (currentLevel < 0 || currentLevel >= config.upgradeLevels.Count) return false;

        // Проверяем данные текущего уровня
        return config.upgradeLevels[currentLevel].providesAutoFeeding;
    }

}