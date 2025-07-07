using System;
using UnityEngine;


public static class GameEvents 
{
    // Отслеживаем сбор урожая
    public static event Action<int> OnHarvestTheCrop;

    // Отслеживаем сбор предметов из животных 
    public static event Action<int> OnCollectAnimalProduct;

    // Отслеживаем сбор монет 
    public static event Action<int> OnCollectCoin;

    //  Отслеживаем количество новых животных 
    public static event Action<int> OnAddedNewAnimal;
    //  Отслеживаем количество новых растений
    public static event Action<int> OnCollectAllPlants;

    // Отслеживаем новое улучшение
    public static event Action<int> OnAddedNewUpdgrade;
    // Отслеживание количестов всех заданий 

    public static event Action<int> OnCompleteTheQuest;



    // Методы для вызова событий из других скриптов 

    public static void TriggerHarvestCrop(int amount)
    {
        OnHarvestTheCrop?.Invoke(amount);
    }

    public static void TriggerCollectAnimalProduct(int amount)
    {
        OnCollectAnimalProduct?.Invoke(amount);
    }

    public static void TriggerCollectCoin(int amount)
    {
        OnCollectCoin?.Invoke(amount);
    }

    public static void TriggerAddedNewAnimal(int amount)
    {
        OnAddedNewAnimal?.Invoke(amount);
    }
    public static void TriggerOnCollectAllPlants(int amount)
    {
        OnCollectAllPlants?.Invoke(amount);
    }
    public static void TriggerAddedNewUpdgrade(int amount)
    {
        OnAddedNewUpdgrade?.Invoke(amount);
    }
    public static void TriggerCompleteTheQuest(int amount)
    {
        OnCompleteTheQuest?.Invoke(amount);
    }
}
