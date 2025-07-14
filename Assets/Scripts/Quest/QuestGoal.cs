using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public enum GoalType
{
    Gather,       // ������� ���� ���������� ������
    GatherAny,    // ������� ����� ���������� �� ������ ��������
    Buy,          // ������ ���� ���������� �������
    BuyAny,       // ������ ����� ���������� �� ������ ���������  <<< �����
    Earn,         // ���������� �����
    Use,          // ������������ �������/�������� ���-��
    FeedAnimal,
    SellFor,
    SellForAnimals, 
    SellForPlants
}

[System.Serializable]
public class QuestGoal
{
    public GoalType goalType;
    [Tooltip("ID ����. Gather/Buy: ItemData.name. FeedAnimal: AnimalData.speciesName.")]
    public string targetID;
    public int requiredAmount;

    [Tooltip("������ ID ��� ��������� ����� (GatherAny).")]
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