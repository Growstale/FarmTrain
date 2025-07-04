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

    private List<AnimalStateData> allAnimals = new List<AnimalStateData>();

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
        }
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