using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

// QuestGoal.cs

public enum GoalType
{
    Gather,       // Собрать один конкретный ресурс
    GatherAny,    // Собрать любое количество из списка ресурсов
    Buy,          // Купить один конкретный предмет
    BuyAny,       // Купить любое количество из списка предметов  <<< НОВОЕ
    Earn,         // Заработать денег
    Use,          // Использовать предмет/починить что-то
    FeedAnimal,
    SellFor
}

[System.Serializable]
public class QuestGoal
{
    public GoalType goalType;
    [Tooltip("ID цели. Gather/Buy: ItemData.name. FeedAnimal: AnimalData.speciesName.")]
    public string targetID;
    public int requiredAmount;

    [Tooltip("Список ID для составных целей (GatherAny).")]
    public List<string> targetIDs;


    [HideInInspector] public int currentAmount;

    public bool IsReached()
    {
        return (currentAmount >= requiredAmount);
    }

    public void UpdateProgress(int amount)
    {
        currentAmount = Mathf.Clamp(currentAmount + amount, 0, requiredAmount);
    }
}