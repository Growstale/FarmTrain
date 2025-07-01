using UnityEngine;

// Перечисление для типов целей
public enum GoalType
{
    Gather, // Собрать ресурсы (или иметь в инвентаре)
    Buy,    // Купить что-то
    Earn,   // Заработать денег
    Use    // Использовать предмет/починить что-то (пока не используется, но оставим)
}

[System.Serializable]
public class QuestGoal
{
    public GoalType goalType;
    [Tooltip("ID цели. Для Gather/Buy - ItemData.name. Для Earn - не используется.")]
    public string targetID;
    public int requiredAmount;

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