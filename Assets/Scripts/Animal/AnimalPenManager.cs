// AnimalPenManager.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Старый класс PenInfo отсюда удален.

public class AnimalPenManager : MonoBehaviour
{
    public static AnimalPenManager Instance { get; private set; }

    // <<< ИЗМЕНЕНИЕ: Используем новую структуру данных
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
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeStartingAnimals();
        }
    }

    private void InitializeStartingAnimals()
    {
        // Этот метод должен выполниться только один раз за сессию игры
        if (isInitialized) return;
        // В будущем здесь будет логика загрузки сохранения, и если она успешна,
        // этот блок кода выполняться не будет.

        Debug.Log("<color=yellow>[AnimalPenManager]</color> Инициализация стартового набора животных...");

        foreach (var startInfo in startingAnimals)
        {
            if (startInfo.animalData != null && startInfo.count > 0)
            {
                for (int i = 0; i < startInfo.count; i++)
                {
                    // Используем уже существующий метод AddAnimal
                    AddAnimal(startInfo.animalData);
                }
            }
        }

        isInitialized = true;
    }

    // <<< ИЗМЕНЕНИЕ: Этот метод теперь возвращает PenConfigData
    public PenConfigData GetPenConfigForAnimal(AnimalData animalData)
    {
        if (penConfigurations == null) return null;
        return penConfigurations.FirstOrDefault(p => p.animalData == animalData);
    }

    // <<< НОВЫЙ МЕТОД: Возвращает весь список конфигураций для TrainPenController
    public List<PenConfigData> GetAllPenConfigs()
    {
        return penConfigurations;
    }

    // ... (остальной код AddAnimal, SellAnimal, etc. не меняется) ...
    public void AddAnimal(AnimalData animalData)
    {
        AnimalStateData newState = new AnimalStateData(animalData);
        allAnimals.Add(newState);
        Debug.Log($"<color=green>[AnimalPenManager]</color> Добавлено новое животное: {animalData.speciesName}. " +
                  $"Всего в списке: <color=yellow>{allAnimals.Count}</color> животных. " +
                  $"Из них этого типа: <color=yellow>{GetAnimalCount(animalData)}</color>");
    }

    public bool SellAnimal(AnimalData animalData)
    {
        AnimalStateData animalToRemove = allAnimals.FirstOrDefault(a => a.animalData == animalData);

        if (animalToRemove != null)
        {
            allAnimals.Remove(animalToRemove);
            Debug.Log($"Из AnimalPenManager продано животное: {animalData.speciesName}. Осталось: {GetAnimalCount(animalData)}");
            return true;
        }

        Debug.LogWarning($"Попытка продать {animalData.speciesName}, но в данных их не найдено.");
        return false;
    }


    public int GetAnimalCount(AnimalData animalData)
    {
        return allAnimals.Count(a => a.animalData == animalData);
    }

    public List<AnimalStateData> GetStatesForAnimalType(AnimalData animalData)
    {
        return allAnimals.Where(a => a.animalData == animalData).ToList();
    }
}