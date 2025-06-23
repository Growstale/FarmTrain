using UnityEngine;
using System.Collections.Generic;

public class AnimalPenManager : MonoBehaviour
{
    public static AnimalPenManager Instance { get; private set; }

    private Dictionary<AnimalData, int> animalCounts = new Dictionary<AnimalData, int>();

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

    public void AddAnimal(AnimalData animalData, int quantity = 1)
    {
        if (animalCounts.ContainsKey(animalData))
        {
            animalCounts[animalData] += quantity;
        }
        else
        {
            animalCounts[animalData] = quantity;
        }
        Debug.Log($"Данные обновлены: теперь есть {animalCounts[animalData]} {animalData.speciesName}");
    }

    public bool SellAnimal(AnimalData animalData, int quantity = 1)
    {
        if (animalCounts.TryGetValue(animalData, out int currentCount) && currentCount >= quantity)
        {
            animalCounts[animalData] -= quantity;
            Debug.Log($"Данные обновлены: осталось {animalCounts[animalData]} {animalData.speciesName}");
            return true;
        }

        Debug.LogWarning($"Попытка продать {animalData.speciesName}, но в данных их недостаточно.");
        return false;
    }

    public int GetAnimalCount(AnimalData animalData)
    {
        if (animalCounts.TryGetValue(animalData, out int count))
        {
            return count;
        }
        return 0;
    }

    public IReadOnlyDictionary<AnimalData, int> GetAllAnimalCounts()
    {
        return animalCounts;
    }
}